using FluentAssertions;
using Woodpecker.Application.PuzzleSets;

namespace Woodpecker.Application.Tests.PuzzleSets;

public class CreatePuzzleSetTests
{
    [Fact]
    public async Task Handle_WithValidCommand_CreatesPuzzleSetAndPersistsIt()
    {
        using var context = TestDbContextFactory.Create();
        var handler = new CreatePuzzleSetHandler(context);
        var command = new CreatePuzzleSetCommand("Set 1", Guid.NewGuid(), new[] { Guid.NewGuid(), Guid.NewGuid() });

        var id = await handler.Handle(command, CancellationToken.None);

        var stored = await context.PuzzleSets.FindAsync(id);
        stored.Should().NotBeNull();
        stored!.Name.Should().Be("Set 1");
        stored.PuzzleIds.Should().BeEquivalentTo(command.PuzzleIds);
    }
}
