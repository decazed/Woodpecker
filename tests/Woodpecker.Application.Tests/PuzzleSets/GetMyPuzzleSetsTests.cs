using FluentAssertions;
using Woodpecker.Application.PuzzleSets;
using Woodpecker.Domain;

namespace Woodpecker.Application.Tests.PuzzleSets;

public class GetMyPuzzleSetsTests
{
    [Fact]
    public async Task Handle_ReportsAttemptedCountOfTheActiveCycleOnly()
    {
        using var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var ids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();
        var puzzleSet = PuzzleSet.Create("Set", userId, ids);

        // Cycle 1 terminé (5 tentatives), cycle 2 en cours (2 tentatives) : seul le second compte.
        var finished = TrainingCycle.Start(puzzleSet.Id, userId, 1, DateTime.UtcNow);
        foreach (var id in ids)
            finished.RecordAttempt(id, true, TimeSpan.FromSeconds(1), DateTime.UtcNow);
        finished.Complete(ids.Count, DateTime.UtcNow);

        var active = TrainingCycle.Start(puzzleSet.Id, userId, 2, DateTime.UtcNow);
        active.RecordAttempt(ids[0], true, TimeSpan.FromSeconds(1), DateTime.UtcNow);
        active.RecordAttempt(ids[1], false, TimeSpan.FromSeconds(1), DateTime.UtcNow);

        context.PuzzleSets.Add(puzzleSet);
        context.TrainingCycles.AddRange(finished, active);
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetMyPuzzleSetsHandler(context)
            .Handle(new GetMyPuzzleSetsQuery(userId), CancellationToken.None);

        var summary = result.Single();
        summary.ActiveCycleId.Should().Be(active.Id);
        summary.ActiveCycleAttemptedCount.Should().Be(2);
        summary.PuzzleCount.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WithoutActiveCycle_ReportsZeroAttempted()
    {
        using var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        context.PuzzleSets.Add(PuzzleSet.Create("Set", userId, new[] { Guid.NewGuid() }));
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetMyPuzzleSetsHandler(context)
            .Handle(new GetMyPuzzleSetsQuery(userId), CancellationToken.None);

        result.Single().ActiveCycleAttemptedCount.Should().Be(0);
    }
}
