using MediatR;
using Microsoft.EntityFrameworkCore;
using Woodpecker.Application.Abstractions;
using Woodpecker.Application.Common.Exceptions;

namespace Woodpecker.Application.Statistics;

public record CycleProgressionDto(int CycleNumber, int SuccessCount, int AttemptCount, double SuccessRate, TimeSpan AverageDuration, DateTime? CompletedAt);

public record GetSetProgressionQuery(Guid PuzzleSetId, Guid UserId) : IRequest<IReadOnlyList<CycleProgressionDto>>;

// Une seule requête (LEFT JOIN via group join) plutôt qu'une requête par cycle :
// évite le N+1 tout en restant lisible, adapté au volume visé par ce projet.
public class GetSetProgressionHandler(IApplicationDbContext context)
    : IRequestHandler<GetSetProgressionQuery, IReadOnlyList<CycleProgressionDto>>
{
    public async Task<IReadOnlyList<CycleProgressionDto>> Handle(
        GetSetProgressionQuery request,
        CancellationToken cancellationToken)
    {
        var puzzleSet = await context.PuzzleSets.FindAsync([request.PuzzleSetId], cancellationToken)
            ?? throw new NotFoundException($"Set {request.PuzzleSetId} introuvable.");

        if (puzzleSet.OwnerId != request.UserId)
            throw new NotFoundException($"Set {request.PuzzleSetId} introuvable.");

        var cyclesWithAttempts = await (
            from cycle in context.TrainingCycles
            where cycle.PuzzleSetId == request.PuzzleSetId
            join attempt in context.PuzzleAttempts on cycle.Id equals attempt.TrainingCycleId into attemptsGroup
            orderby cycle.CycleNumber
            select new
            {
                cycle.CycleNumber,
                cycle.CompletedAt,
                Attempts = attemptsGroup.ToList(),
            }).ToListAsync(cancellationToken);

        return cyclesWithAttempts
            .Select(c =>
            {
                var successCount = c.Attempts.Count(a => a.IsSuccess);
                var totalDuration = c.Attempts.Aggregate(TimeSpan.Zero, (sum, a) => sum + a.Duration);

                return new CycleProgressionDto(
                    c.CycleNumber,
                    successCount,
                    c.Attempts.Count,
                    c.Attempts.Count == 0 ? 0 : (double)successCount / c.Attempts.Count,
                    c.Attempts.Count == 0 ? TimeSpan.Zero : totalDuration / c.Attempts.Count,
                    c.CompletedAt);
            })
            .ToList();
    }
}
