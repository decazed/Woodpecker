using MediatR;
using Microsoft.EntityFrameworkCore;
using Woodpecker.Application.Abstractions;
using Woodpecker.Application.Common.Exceptions;

namespace Woodpecker.Application.TrainingCycles;

public record CycleAttemptDto(Guid PuzzleId, bool IsSuccess, TimeSpan Duration);

// PreviousAttempts : tentatives du dernier cycle terminé (et non abandonné) du même set avant
// celui-ci, pour comparer les temps puzzle par puzzle. Vide au premier cycle.
public record CycleDetailDto(
    Guid CycleId,
    int CycleNumber,
    int? PreviousCycleNumber,
    IReadOnlyList<CycleAttemptDto> Attempts,
    IReadOnlyList<CycleAttemptDto> PreviousAttempts);

public record GetCycleDetailQuery(Guid CycleId, Guid UserId) : IRequest<CycleDetailDto>;

public class GetCycleDetailHandler(IApplicationDbContext context)
    : IRequestHandler<GetCycleDetailQuery, CycleDetailDto>
{
    public async Task<CycleDetailDto> Handle(GetCycleDetailQuery request, CancellationToken cancellationToken)
    {
        var cycle = await context.TrainingCycles.FindAsync([request.CycleId], cancellationToken)
            ?? throw new NotFoundException($"Cycle {request.CycleId} introuvable.");

        if (cycle.UserId != request.UserId)
            throw new NotFoundException($"Cycle {request.CycleId} introuvable.");

        var attempts = await LoadAttemptsAsync(cycle.Id, cancellationToken);

        // Le tri se fait en mémoire (CycleNumber est petit) : certains providers ne savent pas
        // traduire un OrderBy sur un type nullable côté SQL de façon portable.
        var previousCycle = (await context.TrainingCycles
                .Where(tc => tc.PuzzleSetId == cycle.PuzzleSetId
                    && tc.UserId == cycle.UserId
                    && tc.CompletedAt != null
                    && tc.CycleNumber < cycle.CycleNumber)
                .ToListAsync(cancellationToken))
            .OrderByDescending(tc => tc.CycleNumber)
            .FirstOrDefault();

        var previousAttempts = previousCycle is null
            ? []
            : await LoadAttemptsAsync(previousCycle.Id, cancellationToken);

        return new CycleDetailDto(cycle.Id, cycle.CycleNumber, previousCycle?.CycleNumber, attempts, previousAttempts);
    }

    private async Task<IReadOnlyList<CycleAttemptDto>> LoadAttemptsAsync(Guid cycleId, CancellationToken cancellationToken)
    {
        var rows = await context.PuzzleAttempts
            .Where(a => a.TrainingCycleId == cycleId)
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(a => a.AttemptedAt)
            .Select(a => new CycleAttemptDto(a.PuzzleId, a.IsSuccess, a.Duration))
            .ToList();
    }
}
