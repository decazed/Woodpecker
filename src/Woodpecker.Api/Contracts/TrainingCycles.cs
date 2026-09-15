namespace Woodpecker.Api.Contracts;

public record StartTrainingCycleRequest(Guid PuzzleSetId);

public record StartTrainingCycleResponse(Guid Id);

public record SubmitPuzzleAttemptRequest(Guid PuzzleId, int MoveIndex, string SubmittedMove, TimeSpan Duration);

public record SubmitPuzzleAttemptResponse(bool IsCorrect, bool IsPuzzleComplete, string? ResultingFen, int? NextMoveIndex);
