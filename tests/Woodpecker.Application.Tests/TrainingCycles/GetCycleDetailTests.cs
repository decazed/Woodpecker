using FluentAssertions;
using Woodpecker.Application.Common.Exceptions;
using Woodpecker.Application.TrainingCycles;
using Woodpecker.Domain;

namespace Woodpecker.Application.Tests.TrainingCycles;

public class GetCycleDetailTests
{
    private static TrainingCycle CompletedCycle(PuzzleSet set, Guid userId, int number, params (Guid Puzzle, bool Success, int Seconds)[] attempts)
    {
        var cycle = TrainingCycle.Start(set.Id, userId, number, DateTime.UtcNow);
        foreach (var (puzzle, success, seconds) in attempts)
            cycle.RecordAttempt(puzzle, success, TimeSpan.FromSeconds(seconds), DateTime.UtcNow);
        cycle.Complete(attempts.Length, DateTime.UtcNow);
        return cycle;
    }

    [Fact]
    public async Task Handle_ReturnsAttemptsAndThePreviousCompletedCycle()
    {
        using var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var (a, b) = (Guid.NewGuid(), Guid.NewGuid());
        var set = PuzzleSet.Create("Set", userId, new[] { a, b });

        var cycle1 = CompletedCycle(set, userId, 1, (a, true, 30), (b, false, 5));
        var cycle2 = CompletedCycle(set, userId, 2, (a, true, 20), (b, true, 25));
        var cycle3 = TrainingCycle.Start(set.Id, userId, 3, DateTime.UtcNow);
        cycle3.RecordAttempt(a, true, TimeSpan.FromSeconds(15), DateTime.UtcNow);

        context.PuzzleSets.Add(set);
        context.TrainingCycles.AddRange(cycle1, cycle2, cycle3);
        await context.SaveChangesAsync(CancellationToken.None);

        var detail = await new GetCycleDetailHandler(context)
            .Handle(new GetCycleDetailQuery(cycle3.Id, userId), CancellationToken.None);

        detail.CycleNumber.Should().Be(3);
        detail.Attempts.Should().ContainSingle(x => x.PuzzleId == a && x.Duration == TimeSpan.FromSeconds(15));
        detail.PreviousCycleNumber.Should().Be(2);
        detail.PreviousAttempts.Should().HaveCount(2).And.Contain(x => x.PuzzleId == b && x.Duration == TimeSpan.FromSeconds(25));
    }

    [Fact]
    public async Task Handle_IgnoresAbandonedAndFirstCycleHasNoPrevious()
    {
        using var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var a = Guid.NewGuid();
        var set = PuzzleSet.Create("Set", userId, new[] { a });

        var abandoned = TrainingCycle.Start(set.Id, userId, 1, DateTime.UtcNow);
        abandoned.RecordAttempt(a, true, TimeSpan.FromSeconds(9), DateTime.UtcNow);
        abandoned.Abandon(DateTime.UtcNow);
        var current = TrainingCycle.Start(set.Id, userId, 1, DateTime.UtcNow);

        context.PuzzleSets.Add(set);
        context.TrainingCycles.AddRange(abandoned, current);
        await context.SaveChangesAsync(CancellationToken.None);

        var detail = await new GetCycleDetailHandler(context)
            .Handle(new GetCycleDetailQuery(current.Id, userId), CancellationToken.None);

        detail.PreviousCycleNumber.Should().BeNull();
        detail.PreviousAttempts.Should().BeEmpty();
        detail.Attempts.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotOwnCycle_ThrowsNotFound()
    {
        using var context = TestDbContextFactory.Create();
        var cycle = TrainingCycle.Start(Guid.NewGuid(), Guid.NewGuid(), 1, DateTime.UtcNow);
        context.TrainingCycles.Add(cycle);
        await context.SaveChangesAsync(CancellationToken.None);

        var act = () => new GetCycleDetailHandler(context)
            .Handle(new GetCycleDetailQuery(cycle.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
