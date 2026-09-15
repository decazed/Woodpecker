using FluentAssertions;
using Woodpecker.Domain;
using Woodpecker.Infrastructure;

namespace Woodpecker.Infrastructure.Tests;

public class SolutionMoveValidatorTests
{
    // SolutionMoves[0] est toujours la mise en place jouée automatiquement (format Lichess) :
    // ici le FEN correspond à la position juste après 1.e4 (trait aux Noirs), ...e5 est
    // auto-jouée, puis le joueur (Blancs) doit trouver 2. Cf3.
    private static Puzzle CreateTwoPlyPuzzle() =>
        Puzzle.Create(
            fen: "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1",
            solutionMoves: new[] { "e7e5", "g1f3" },
            rating: 1200);

    [Fact]
    public void GetPlayablePosition_AppliesSetupMove()
    {
        var puzzle = Puzzle.Create(
            fen: "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            solutionMoves: new[] { "e2e4", "e7e5", "g1f3" },
            rating: 1200);
        var validator = new SolutionMoveValidator();

        var fen = validator.GetPlayablePosition(puzzle);

        fen.Should().StartWith("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR");
    }

    [Fact]
    public void ValidateMove_WithMatchingLegalMove_ReturnsCorrect()
    {
        var puzzle = CreateTwoPlyPuzzle();
        var validator = new SolutionMoveValidator();

        var result = validator.ValidateMove(puzzle, moveIndex: 1, submittedMove: "g1f3");

        result.IsLegal.Should().BeTrue();
        result.IsCorrect.Should().BeTrue();
        result.IsLastMove.Should().BeTrue();
        result.ResultingFen.Should().BeNull();
    }

    [Fact]
    public void ValidateMove_WithDifferentButLegalMove_ReturnsIncorrect()
    {
        var puzzle = CreateTwoPlyPuzzle();
        var validator = new SolutionMoveValidator();

        var result = validator.ValidateMove(puzzle, moveIndex: 1, submittedMove: "b1c3");

        result.IsLegal.Should().BeTrue();
        result.IsCorrect.Should().BeFalse();
    }

    [Fact]
    public void ValidateMove_WithIllegalMove_ReturnsIllegalAndIncorrect()
    {
        var puzzle = CreateTwoPlyPuzzle();
        var validator = new SolutionMoveValidator();

        // Le cavalier ne peut pas atteindre h5 en un coup depuis g1.
        var result = validator.ValidateMove(puzzle, moveIndex: 1, submittedMove: "g1h5");

        result.IsLegal.Should().BeFalse();
        result.IsCorrect.Should().BeFalse();
    }

    [Theory]
    [InlineData("zz99")]
    [InlineData("e2")]
    [InlineData("e2e4e5")]
    [InlineData("e2e4x")]
    [InlineData("")]
    public void ValidateMove_WithMalformedUci_ReturnsIllegalWithoutThrowing(string submittedMove)
    {
        var puzzle = CreateTwoPlyPuzzle();
        var validator = new SolutionMoveValidator();

        var result = validator.ValidateMove(puzzle, moveIndex: 1, submittedMove);

        result.IsLegal.Should().BeFalse();
        result.IsCorrect.Should().BeFalse();
    }

    [Fact]
    public void ValidateMove_IsCaseInsensitive()
    {
        var puzzle = CreateTwoPlyPuzzle();
        var validator = new SolutionMoveValidator();

        validator.ValidateMove(puzzle, moveIndex: 1, submittedMove: "G1F3").IsCorrect.Should().BeTrue();
    }

    [Fact]
    public void ValidateMove_WithOutOfRangeIndex_Throws()
    {
        var puzzle = CreateTwoPlyPuzzle();
        var validator = new SolutionMoveValidator();

        var act = () => validator.ValidateMove(puzzle, moveIndex: 99, submittedMove: "g1f3");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ValidateMove_WhenNotLastMove_ReturnsFenAfterAutomaticReply()
    {
        // Position après 1.e4 (trait aux Noirs) : ...e5 auto-jouée (indice 0), le joueur
        // (Blancs) doit trouver Cf3 (indice 1), puis ...Cc6 est auto-jouée (indice 2) avant
        // que le joueur ne doive trouver Fb5 (indice 3, dernier coup de la solution).
        var puzzle = Puzzle.Create(
            fen: "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1",
            solutionMoves: new[] { "e7e5", "g1f3", "b8c6", "f1b5" },
            rating: 1500);
        var validator = new SolutionMoveValidator();

        var result = validator.ValidateMove(puzzle, moveIndex: 1, submittedMove: "g1f3");

        result.IsCorrect.Should().BeTrue();
        result.IsLastMove.Should().BeFalse();
        result.ResultingFen.Should().NotBeNull();
        result.ResultingFen.Should().Contain("r1bqkbnr/pppp1ppp/2n5/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R");
    }

    [Fact]
    public void ValidateMove_WithPromotion_PromotesToRequestedPiece()
    {
        // Trait aux Noirs : ...Ra2-a1 (auto-joué) n'empêche pas a7a8=D pour les Blancs.
        var puzzle = Puzzle.Create(
            fen: "8/P7/8/8/8/8/k7/2K5 b - - 0 1",
            solutionMoves: new[] { "a2a1", "a7a8q" },
            rating: 1000);
        var validator = new SolutionMoveValidator();

        var result = validator.ValidateMove(puzzle, moveIndex: 1, submittedMove: "a7a8q");

        result.IsLegal.Should().BeTrue();
        result.IsCorrect.Should().BeTrue();
    }

    [Fact]
    public void ValidateMove_WithCastling_IsAcceptedInUciNotation()
    {
        // Trait aux Noirs : ...Ta8-b8 (auto-joué), puis les Blancs roquent (e1g1 en UCI).
        var puzzle = Puzzle.Create(
            fen: "r3k2r/8/8/8/8/8/8/R3K2R b KQkq - 0 1",
            solutionMoves: new[] { "a8b8", "e1g1" },
            rating: 1000);
        var validator = new SolutionMoveValidator();

        var result = validator.ValidateMove(puzzle, moveIndex: 1, submittedMove: "e1g1");

        result.IsLegal.Should().BeTrue();
        result.IsCorrect.Should().BeTrue();
    }

    [Fact]
    public void Create_WithSingleMove_Throws()
    {
        var act = () => Puzzle.Create("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", new[] { "e2e4" }, 1200);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateMove_OnRealLichessPuzzle_SolvesFullSequence()
    {
        // Puzzle Lichess réel (id 00008, cf. LichessPuzzleCsvParserTests) : 6 demi-coups,
        // avec captures. Vérifie l'enchaînement complet indice par indice, pas seulement
        // des positions synthétiques à un ou deux coups.
        var puzzle = Puzzle.Create(
            fen: "r6k/pp2r2p/4Rp1Q/3p4/8/1N1P2R1/PqP2bPP/7K b - - 0 24",
            solutionMoves: new[] { "f2g3", "e6e7", "b2b1", "b3c1", "b1c1", "h6c1" },
            rating: 1913);
        var validator = new SolutionMoveValidator();

        for (var moveIndex = 1; moveIndex < puzzle.SolutionMoves.Count; moveIndex += 2)
        {
            var result = validator.ValidateMove(puzzle, moveIndex, puzzle.SolutionMoves[moveIndex]);

            result.IsLegal.Should().BeTrue($"le coup à l'indice {moveIndex} doit être légal");
            result.IsCorrect.Should().BeTrue($"le coup à l'indice {moveIndex} doit correspondre à la solution");
        }
    }
}
