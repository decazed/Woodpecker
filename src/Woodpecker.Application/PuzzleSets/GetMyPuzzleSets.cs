using MediatR;
using Microsoft.EntityFrameworkCore;
using Woodpecker.Application.Abstractions;

namespace Woodpecker.Application.PuzzleSets;

// ActiveCycleId : cycle démarré mais pas encore clôturé sur ce set (au plus un, cf.
// StartTrainingCycleHandler), pour que le client propose "reprendre" plutôt que "démarrer".
public record PuzzleSetSummaryDto(Guid Id, string Name, int PuzzleCount, Guid? ActiveCycleId);

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
            .Where(tc => tc.UserId == request.OwnerId && tc.CompletedAt == null && setIds.Contains(tc.PuzzleSetId))
            .ToListAsync(cancellationToken);

        // Un seul cycle ouvert par set en régime normal, mais on tolère des données plus
        // anciennes (d'avant cette règle) en prenant le plus récent.
        var activeCycles = openCycles
            .GroupBy(tc => tc.PuzzleSetId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(tc => tc.CycleNumber).First().Id);

        return sets
            .Select(ps => new PuzzleSetSummaryDto(
                ps.Id,
                ps.Name,
                ps.PuzzleIds.Count,
                activeCycles.TryGetValue(ps.Id, out var cycleId) ? cycleId : null))
            .ToList();
    }
}
