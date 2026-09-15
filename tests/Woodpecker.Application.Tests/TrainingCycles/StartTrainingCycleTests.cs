using FluentAssertions;
using Woodpecker.Application.Common.Exceptions;
using Woodpecker.Application.TrainingCycles;
using Woodpecker.Domain;

namespace Woodpecker.Application.Tests.TrainingCycles;

public class StartTrainingCycleTests
{
    private static async Task<(Guid PuzzleSetId, Guid PuzzleId)> SeedPuzzleSetAsync(Infrastructure.WoodpeckerDbContext context, Guid ownerId)
    {
        var puzzleId = Guid.NewGuid();
        var puzzleSet = PuzzleSet.Create("Set", ownerId, new[] { puzzleId });
        context.PuzzleSets.Add(puzzleSet);
        await context.SaveChangesAsync(CancellationToken.None);
        return (puzzleSet.Id, puzzleId);
    }

    [Fact]
    public async Task Handle_FirstCycleOnASet_CreatesCycleWithNumberOne()
    {
        using var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var (puzzleSetId, _) = await SeedPuzzleSetAsync(context, userId);
        var handler = new StartTrainingCycleHandler(context);

        var id = await handler.Handle(new StartTrainingCycleCommand(puzzleSetId, userId), CancellationToken.None);

        var stored = await context.TrainingCycles.FindAsync(id);
        stored.Should().NotBeNull();
        stored!.CycleNumber.Should().Be(1);
    }

    [Fact]
    public async Task Handle_SecondCycleAfterFirstCompleted_CreatesCycleWithNumberTwo()
    {
        using var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var (puzzleSetId, puzzleId) = await SeedPuzzleSetAsync(context, userId);
        var handler = new StartTrainingCycleHandler(context);

        var firstId = await handler.Handle(new StartTrainingCycleCommand(puzzleSetId, userId), CancellationToken.None);
        var firstCycle = await context.TrainingCycles.FindAsync(firstId);
        firstCycle!.RecordAttempt(puzzleId, true, TimeSpan.FromSeconds(1), DateTime.UtcNow);
        firstCycle.Complete(1, DateTime.UtcNow);
        await context.SaveChangesAsync(CancellationToken.None);

        var secondId = await handler.Handle(new StartTrainingCycleCommand(puzzleSetId, userId), CancellationToken.None);

        var stored = await context.TrainingCycles.FindAsync(secondId);
        stored!.CycleNumber.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WhenPreviousCycleNotCompleted_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var (puzzleSetId, _) = await SeedPuzzleSetAsync(context, userId);
        var handler = new StartTrainingCycleHandler(context);

        await handler.Handle(new StartTrainingCycleCommand(puzzleSetId, userId), CancellationToken.None);

        var act = () => handler.Handle(new StartTrainingCycleCommand(puzzleSetId, userId), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_CyclesOnDifferentSets_AreNumberedIndependently()
    {
        using var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var (firstSetId, _) = await SeedPuzzleSetAsync(context, userId);
        var (secondSetId, _) = await SeedPuzzleSetAsync(context, userId);
        var handler = new StartTrainingCycleHandler(context);

        await handler.Handle(new StartTrainingCycleCommand(firstSetId, userId), CancellationToken.None);
        var idOnOtherSet = await handler.Handle(new StartTrainingCycleCommand(secondSetId, userId), CancellationToken.None);

        var stored = await context.TrainingCycles.FindAsync(idOnOtherSet);
        stored!.CycleNumber.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotOwnSet_ThrowsNotFound()
    {
        using var context = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var (puzzleSetId, _) = await SeedPuzzleSetAsync(context, ownerId);
        var handler = new StartTrainingCycleHandler(context);

        var act = () => handler.Handle(new StartTrainingCycleCommand(puzzleSetId, otherUserId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
