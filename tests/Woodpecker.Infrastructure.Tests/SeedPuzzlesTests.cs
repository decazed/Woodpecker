using FluentAssertions;
using Woodpecker.Application.Abstractions;
using Woodpecker.Domain;
using Woodpecker.Infrastructure;
using Woodpecker.Infrastructure.Puzzles;

namespace Woodpecker.Infrastructure.Tests;

// Garantit que le catalogue embarqué (src/Woodpecker.Api/Seed/puzzles.csv, chargé au premier
// démarrage) est importable tel quel et que chaque puzzle est jouable de bout en bout par le
// moteur (coups légaux, indices cohérents), avec assez de puzzles pour chaque niveau proposé.
public class SeedPuzzlesTests
{
    private static readonly string SeedPath = Path.Combine(AppContext.BaseDirectory, "Seed", "puzzles.csv");

    [Fact]
    public void SeedCsv_EveryPuzzle_IsFullyPlayable()
    {
        using var stream = File.OpenRead(SeedPath);
        var items = new LichessPuzzleCsvParser().Parse(stream, new LichessPuzzleImportFilter(null, null, null));
        var validator = new SolutionMoveValidator();

        items.Should().NotBeEmpty();

        foreach (var item in items)
        {
            var puzzle = Puzzle.Create(item.Fen, item.SolutionMoves, item.Rating);

            validator.GetPlayablePosition(puzzle).Should().NotBeNullOrWhiteSpace();

            for (var moveIndex = 1; moveIndex < puzzle.SolutionMoves.Count; moveIndex += 2)
            {
                var result = validator.ValidateMove(puzzle, moveIndex, puzzle.SolutionMoves[moveIndex]);

                result.IsLegal.Should().BeTrue($"puzzle {item.Fen} : coup {moveIndex} ({puzzle.SolutionMoves[moveIndex]}) doit être légal");
                result.IsCorrect.Should().BeTrue();
                result.IsLastMove.Should().Be(moveIndex == puzzle.SolutionMoves.Count - 1);
            }
        }
    }

    [Fact]
    public void SeedCsv_HasEnoughPuzzlesForEveryTemplate()
    {
        using var stream = File.OpenRead(SeedPath);
        var items = new LichessPuzzleCsvParser().Parse(stream, new LichessPuzzleImportFilter(null, null, null));

        foreach (var template in Application.PuzzleSets.SetTemplates.All)
        {
            items.Count(i => i.Rating >= template.MinRating && i.Rating <= template.MaxRating)
                .Should().BeGreaterThanOrEqualTo(template.PuzzleCount, $"le niveau {template.Name} doit être jouable");
        }
    }
}
