using FluentAssertions;
using Woodpecker.Domain;

namespace Woodpecker.Domain.Tests;

public class PuzzleSetTests
{
    [Fact]
    public void Create_WithValidData_CreatesSet()
    {
        var puzzleIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var set = PuzzleSet.Create("Set 1", Guid.NewGuid(), puzzleIds);

        set.PuzzleIds.Should().BeEquivalentTo(puzzleIds);
    }

    [Fact]
    public void Create_WithEmptyName_Throws()
    {
        var act = () => PuzzleSet.Create("", Guid.NewGuid(), new[] { Guid.NewGuid() });

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNoPuzzles_Throws()
    {
        var act = () => PuzzleSet.Create("Set 1", Guid.NewGuid(), Array.Empty<Guid>());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithDuplicatePuzzleIds_Throws()
    {
        var duplicateId = Guid.NewGuid();

        var act = () => PuzzleSet.Create("Set 1", Guid.NewGuid(), new[] { duplicateId, duplicateId });

        act.Should().Throw<ArgumentException>();
    }
}
