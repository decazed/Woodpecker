using FluentAssertions;
using Woodpecker.Application.Statistics;
using Woodpecker.Domain;

namespace Woodpecker.Application.Tests.Statistics;

public class StatisticsTests
{
    [Fact]
    public async Task GetCycleStats_ComputesSuccessRateAndDurations()
    {
        using var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var puzzleId = Guid.NewGuid();
        var cycle = TrainingCycle.Start(Guid.NewGuid(), userId, 1, DateTime.UtcNow);
        cycle.RecordAttempt(puzzleId, true, TimeSpan.FromSeconds(10), DateTime.UtcNow);
        cycle.RecordAttempt(Guid.NewGuid(), false, TimeSpan.FromSeconds(20), DateTime.UtcNow);
        context.TrainingCycles.Add(cycle);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetCycleStatsHandler(context);
        var stats = await handler.Handle(new GetCycleStatsQuery(cycle.Id, userId), CancellationToken.None);

        stats.PuzzleCount.Should().Be(2);
        stats.SuccessCount.Should().Be(1);
        stats.SuccessRate.Should().Be(0.5);
        stats.TotalDuration.Should().Be(TimeSpan.FromSeconds(30));
        stats.AverageDuration.Should().Be(TimeSpan.FromSeconds(15));
    }

    [Fact]
    public async Task GetFrequentlyMissedPuzzles_ReturnsPuzzlesAboveMinAttemptsOrderedByFailureRate()
    {
        using var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var oftenMissedPuzzle = Guid.NewGuid();
        var rarelyMissedPuzzle = Guid.NewGuid();

        var puzzleSet = PuzzleSet.Create("Set", userId, new[] { oftenMissedPuzzle, rarelyMissedPuzzle });
        var setId = puzzleSet.Id;

        var cycle1 = TrainingCycle.Start(setId, userId, 1, DateTime.UtcNow);
        cycle1.RecordAttempt(oftenMissedPuzzle, false, TimeSpan.FromSeconds(5), DateTime.UtcNow);
        cycle1.RecordAttempt(rarelyMissedPuzzle, true, TimeSpan.FromSeconds(5), DateTime.UtcNow);

        var cycle2 = TrainingCycle.Start(setId, userId, 2, DateTime.UtcNow);
        cycle2.RecordAttempt(oftenMissedPuzzle, false, TimeSpan.FromSeconds(5), DateTime.UtcNow);
        cycle2.RecordAttempt(rarelyMissedPuzzle, true, TimeSpan.FromSeconds(5), DateTime.UtcNow);

        context.PuzzleSets.Add(puzzleSet);
        context.TrainingCycles.AddRange(cycle1, cycle2);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetFrequentlyMissedPuzzlesHandler(context);
        var result = await handler.Handle(new GetFrequentlyMissedPuzzlesQuery(setId, userId, MinAttempts: 2), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].PuzzleId.Should().Be(oftenMissedPuzzle);
        result[0].FailureRate.Should().Be(1.0);
        result[1].PuzzleId.Should().Be(rarelyMissedPuzzle);
        result[1].FailureRate.Should().Be(0.0);
    }
}
