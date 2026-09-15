using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Woodpecker.Application.Abstractions;
using Woodpecker.Application.Common.Exceptions;
using Woodpecker.Domain;

namespace Woodpecker.Application.TrainingCycles;

public record StartTrainingCycleCommand(Guid PuzzleSetId, Guid UserId) : IRequest<Guid>;

public class StartTrainingCycleValidator : AbstractValidator<StartTrainingCycleCommand>
{
    public StartTrainingCycleValidator()
    {
        RuleFor(c => c.PuzzleSetId).NotEqual(Guid.Empty);
        RuleFor(c => c.UserId).NotEqual(Guid.Empty);
    }
}

public class StartTrainingCycleHandler(IApplicationDbContext context)
    : IRequestHandler<StartTrainingCycleCommand, Guid>
{
    public async Task<Guid> Handle(StartTrainingCycleCommand request, CancellationToken cancellationToken)
    {
        var puzzleSet = await context.PuzzleSets.FindAsync([request.PuzzleSetId], cancellationToken)
            ?? throw new NotFoundException($"Set {request.PuzzleSetId} introuvable.");

        if (puzzleSet.OwnerId != request.UserId)
            throw new NotFoundException($"Set {request.PuzzleSetId} introuvable.");

        var existingCycles = await context.TrainingCycles
            .Where(tc => tc.PuzzleSetId == request.PuzzleSetId && tc.UserId == request.UserId)
            .ToListAsync(cancellationToken);

        // La méthode Woodpecker enchaîne des cycles complets : on ne peut pas en démarrer
        // un nouveau tant que le précédent n'est pas clôturé (cf. TrainingCycle.Complete,
        // déclenché automatiquement par SubmitPuzzleAttemptHandler).
        if (existingCycles.Any(c => !c.IsCompleted))
            throw new InvalidOperationException("Un cycle est déjà en cours pour ce set.");

        var cycle = TrainingCycle.Start(
            request.PuzzleSetId,
            request.UserId,
            cycleNumber: existingCycles.Count + 1,
            startedAt: DateTime.UtcNow);

        context.TrainingCycles.Add(cycle);
        await context.SaveChangesAsync(cancellationToken);

        return cycle.Id;
    }
}
