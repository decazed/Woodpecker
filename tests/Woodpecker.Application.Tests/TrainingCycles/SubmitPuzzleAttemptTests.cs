using FluentAssertions;
using Woodpecker.Application.Common.Exceptions;
using Woodpecker.Application.TrainingCycles;
using Woodpecker.Domain;
using Woodpecker.Infrastructure;

namespace Woodpecker.Application.Tests.TrainingCycles;

public class SubmitPuzzleAttemptTests
{
    // Position après 1.e4 (trait aux Noirs) : ...e5 est auto-jouée (indice 0), le joueur
    // (Blancs) doit trouver Cf3 (indice 1, seul et dernier coup de la solution).
    private static async Task<(WoodpeckerDbContext Context, TrainingCycle Cycle, Puzzle Puzzle, Guid UserId)> SeedAsync()
    {
        var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();

        var puzzle = Puzzle.Create(
            "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1",
            new[] { "e7e5", "g1f3" },
            1200);
        var puzzleSet = PuzzleSet.Create("Set", userId, new[] { puzzle.Id });
        var cycle = TrainingCycle.Start(puzzleSet.Id, userId, 1, DateTime.UtcNow);

        context.Puzzles.Add(puzzle);
        context.PuzzleSets.Add(puzzleSet);
        context.TrainingCycles.Add(cycle);
        await context.SaveChangesAsync(CancellationToken.None);

        return (context, cycle, puzzle, userId);
    }

    [Fact]
    public async Task Handle_WithCorrectMove_ReturnsSuccessAndRecordsAttempt()
    {
        var (context, cycle, puzzle, userId) = await SeedAsync();
        var handler = new SubmitPuzzleAttemptHandler(context, new SolutionMoveValidator());
        var command = new SubmitPuzzleAttemptCommand(cycle.Id, userId, puzzle.Id, 1, "g1f3", TimeSpan.FromSeconds(5));

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsCorrect.Should().BeTrue();
        result.IsPuzzleComplete.Should().BeTrue();
        var updatedCycle = await context.TrainingCycles.FindAsync(cycle.Id);
        updatedCycle!.Attempts.Should().ContainSingle(a => a.PuzzleId == puzzle.Id && a.IsSuccess);
    }

    [Fact]
    public async Task Handle_WithIncorrectMove_ReturnsFailureAndRecordsAttempt()
    {
        var (context, cycle, puzzle, userId) = await SeedAsync();
        var handler = new SubmitPuzzleAttemptHandler(context, new SolutionMoveValidator());
        var command = new SubmitPuzzleAttemptCommand(cycle.Id, userId, puzzle.Id, 1, "d2d4", TimeSpan.FromSeconds(5));

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsCorrect.Should().BeFalse();
        result.IsPuzzleComplete.Should().BeTrue();
        var updatedCycle = await context.TrainingCycles.FindAsync(cycle.Id);
        updatedCycle!.Attempts.Should().ContainSingle(a => a.PuzzleId == puzzle.Id && !a.IsSuccess);
    }

    [Fact]
    public async Task Handle_WithCorrectMoveButNotLastOfSolution_DoesNotRecordAttemptYet()
    {
        var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();

        // Position après 1.e4 (trait aux Noirs) : ...e5 auto-jouée (indice 0), le joueur
        // (Blancs) trouve Cf3 (indice 1, pas le dernier coup), puis ...Cc6 est auto-jouée
        // (indice 2) avant le dernier coup du joueur Fb5 (indice 3).
        var puzzle = Puzzle.Create(
            "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1",
            new[] { "e7e5", "g1f3", "b8c6", "f1b5" },
            1500);
        var puzzleSet = PuzzleSet.Create("Set", userId, new[] { puzzle.Id });
        var cycle = TrainingCycle.Start(puzzleSet.Id, userId, 1, DateTime.UtcNow);
        context.Puzzles.Add(puzzle);
        context.PuzzleSets.Add(puzzleSet);
        context.TrainingCycles.Add(cycle);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new SubmitPuzzleAttemptHandler(context, new SolutionMoveValidator());
        var command = new SubmitPuzzleAttemptCommand(cycle.Id, userId, puzzle.Id, 1, "g1f3", TimeSpan.FromSeconds(5));

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsCorrect.Should().BeTrue();
        result.IsPuzzleComplete.Should().BeFalse();
        result.ResultingFen.Should().NotBeNull();
        result.NextMoveIndex.Should().Be(3);
        var updatedCycle = await context.TrainingCycles.FindAsync(cycle.Id);
        updatedCycle!.Attempts.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenLastPuzzleOfSetIsAttempted_CompletesCycle()
    {
        var (context, cycle, puzzle, userId) = await SeedAsync();
        var handler = new SubmitPuzzleAttemptHandler(context, new SolutionMoveValidator());
        var command = new SubmitPuzzleAttemptCommand(cycle.Id, userId, puzzle.Id, 1, "g1f3", TimeSpan.FromSeconds(5));

        await handler.Handle(command, CancellationToken.None);

        var updatedCycle = await context.TrainingCycles.FindAsync(cycle.Id);
        updatedCycle!.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AcrossSeparateDbContexts_StillCompletesCycleAfterLastPuzzle()
    {
        // Reproduit le vrai déroulement HTTP : chaque tentative arrive dans un scope (donc un
        // DbContext) différent. Le handler doit recharger les tentatives déjà persistées pour
        // savoir que tous les puzzles du set ont été tentés.
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        Guid cycleId, firstPuzzleId, secondPuzzleId;

        await using (var seed = TestDbContextFactory.Create(dbName))
        {
            var first = Puzzle.Create("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1", new[] { "e7e5", "g1f3" }, 1200);
            var second = Puzzle.Create("rnbqkbnr/pppppppp/8/8/3P4/8/PPP1PPPP/RNBQKBNR b KQkq d3 0 1", new[] { "d7d5", "c2c4" }, 1200);
            var puzzleSet = PuzzleSet.Create("Set", userId, new[] { first.Id, second.Id });
            var cycle = TrainingCycle.Start(puzzleSet.Id, userId, 1, DateTime.UtcNow);
            seed.Puzzles.AddRange(first, second);
            seed.PuzzleSets.Add(puzzleSet);
            seed.TrainingCycles.Add(cycle);
            await seed.SaveChangesAsync(CancellationToken.None);
            (cycleId, firstPuzzleId, secondPuzzleId) = (cycle.Id, first.Id, second.Id);
        }

        await using (var request1 = TestDbContextFactory.Create(dbName))
        {
            var handler = new SubmitPuzzleAttemptHandler(request1, new SolutionMoveValidator());
            await handler.Handle(new SubmitPuzzleAttemptCommand(cycleId, userId, firstPuzzleId, 1, "g1f3", TimeSpan.FromSeconds(5)), CancellationToken.None);
        }

        await using (var request2 = TestDbContextFactory.Create(dbName))
        {
            var handler = new SubmitPuzzleAttemptHandler(request2, new SolutionMoveValidator());
            await handler.Handle(new SubmitPuzzleAttemptCommand(cycleId, userId, secondPuzzleId, 1, "c2c4", TimeSpan.FromSeconds(5)), CancellationToken.None);
        }

        await using var check = TestDbContextFactory.Create(dbName);
        var stored = await check.TrainingCycles.FindAsync(cycleId);
        stored!.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotOwnCycle_ThrowsNotFound()
    {
        var (context, cycle, puzzle, _) = await SeedAsync();
        var handler = new SubmitPuzzleAttemptHandler(context, new SolutionMoveValidator());
        var command = new SubmitPuzzleAttemptCommand(cycle.Id, Guid.NewGuid(), puzzle.Id, 1, "g1f3", TimeSpan.FromSeconds(5));

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
