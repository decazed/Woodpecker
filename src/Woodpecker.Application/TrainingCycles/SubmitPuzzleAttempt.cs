using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Woodpecker.Application.Abstractions;
using Woodpecker.Application.Common.Exceptions;

namespace Woodpecker.Application.TrainingCycles;

// MoveIndex correspond à l'indice dans Puzzle.SolutionMoves : le coup d'indice 0 (mise en
// place jouée par l'adversaire) est toujours appliqué automatiquement (cf. GetPuzzleById /
// PuzzleDto.PlayableFen), donc le premier coup soumis par le joueur est toujours à l'indice 1.
public record SubmitPuzzleAttemptCommand(
    Guid TrainingCycleId,
    Guid UserId,
    Guid PuzzleId,
    int MoveIndex,
    string SubmittedMove,
    TimeSpan Duration) : IRequest<SubmitPuzzleAttemptResult>;

// IsPuzzleComplete indique que le puzzle est terminé (résolu ou raté) : le client doit passer
// au suivant. Sinon (coup correct mais pas le dernier de la solution), ResultingFen contient
// la position après la réponse automatique de l'adversaire, et NextMoveIndex l'indice à
// soumettre pour le prochain coup du joueur.
public record SubmitPuzzleAttemptResult(
    bool IsCorrect,
    bool IsPuzzleComplete,
    string? ResultingFen,
    int? NextMoveIndex);

public class SubmitPuzzleAttemptValidator : AbstractValidator<SubmitPuzzleAttemptCommand>
{
    public SubmitPuzzleAttemptValidator()
    {
        RuleFor(c => c.TrainingCycleId).NotEqual(Guid.Empty);
        RuleFor(c => c.UserId).NotEqual(Guid.Empty);
        RuleFor(c => c.PuzzleId).NotEqual(Guid.Empty);
        RuleFor(c => c.MoveIndex).GreaterThanOrEqualTo(1);
        RuleFor(c => c.SubmittedMove).NotEmpty();
        RuleFor(c => c.Duration).GreaterThan(TimeSpan.Zero);
    }
}

public class SubmitPuzzleAttemptHandler(IApplicationDbContext context, IMoveValidator moveValidator)
    : IRequestHandler<SubmitPuzzleAttemptCommand, SubmitPuzzleAttemptResult>
{
    public async Task<SubmitPuzzleAttemptResult> Handle(
        SubmitPuzzleAttemptCommand request,
        CancellationToken cancellationToken)
    {
        // Include("_attempts") indispensable : Complete() et le calcul d'auto-clôture comptent
        // les tentatives déjà persistées, or un DbContext frais (une requête HTTP = un scope)
        // ne les charge pas tout seul. Sans ça, cycle.Attempts ne contiendrait que la tentative
        // ajoutée dans cette requête et le cycle ne se clôturerait jamais.
        var cycle = await context.TrainingCycles
            .Include("_attempts")
            .FirstOrDefaultAsync(tc => tc.Id == request.TrainingCycleId, cancellationToken)
            ?? throw new NotFoundException($"Cycle {request.TrainingCycleId} introuvable.");

        if (cycle.UserId != request.UserId)
            throw new NotFoundException($"Cycle {request.TrainingCycleId} introuvable.");

        // Sans ce garde-fou, les demi-coups corrects d'un cycle abandonné passeraient (seul
        // RecordAttempt, appelé en fin de puzzle, refuserait).
        if (cycle.IsAbandoned)
            throw new InvalidOperationException("Ce cycle a été abandonné.");

        var puzzle = await context.Puzzles.FindAsync([request.PuzzleId], cancellationToken)
            ?? throw new NotFoundException($"Puzzle {request.PuzzleId} introuvable.");

        var validation = moveValidator.ValidateMove(puzzle, request.MoveIndex, request.SubmittedMove);

        // Méthode Woodpecker oblige : pas de retour en arrière sur un coup faux, l'échec est
        // définitif pour cette tentative (cf. le comportement des puzzles Lichess). On ne
        // persiste donc une TrainingCycle.RecordAttempt qu'au moment où le puzzle est résolu
        // ou raté, jamais à chaque demi-coup intermédiaire (sinon les stats compteraient
        // plusieurs "puzzles" pour un seul, un demi-coup correct = pas encore une tentative).
        if (!validation.IsCorrect)
        {
            cycle.RecordAttempt(request.PuzzleId, isSuccess: false, request.Duration, DateTime.UtcNow);
            await CompleteCycleIfSetFinishedAsync(cycle, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return new SubmitPuzzleAttemptResult(IsCorrect: false, IsPuzzleComplete: true, ResultingFen: null, NextMoveIndex: null);
        }

        if (validation.IsLastMove)
        {
            cycle.RecordAttempt(request.PuzzleId, isSuccess: true, request.Duration, DateTime.UtcNow);
            await CompleteCycleIfSetFinishedAsync(cycle, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return new SubmitPuzzleAttemptResult(IsCorrect: true, IsPuzzleComplete: true, ResultingFen: null, NextMoveIndex: null);
        }

        return new SubmitPuzzleAttemptResult(
            IsCorrect: true,
            IsPuzzleComplete: false,
            ResultingFen: validation.ResultingFen,
            NextMoveIndex: request.MoveIndex + 2);
    }

    private async Task CompleteCycleIfSetFinishedAsync(Domain.TrainingCycle cycle, CancellationToken cancellationToken)
    {
        var puzzleSet = await context.PuzzleSets
            .Include("_items")
            .FirstOrDefaultAsync(ps => ps.Id == cycle.PuzzleSetId, cancellationToken)
            ?? throw new NotFoundException($"Set {cycle.PuzzleSetId} introuvable.");

        var distinctPuzzlesAttempted = cycle.Attempts.Select(a => a.PuzzleId).Distinct().Count();
        if (distinctPuzzlesAttempted >= puzzleSet.PuzzleIds.Count)
            cycle.Complete(puzzleSet.PuzzleIds.Count, DateTime.UtcNow);
    }
}
