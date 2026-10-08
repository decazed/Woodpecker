using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Woodpecker.Application.Common.Exceptions;
using Woodpecker.Application.PuzzleSets;
using Woodpecker.Domain;

namespace Woodpecker.Application.Tests.PuzzleSets;

public class DeletePuzzleSetTests
{
    [Fact]
    public async Task Handle_DeletesSetItsCyclesAndTheirAttempts()
    {
        var database = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var puzzleId = Guid.NewGuid();
        var puzzleSet = PuzzleSet.Create("Set", userId, new[] { puzzleId });
        var cycle = TrainingCycle.Start(puzzleSet.Id, userId, 1, DateTime.UtcNow);
        cycle.RecordAttempt(puzzleId, true, TimeSpan.FromSeconds(5), DateTime.UtcNow);

        using (var seed = TestDbContextFactory.Create(database))
        {
            seed.PuzzleSets.Add(puzzleSet);
            seed.TrainingCycles.Add(cycle);
            await seed.SaveChangesAsync(CancellationToken.None);
        }

        // Contexte neuf (tracker vierge) : simule une requête HTTP distincte de celle qui a créé les données.
        using (var context = TestDbContextFactory.Create(database))
        {
            await new DeletePuzzleSetHandler(context)
                .Handle(new DeletePuzzleSetCommand(puzzleSet.Id, userId), CancellationToken.None);
        }

        using var verify = TestDbContextFactory.Create(database);
        (await verify.PuzzleSets.AnyAsync()).Should().BeFalse();
        (await verify.TrainingCycles.AnyAsync()).Should().BeFalse();
        (await verify.PuzzleAttempts.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DoesNotTouchOtherSets()
    {
        using var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var deleted = PuzzleSet.Create("A supprimer", userId, new[] { Guid.NewGuid() });
        var kept = PuzzleSet.Create("A garder", userId, new[] { Guid.NewGuid() });
        context.PuzzleSets.AddRange(deleted, kept);
        context.TrainingCycles.Add(TrainingCycle.Start(kept.Id, userId, 1, DateTime.UtcNow));
        await context.SaveChangesAsync(CancellationToken.None);

        await new DeletePuzzleSetHandler(context)
            .Handle(new DeletePuzzleSetCommand(deleted.Id, userId), CancellationToken.None);

        (await context.PuzzleSets.Select(ps => ps.Id).ToListAsync()).Should().Equal(kept.Id);
        (await context.TrainingCycles.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotOwnSet_ThrowsNotFoundAndKeepsSet()
    {
        using var context = TestDbContextFactory.Create();
        var puzzleSet = PuzzleSet.Create("Set", Guid.NewGuid(), new[] { Guid.NewGuid() });
        context.PuzzleSets.Add(puzzleSet);
        await context.SaveChangesAsync(CancellationToken.None);

        var act = () => new DeletePuzzleSetHandler(context)
            .Handle(new DeletePuzzleSetCommand(puzzleSet.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        (await context.PuzzleSets.CountAsync()).Should().Be(1);
    }
}
