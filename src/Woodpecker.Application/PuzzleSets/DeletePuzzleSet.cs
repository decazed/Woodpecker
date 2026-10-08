using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Woodpecker.Application.Abstractions;
using Woodpecker.Application.Common.Exceptions;

namespace Woodpecker.Application.PuzzleSets;

public record DeletePuzzleSetCommand(Guid PuzzleSetId, Guid UserId) : IRequest;

public class DeletePuzzleSetValidator : AbstractValidator<DeletePuzzleSetCommand>
{
    public DeletePuzzleSetValidator()
    {
        RuleFor(c => c.PuzzleSetId).NotEqual(Guid.Empty);
        RuleFor(c => c.UserId).NotEqual(Guid.Empty);
    }
}

public class DeletePuzzleSetHandler(IApplicationDbContext context)
    : IRequestHandler<DeletePuzzleSetCommand>
{
    public async Task Handle(DeletePuzzleSetCommand request, CancellationToken cancellationToken)
    {
        var puzzleSet = await context.PuzzleSets
            .Include("_items")
            .FirstOrDefaultAsync(ps => ps.Id == request.PuzzleSetId, cancellationToken)
            ?? throw new NotFoundException($"Set {request.PuzzleSetId} introuvable.");

        if (puzzleSet.OwnerId != request.UserId)
            throw new NotFoundException($"Set {request.PuzzleSetId} introuvable.");

        // TrainingCycle ne référence son set que par Id (pas de FK, cf. frontière d'agrégat) :
        // aucune cascade ne les atteint depuis PuzzleSet, on supprime donc les cycles à la main.
        // Leurs tentatives, elles, partent en cascade (relation _attempts).
        var cycles = await context.TrainingCycles
            .Include("_attempts")
            .Where(tc => tc.PuzzleSetId == request.PuzzleSetId)
            .ToListAsync(cancellationToken);

        context.TrainingCycles.RemoveRange(cycles);
        context.PuzzleSets.Remove(puzzleSet);
        await context.SaveChangesAsync(cancellationToken);
    }
}
