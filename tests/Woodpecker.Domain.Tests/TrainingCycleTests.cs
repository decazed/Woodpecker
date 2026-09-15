using FluentAssertions;
using Woodpecker.Domain;

namespace Woodpecker.Domain.Tests;

public class TrainingCycleTests
{
    private static TrainingCycle StartCycle() =>
        TrainingCycle.Start(Guid.NewGuid(), Guid.NewGuid(), cycleNumber: 1, startedAt: DateTime.UtcNow);

    [Fact]
    public void Start_WithInvalidCycleNumber_Throws()
    {
        var act = () => TrainingCycle.Start(Guid.NewGuid(), Guid.NewGuid(), cycleNumber: 0, DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RecordAttempt_AddsAttemptToCycle()
    {
        var cycle = StartCycle();
        var puzzleId = Guid.NewGuid();

        cycle.RecordAttempt(puzzleId, isSuccess: true, TimeSpan.FromSeconds(12), DateTime.UtcNow);

        cycle.Attempts.Should().ContainSingle(a => a.PuzzleId == puzzleId && a.IsSuccess);
    }

    [Fact]
    public void RecordAttempt_WithNonPositiveDuration_Throws()
    {
        var cycle = StartCycle();

        var act = () => cycle.RecordAttempt(Guid.NewGuid(), true, TimeSpan.Zero, DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Complete_WhenAllPuzzlesAttempted_ClosesCycle()
    {
        var cycle = StartCycle();
        cycle.RecordAttempt(Guid.NewGuid(), true, TimeSpan.FromSeconds(5), DateTime.UtcNow);

        cycle.Complete(expectedPuzzleCount: 1, completedAt: DateTime.UtcNow);

        cycle.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void Complete_WhenNotAllPuzzlesAttempted_Throws()
    {
        var cycle = StartCycle();
        cycle.RecordAttempt(Guid.NewGuid(), true, TimeSpan.FromSeconds(5), DateTime.UtcNow);

        var act = () => cycle.Complete(expectedPuzzleCount: 2, completedAt: DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RecordAttempt_OnCompletedCycle_Throws()
    {
        var cycle = StartCycle();
        cycle.RecordAttempt(Guid.NewGuid(), true, TimeSpan.FromSeconds(5), DateTime.UtcNow);
        cycle.Complete(expectedPuzzleCount: 1, completedAt: DateTime.UtcNow);

        var act = () => cycle.RecordAttempt(Guid.NewGuid(), true, TimeSpan.FromSeconds(3), DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Complete_Twice_Throws()
    {
        var cycle = StartCycle();
        cycle.RecordAttempt(Guid.NewGuid(), true, TimeSpan.FromSeconds(5), DateTime.UtcNow);
        cycle.Complete(expectedPuzzleCount: 1, completedAt: DateTime.UtcNow);

        var act = () => cycle.Complete(expectedPuzzleCount: 1, completedAt: DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>();
    }
}
