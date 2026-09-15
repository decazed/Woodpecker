using FluentAssertions;
using Woodpecker.Application.Puzzles;

namespace Woodpecker.Application.Tests.Puzzles;

public class ImportPuzzlesTests
{
    [Fact]
    public async Task Handle_WithValidItems_CreatesAllPuzzlesAndPersistsThem()
    {
        using var context = TestDbContextFactory.Create();
        var handler = new ImportPuzzlesHandler(context);
        var command = new ImportPuzzlesCommand(new[]
        {
            new PuzzleImportItem("fen-1", new[] { "e2e4", "e7e5" }, 1200),
            new PuzzleImportItem("fen-2", new[] { "d2d4", "d7d5" }, 1500),
        });

        var ids = await handler.Handle(command, CancellationToken.None);

        ids.Should().HaveCount(2);
        context.Puzzles.Count().Should().Be(2);
    }
}
