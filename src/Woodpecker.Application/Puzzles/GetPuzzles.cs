using MediatR;
using Microsoft.EntityFrameworkCore;
using Woodpecker.Application.Abstractions;

namespace Woodpecker.Application.Puzzles;

public record PuzzleSummaryDto(Guid Id, int Rating);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

// Le catalogue de puzzles (importé depuis Lichess) peut compter plusieurs millions de
// lignes : Page/PageSize évitent de tout charger en mémoire sur un simple GET /api/puzzles.
public record GetPuzzlesQuery(int? MinRating, int? MaxRating, int Page = 1, int PageSize = 50)
    : IRequest<PagedResult<PuzzleSummaryDto>>;

public class GetPuzzlesHandler(IApplicationDbContext context)
    : IRequestHandler<GetPuzzlesQuery, PagedResult<PuzzleSummaryDto>>
{
    private const int MaxPageSize = 200;

    public async Task<PagedResult<PuzzleSummaryDto>> Handle(GetPuzzlesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = context.Puzzles.AsQueryable();

        if (request.MinRating is not null)
            query = query.Where(p => p.Rating >= request.MinRating);

        if (request.MaxRating is not null)
            query = query.Where(p => p.Rating <= request.MaxRating);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Rating)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PuzzleSummaryDto(p.Id, p.Rating))
            .ToListAsync(cancellationToken);

        return new PagedResult<PuzzleSummaryDto>(items, page, pageSize, totalCount);
    }
}
