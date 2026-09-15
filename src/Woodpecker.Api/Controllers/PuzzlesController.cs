using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Woodpecker.Api.Contracts;
using Woodpecker.Application.Abstractions;
using Woodpecker.Application.Puzzles;

namespace Woodpecker.Api.Controllers;

[ApiController]
[Route("api/puzzles")]
[Authorize]
public class PuzzlesController(ISender sender, ILichessPuzzleCsvParser csvParser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<PuzzleSummaryDto>>> GetAll(
        [FromQuery] int? minRating,
        [FromQuery] int? maxRating,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var puzzles = await sender.Send(new GetPuzzlesQuery(minRating, maxRating, page, pageSize), cancellationToken);
        return Ok(puzzles);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PuzzleDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var puzzle = await sender.Send(new GetPuzzleByIdQuery(id), cancellationToken);
        return Ok(puzzle);
    }

    // Le catalogue de puzzles est partagé entre tous les utilisateurs (importé depuis
    // Lichess) : seul un admin peut l'alimenter, pas chaque utilisateur individuellement.
    [Authorize(Roles = nameof(Woodpecker.Domain.UserRole.Admin))]
    [HttpPost("import")]
    public async Task<ActionResult<ImportPuzzlesResponse>> Import(
        ImportPuzzlesRequest request,
        CancellationToken cancellationToken)
    {
        var items = request.Puzzles
            .Select(p => new PuzzleImportItem(p.Fen, p.SolutionMoves, p.Rating))
            .ToList();

        var ids = await sender.Send(new ImportPuzzlesCommand(items), cancellationToken);

        return Ok(new ImportPuzzlesResponse(ids));
    }

    // Import en masse depuis un export CSV de la base de puzzles Lichess
    // (https://database.lichess.org/#puzzles), avec filtrage optionnel par rating/thème.
    [Authorize(Roles = nameof(Woodpecker.Domain.UserRole.Admin))]
    [HttpPost("import-csv")]
    public async Task<ActionResult<ImportPuzzlesResponse>> ImportCsv(
        IFormFile file,
        [FromQuery] int? minRating,
        [FromQuery] int? maxRating,
        [FromQuery] string? theme,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var items = csvParser.Parse(stream, new LichessPuzzleImportFilter(minRating, maxRating, theme));

        if (items.Count == 0)
            return Ok(new ImportPuzzlesResponse(Array.Empty<Guid>()));

        var ids = await sender.Send(new ImportPuzzlesCommand(items), cancellationToken);

        return Ok(new ImportPuzzlesResponse(ids));
    }
}
