using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Woodpecker.Api.Contracts;
using Woodpecker.Application.PuzzleSets;
using Woodpecker.Application.Statistics;

namespace Woodpecker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/puzzle-sets")]
public class PuzzleSetsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreatePuzzleSetResponse>> Create(
        CreatePuzzleSetRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(
            new CreatePuzzleSetCommand(request.Name, User.GetUserId(), request.PuzzleIds),
            cancellationToken);

        return CreatedAtAction(nameof(Create), new { id }, new CreatePuzzleSetResponse(id));
    }

    [HttpGet("templates")]
    public async Task<ActionResult<IReadOnlyList<SetTemplateDto>>> GetTemplates(CancellationToken cancellationToken)
    {
        var templates = await sender.Send(new GetSetTemplatesQuery(), cancellationToken);
        return Ok(templates);
    }

    [HttpPost("from-template")]
    public async Task<ActionResult<CreatePuzzleSetResponse>> CreateFromTemplate(
        CreatePuzzleSetFromTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(
            new CreatePuzzleSetFromTemplateCommand(request.TemplateKey, User.GetUserId(), request.PuzzleCount),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id }, new CreatePuzzleSetResponse(id));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PuzzleSetSummaryDto>>> GetMine(CancellationToken cancellationToken)
    {
        var sets = await sender.Send(new GetMyPuzzleSetsQuery(User.GetUserId()), cancellationToken);
        return Ok(sets);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PuzzleSetDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var set = await sender.Send(new GetPuzzleSetByIdQuery(id, User.GetUserId()), cancellationToken);
        return Ok(set);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeletePuzzleSetCommand(id, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/progression")]
    public async Task<ActionResult<IReadOnlyList<CycleProgressionDto>>> GetProgression(
        Guid id,
        CancellationToken cancellationToken)
    {
        var progression = await sender.Send(new GetSetProgressionQuery(id, User.GetUserId()), cancellationToken);
        return Ok(progression);
    }

    [HttpGet("{id:guid}/frequently-missed")]
    public async Task<ActionResult<IReadOnlyList<FrequentlyMissedPuzzleDto>>> GetFrequentlyMissed(
        Guid id,
        [FromQuery] int minAttempts,
        CancellationToken cancellationToken)
    {
        var query = minAttempts > 0
            ? new GetFrequentlyMissedPuzzlesQuery(id, User.GetUserId(), minAttempts)
            : new GetFrequentlyMissedPuzzlesQuery(id, User.GetUserId());

        var puzzles = await sender.Send(query, cancellationToken);
        return Ok(puzzles);
    }
}
