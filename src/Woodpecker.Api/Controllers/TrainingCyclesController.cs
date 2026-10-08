using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Woodpecker.Api.Contracts;
using Woodpecker.Application.Statistics;
using Woodpecker.Application.TrainingCycles;

namespace Woodpecker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/training-cycles")]
public class TrainingCyclesController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<StartTrainingCycleResponse>> Start(
        StartTrainingCycleRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(
            new StartTrainingCycleCommand(request.PuzzleSetId, User.GetUserId()),
            cancellationToken);

        return CreatedAtAction(nameof(Start), new { id }, new StartTrainingCycleResponse(id));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TrainingCycleDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var cycle = await sender.Send(new GetTrainingCycleQuery(id, User.GetUserId()), cancellationToken);
        return Ok(cycle);
    }

    [HttpGet("{id:guid}/detail")]
    public async Task<ActionResult<CycleDetailDto>> GetDetail(Guid id, CancellationToken cancellationToken)
    {
        var detail = await sender.Send(new GetCycleDetailQuery(id, User.GetUserId()), cancellationToken);
        return Ok(detail);
    }

    [HttpPost("{id:guid}/attempts")]
    public async Task<ActionResult<SubmitPuzzleAttemptResponse>> SubmitAttempt(
        Guid id,
        SubmitPuzzleAttemptRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SubmitPuzzleAttemptCommand(id, User.GetUserId(), request.PuzzleId, request.MoveIndex, request.SubmittedMove, request.Duration),
            cancellationToken);

        return Ok(new SubmitPuzzleAttemptResponse(result.IsCorrect, result.IsPuzzleComplete, result.ResultingFen, result.NextMoveIndex));
    }

    [HttpPost("{id:guid}/abandon")]
    public async Task<IActionResult> Abandon(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new AbandonTrainingCycleCommand(id, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/stats")]
    public async Task<ActionResult<CycleStatsDto>> GetStats(Guid id, CancellationToken cancellationToken)
    {
        var stats = await sender.Send(new GetCycleStatsQuery(id, User.GetUserId()), cancellationToken);
        return Ok(stats);
    }
}
