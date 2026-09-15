using MediatR;
using Microsoft.EntityFrameworkCore;
using Woodpecker.Application.Abstractions;
using Woodpecker.Application.Common.Exceptions;

namespace Woodpecker.Application.TrainingCycles;

// AttemptedPuzzleIds permet au client de reprendre un cycle en cours là où il en était
// (l'état "puzzle courant" ne vit que côté client, il est perdu au rafraîchissement).
public record TrainingCycleDto(
    Guid Id,
    Guid PuzzleSetId,
    int CycleNumber,
    bool IsCompleted,
    IReadOnlyList<Guid> AttemptedPuzzleIds);

public record GetTrainingCycleQuery(Guid CycleId, Guid UserId) : IRequest<TrainingCycleDto>;

public class GetTrainingCycleHandler(IApplicationDbContext context)
    : IRequestHandler<GetTrainingCycleQuery, TrainingCycleDto>
{
    public async Task<TrainingCycleDto> Handle(GetTrainingCycleQuery request, CancellationToken cancellationToken)
    {
        var cycle = await context.TrainingCycles.FindAsync([request.CycleId], cancellationToken)
            ?? throw new NotFoundException($"Cycle {request.CycleId} introuvable.");

        if (cycle.UserId != request.UserId)
            throw new NotFoundException($"Cycle {request.CycleId} introuvable.");

        var attemptedPuzzleIds = await context.PuzzleAttempts
            .Where(a => a.TrainingCycleId == cycle.Id)
            .Select(a => a.PuzzleId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return new TrainingCycleDto(cycle.Id, cycle.PuzzleSetId, cycle.CycleNumber, cycle.IsCompleted, attemptedPuzzleIds);
    }
}
