using FluentAssertions;
using Woodpecker.Application.Common.Exceptions;
using Woodpecker.Application.PuzzleSets;
using Woodpecker.Application.Statistics;
using Woodpecker.Application.TrainingCycles;
using Woodpecker.Domain;
using Woodpecker.Infrastructure;

namespace Woodpecker.Application.Tests.TrainingCycles;

public class AbandonTrainingCycleTests
{
    private static async Task<(PuzzleSet Set, TrainingCycle Cycle, Guid UserId)> SeedAsync(WoodpeckerDbContext context)
    {
        var userId = Guid.NewGuid();
        var puzzleSet = PuzzleSet.Create("Set", userId, new[] { Guid.NewGuid(), Guid.NewGuid() });
        var cycle = TrainingCycle.Start(puzzleSet.Id, userId, 1, DateTime.UtcNow);
        cycle.RecordAttempt(puzzleSet.PuzzleIds[0], true, TimeSpan.FromSeconds(5), DateTime.UtcNow);

        context.PuzzleSets.Add(puzzleSet);
        context.TrainingCycles.Add(cycle);
        await context.SaveChangesAsync(CancellationToken.None);
        return (puzzleSet, cycle, userId);
    }

    [Fact]
    public async Task Handle_OnActiveCycle_MarksItAbandoned()
    {
        using var context = TestDbContextFactory.Create();
        var (_, cycle, userId) = await SeedAsync(context);
        var handler = new AbandonTrainingCycleHandler(context);

        await handler.Handle(new AbandonTrainingCycleCommand(cycle.Id, userId), CancellationToken.None);

        var stored = await context.TrainingCycles.FindAsync(cycle.Id);
        stored!.IsAbandoned.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotOwnCycle_ThrowsNotFound()
    {
        using var context = TestDbContextFactory.Create();
        var (_, cycle, _) = await SeedAsync(context);
        var handler = new AbandonTrainingCycleHandler(context);

        var act = () => handler.Handle(new AbandonTrainingCycleCommand(cycle.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_OnAlreadyAbandonedCycle_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var (_, cycle, userId) = await SeedAsync(context);
        var handler = new AbandonTrainingCycleHandler(context);
        await handler.Handle(new AbandonTrainingCycleCommand(cycle.Id, userId), CancellationToken.None);

        var act = () => handler.Handle(new AbandonTrainingCycleCommand(cycle.Id, userId), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task StartCycle_AfterAbandon_IsAllowedAndReusesTheCycleNumber()
    {
        using var context = TestDbContextFactory.Create();
        var (set, cycle, userId) = await SeedAsync(context);
        await new AbandonTrainingCycleHandler(context)
            .Handle(new AbandonTrainingCycleCommand(cycle.Id, userId), CancellationToken.None);

        var newId = await new StartTrainingCycleHandler(context)
            .Handle(new StartTrainingCycleCommand(set.Id, userId), CancellationToken.None);

        var stored = await context.TrainingCycles.FindAsync(newId);
        stored!.CycleNumber.Should().Be(1);
    }

    [Fact]
    public async Task GetMyPuzzleSets_AfterAbandon_HasNoActiveCycle()
    {
        using var context = TestDbContextFactory.Create();
        var (set, cycle, userId) = await SeedAsync(context);
        var query = new GetMyPuzzleSetsQuery(userId);
        var handler = new GetMyPuzzleSetsHandler(context);

        (await handler.Handle(query, CancellationToken.None)).Single().ActiveCycleId.Should().Be(cycle.Id);

        await new AbandonTrainingCycleHandler(context)
            .Handle(new AbandonTrainingCycleCommand(cycle.Id, userId), CancellationToken.None);

        (await handler.Handle(query, CancellationToken.None)).Single().ActiveCycleId.Should().BeNull();
    }

    [Fact]
    public async Task Statistics_ExcludeAbandonedCycles()
    {
        using var context = TestDbContextFactory.Create();
        var (set, cycle, userId) = await SeedAsync(context);
        await new AbandonTrainingCycleHandler(context)
            .Handle(new AbandonTrainingCycleCommand(cycle.Id, userId), CancellationToken.None);

        var progression = await new GetSetProgressionHandler(context)
            .Handle(new GetSetProgressionQuery(set.Id, userId), CancellationToken.None);
        var missed = await new GetFrequentlyMissedPuzzlesHandler(context)
            .Handle(new GetFrequentlyMissedPuzzlesQuery(set.Id, userId, MinAttempts: 1), CancellationToken.None);

        progression.Should().BeEmpty();
        missed.Should().BeEmpty();
    }

    [Fact]
    public async Task SubmitAttempt_OnAbandonedCycle_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var (_, cycle, userId) = await SeedAsync(context);
        await new AbandonTrainingCycleHandler(context)
            .Handle(new AbandonTrainingCycleCommand(cycle.Id, userId), CancellationToken.None);
        var handler = new SubmitPuzzleAttemptHandler(context, new SolutionMoveValidator());

        var act = () => handler.Handle(
            new SubmitPuzzleAttemptCommand(cycle.Id, userId, Guid.NewGuid(), 1, "g1f3", TimeSpan.FromSeconds(1)),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
