using FluentAssertions;
using Woodpecker.Application.Common.Exceptions;
using Woodpecker.Application.PuzzleSets;
using Woodpecker.Domain;

namespace Woodpecker.Application.Tests.PuzzleSets;

public class CreatePuzzleSetFromTemplateTests
{
    private const string Fen = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1";
    private static readonly string[] Moves = ["e7e5", "g1f3"];

    [Fact]
    public async Task Handle_PicksPuzzlesInTemplateRatingRange()
    {
        using var context = TestDbContextFactory.Create();
        context.Puzzles.AddRange(Enumerable.Range(0, 15).Select(i => Puzzle.Create(Fen, Moves, 1000 + i)));
        context.Puzzles.AddRange(Enumerable.Range(0, 5).Select(i => Puzzle.Create(Fen, Moves, 2000 + i)));
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = new CreatePuzzleSetFromTemplateHandler(context);

        var id = await handler.Handle(new CreatePuzzleSetFromTemplateCommand("debutant", Guid.NewGuid()), CancellationToken.None);

        var set = await context.PuzzleSets.FindAsync(id);
        set!.PuzzleIds.Should().HaveCount(10);
        var ratings = context.Puzzles.Where(p => set.PuzzleIds.Contains(p.Id)).Select(p => p.Rating);
        ratings.Should().OnlyContain(r => r < 1300);
    }

    [Fact]
    public async Task Handle_WhenCatalogTooSmall_Throws()
    {
        using var context = TestDbContextFactory.Create();
        context.Puzzles.Add(Puzzle.Create(Fen, Moves, 1000));
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = new CreatePuzzleSetFromTemplateHandler(context);

        var act = () => handler.Handle(new CreatePuzzleSetFromTemplateCommand("debutant", Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WithUnknownTemplate_ThrowsNotFound()
    {
        using var context = TestDbContextFactory.Create();
        var handler = new CreatePuzzleSetFromTemplateHandler(context);

        var act = () => handler.Handle(new CreatePuzzleSetFromTemplateCommand("grandmaster", Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
