using MediatR;
using Microsoft.EntityFrameworkCore;
using Woodpecker.Application.Abstractions;
using Woodpecker.Application.Common.Exceptions;

namespace Woodpecker.Application.Statistics;

public record FrequentlyMissedPuzzleDto(Guid PuzzleId, int AttemptCount, int FailureCount, double FailureRate);

public record GetFrequentlyMissedPuzzlesQuery(Guid PuzzleSetId, Guid UserId, int MinAttempts = 2)
    : IRequest<IReadOnlyList<FrequentlyMissedPuzzleDto>>;

public class GetFrequentlyMissedPuzzlesHandler(IApplicationDbContext context)
    : IRequestHandler<GetFrequentlyMissedPuzzlesQuery, IReadOnlyList<FrequentlyMissedPuzzleDto>>
{
    public async Task<IReadOnlyList<FrequentlyMissedPuzzleDto>> Handle(
        GetFrequentlyMissedPuzzlesQuery request,
        CancellationToken cancellationToken)
    {
        var puzzleSet = await context.PuzzleSets.FindAsync([request.PuzzleSetId], cancellationToken)
            ?? throw new NotFoundException($"Set {request.PuzzleSetId} introuvable.");

        if (puzzleSet.OwnerId != request.UserId)
            throw new NotFoundException($"Set {request.PuzzleSetId} introuvable.");

        var cycleIds = context.TrainingCycles
            .Where(tc => tc.PuzzleSetId == request.PuzzleSetId && tc.AbandonedAt == null)
            .Select(tc => tc.Id);

        // GroupBy traduit en GROUP BY + COUNT côté SQL : l'agrégation se fait en base,
        // pas en mémoire. C'est exactement le genre de requête qu'un repository générique
        // gèrerait mal (cf. la décision d'architecture de l'étape 4).
        var grouped = await context.PuzzleAttempts
            .Where(a => cycleIds.Contains(a.TrainingCycleId))
            .GroupBy(a => a.PuzzleId)
            .Select(g => new
            {
                PuzzleId = g.Key,
                AttemptCount = g.Count(),
                FailureCount = g.Count(a => !a.IsSuccess),
            })
            .Where(x => x.AttemptCount >= request.MinAttempts)
            .ToListAsync(cancellationToken);

        return grouped
            .Select(x => new FrequentlyMissedPuzzleDto(x.PuzzleId, x.AttemptCount, x.FailureCount, (double)x.FailureCount / x.AttemptCount))
            .OrderByDescending(x => x.FailureRate)
            .ToList();
    }
}
