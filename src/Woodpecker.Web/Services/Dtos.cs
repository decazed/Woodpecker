namespace Woodpecker.Web.Services;

// DTOs dupliqués côté client plutôt que projet Contracts partagé, pour gagner du temps ;
// dans un vrai projet on extrairait ça dans une bibliothèque commune Api <-> Web.
public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token);

public record PuzzleSetSummaryDto(Guid Id, string Name, int PuzzleCount, Guid? ActiveCycleId, int ActiveCycleAttemptedCount);
public record PuzzleSetDto(Guid Id, string Name, IReadOnlyList<Guid> PuzzleIds);
public record CreatePuzzleSetResponse(Guid Id);
public record CreatePuzzleSetFromTemplateRequest(string TemplateKey, int? PuzzleCount = null);
public record SetTemplateDto(string Key, string Name, string Description, int PuzzleCount, int AvailablePuzzleCount, IReadOnlyList<int> PuzzleCountChoices);

public record PuzzleDto(Guid Id, string Fen, string PlayableFen, int Rating);

public record StartTrainingCycleRequest(Guid PuzzleSetId);
public record StartTrainingCycleResponse(Guid Id);
public record TrainingCycleDto(Guid Id, Guid PuzzleSetId, int CycleNumber, bool IsCompleted, bool IsAbandoned, IReadOnlyList<Guid> AttemptedPuzzleIds);

public record CycleAttemptDto(Guid PuzzleId, bool IsSuccess, TimeSpan Duration);
public record CycleDetailDto(
    Guid CycleId,
    int CycleNumber,
    int? PreviousCycleNumber,
    IReadOnlyList<CycleAttemptDto> Attempts,
    IReadOnlyList<CycleAttemptDto> PreviousAttempts);

public record SubmitPuzzleAttemptRequest(Guid PuzzleId, int MoveIndex, string SubmittedMove, TimeSpan Duration);
public record SubmitPuzzleAttemptResponse(bool IsCorrect, bool IsPuzzleComplete, string? ResultingFen, int? NextMoveIndex);

public record CycleProgressionDto(
    int CycleNumber,
    int SuccessCount,
    int AttemptCount,
    double SuccessRate,
    TimeSpan AverageDuration,
    DateTime? CompletedAt);

public record CycleStatsDto(
    Guid CycleId,
    int CycleNumber,
    int PuzzleCount,
    int SuccessCount,
    double SuccessRate,
    TimeSpan TotalDuration,
    TimeSpan AverageDuration);
