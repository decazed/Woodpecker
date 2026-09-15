using MediatR;
using Microsoft.EntityFrameworkCore;
using Woodpecker.Application.Abstractions;
using Woodpecker.Application.Common.Exceptions;

namespace Woodpecker.Application.Statistics;

public record CycleStatsDto(
    Guid CycleId,
    int CycleNumber,
    int PuzzleCount,
    int SuccessCount,
    double SuccessRate,
    TimeSpan TotalDuration,
    TimeSpan AverageDuration);

public record GetCycleStatsQuery(Guid CycleId, Guid UserId) : IRequest<CycleStatsDto>;

public class GetCycleStatsHandler(IApplicationDbContext context) : IRequestHandler<GetCycleStatsQuery, CycleStatsDto>
{
    public async Task<CycleStatsDto> Handle(GetCycleStatsQuery request, CancellationToken cancellationToken)
    {
        var cycle = await context.TrainingCycles.FindAsync([request.CycleId], cancellationToken)
            ?? throw new NotFoundException($"Cycle {request.CycleId} introuvable.");

        if (cycle.UserId != request.UserId)
            throw new NotFoundException($"Cycle {request.CycleId} introuvable.");

        var attempts = await context.PuzzleAttempts
            .Where(a => a.TrainingCycleId == request.CycleId)
            .ToListAsync(cancellationToken);

        var successCount = attempts.Count(a => a.IsSuccess);
        var totalDuration = attempts.Aggregate(TimeSpan.Zero, (sum, a) => sum + a.Duration);

        return new CycleStatsDto(
            cycle.Id,
            cycle.CycleNumber,
            attempts.Count,
            successCount,
            attempts.Count == 0 ? 0 : (double)successCount / attempts.Count,
            totalDuration,
            attempts.Count == 0 ? TimeSpan.Zero : totalDuration / attempts.Count);
    }
}
