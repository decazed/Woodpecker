namespace Woodpecker.Domain;

public class PuzzleAttempt : Entity
{
    public Guid TrainingCycleId { get; }
    public Guid PuzzleId { get; }
    public bool IsSuccess { get; }
    public TimeSpan Duration { get; }
    public DateTime AttemptedAt { get; }

    private PuzzleAttempt(
        Guid id,
        Guid trainingCycleId,
        Guid puzzleId,
        bool isSuccess,
        TimeSpan duration,
        DateTime attemptedAt) : base(id)
    {
        TrainingCycleId = trainingCycleId;
        PuzzleId = puzzleId;
        IsSuccess = isSuccess;
        Duration = duration;
        AttemptedAt = attemptedAt;
    }

    // internal : une tentative ne doit être créée qu'à travers TrainingCycle.RecordAttempt,
    // pour garantir qu'elle est toujours rattachée à un cycle valide et non clôturé.
    internal static PuzzleAttempt Create(
        Guid trainingCycleId,
        Guid puzzleId,
        bool isSuccess,
        TimeSpan duration,
        DateTime attemptedAt)
    {
        if (duration <= TimeSpan.Zero)
            throw new ArgumentException("La durée d'une tentative doit être positive.", nameof(duration));

        return new PuzzleAttempt(Guid.NewGuid(), trainingCycleId, puzzleId, isSuccess, duration, attemptedAt);
    }
}
