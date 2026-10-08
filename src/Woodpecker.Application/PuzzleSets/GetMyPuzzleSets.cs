using MediatR;
using Microsoft.EntityFrameworkCore;
using Woodpecker.Application.Abstractions;

namespace Woodpecker.Application.PuzzleSets;

// ActiveCycleId : cycle démarré mais ni clôturé ni abandonné sur ce set (au plus un, cf.
// StartTrainingCycleHandler), pour que le client propose "reprendre" plutôt que "démarrer".
// ActiveCycleAttemptedCount : nombre de puzzles déjà tentés dans ce cycle actif (0 s'il n'y en a pas),
// pour afficher l'avancement "3/10" sans que le client ait à charger le cycle.
public record PuzzleSetSummaryDto(Guid Id, string Name, int PuzzleCount, Guid? ActiveCycleId, int ActiveCycleAttemptedCount);

public record GetMyPuzzleSetsQuery(Guid OwnerId) : IRequest<IReadOnlyList<PuzzleSetSummaryDto>>;

public class GetMyPuzzleSetsHandler(IApplicationDbContext context)
    : IRequestHandler<GetMyPuzzleSetsQuery, IReadOnlyList<PuzzleSetSummaryDto>>
{
    public async Task<IReadOnlyList<PuzzleSetSummaryDto>> Handle(
        GetMyPuzzleSetsQuery request,
        CancellationToken cancellationToken)
    {
        var sets = await context.PuzzleSets
            .Include("_items")
            .Where(ps => ps.OwnerId == request.OwnerId)
            .ToListAsync(cancellationToken);

        var setIds = sets.Select(ps => ps.Id).ToList();
        var openCycles = await context.TrainingCycles
            .Where(tc => tc.UserId == request.OwnerId && tc.CompletedAt == null && tc.AbandonedAt == null && setIds.Contains(tc.PuzzleSetId))
            .ToListAsync(cancellationToken);

        // Un seul cycle ouvert par set en régime normal, mais on tolère des données plus
        // anciennes (d'avant cette règle) en prenant le plus récent.
        var activeCycles = openCycles
            .GroupBy(tc => tc.PuzzleSetId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(tc => tc.CycleNumber).First().Id);

        var activeCycleIds = activeCycles.Values.ToList();
        var attemptedCounts = await context.PuzzleAttempts
            .Where(a => activeCycleIds.Contains(a.TrainingCycleId))
            .GroupBy(a => a.TrainingCycleId)
            .Select(g => new { CycleId = g.Key, Count = g.Select(a => a.PuzzleId).Distinct().Count() })
            .ToDictionaryAsync(x => x.CycleId, x => x.Count, cancellationToken);

        return sets
            .Select(ps =>
            {
                Guid? cycleId = activeCycles.TryGetValue(ps.Id, out var id) ? id : null;
                var attempted = cycleId is { } c && attemptedCounts.TryGetValue(c, out var n) ? n : 0;
                return new PuzzleSetSummaryDto(ps.Id, ps.Name, ps.PuzzleIds.Count, cycleId, attempted);
            })
            .ToList();
    }
}
