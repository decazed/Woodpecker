using Chess;
using Woodpecker.Application.Abstractions;
using Woodpecker.Domain;

namespace Woodpecker.Infrastructure;

// Implémentation basée sur Gera.Chess (licence MIT, cible net6.0+) : rejoue la partie coup
// par coup depuis Puzzle.Fen pour disposer d'un vrai plateau et vérifier la légalité des
// coups, pas seulement comparer des chaînes. Un ChessBoard est reconstruit à chaque appel
// (pas de dépendance singleton mutable) : la classe est sans état, donc thread-safe.
public class SolutionMoveValidator : IMoveValidator
{
    public string GetPlayablePosition(Puzzle puzzle)
    {
        var board = ChessBoard.LoadFromFen(puzzle.Fen);
        ApplyMove(board, puzzle.SolutionMoves[0]);
        return board.ToFen();
    }

    public MoveValidationResult ValidateMove(Puzzle puzzle, int moveIndex, string submittedMove)
    {
        if (moveIndex < 0 || moveIndex >= puzzle.SolutionMoves.Count)
            throw new ArgumentOutOfRangeException(nameof(moveIndex), "L'indice de coup est hors limites.");

        var board = ChessBoard.LoadFromFen(puzzle.Fen);
        for (var i = 0; i < moveIndex; i++)
            ApplyMove(board, puzzle.SolutionMoves[i]);

        var isLastMove = moveIndex == puzzle.SolutionMoves.Count - 1;

        Move move;
        try
        {
            move = ParseUciMove(submittedMove);
        }
        catch (FormatException)
        {
            return new MoveValidationResult(IsLegal: false, IsCorrect: false, isLastMove, ResultingFen: null);
        }

        if (!IsLegal(board, move))
            return new MoveValidationResult(IsLegal: false, IsCorrect: false, isLastMove, ResultingFen: null);

        var isCorrect = string.Equals(
            NormalizeUci(submittedMove),
            puzzle.SolutionMoves[moveIndex],
            StringComparison.OrdinalIgnoreCase);

        if (!isCorrect)
            return new MoveValidationResult(IsLegal: true, IsCorrect: false, isLastMove, ResultingFen: null);

        ApplyMove(board, submittedMove);

        string? resultingFen = null;
        if (!isLastMove)
        {
            ApplyMove(board, puzzle.SolutionMoves[moveIndex + 1]);
            resultingFen = board.ToFen();
        }

        return new MoveValidationResult(IsLegal: true, IsCorrect: true, isLastMove, resultingFen);
    }

    // IsValidMove ne renvoie pas toujours false : il lève ChessException quand la case de
    // départ est vide (ou pas au trait). Pour un coup soumis par un client, c'est un simple
    // coup illégal, pas une erreur serveur.
    private static bool IsLegal(ChessBoard board, Move move)
    {
        try
        {
            return board.IsValidMove(move);
        }
        catch (ChessException)
        {
            return false;
        }
    }

    // Applique un coup connu pour être légal (coup de la solution, ou coup déjà validé par
    // IsValidMove) : toute exception ici signalerait des données de puzzle incohérentes.
    private static void ApplyMove(ChessBoard board, string uci)
    {
        var move = ParseUciMove(uci);
        var promotion = ParsePromotion(uci);

        void OnPromotePawn(object? sender, PromotionEventArgs e) => e.PromotionResult = promotion;

        board.OnPromotePawn += OnPromotePawn;
        try
        {
            board.Move(move);
        }
        finally
        {
            board.OnPromotePawn -= OnPromotePawn;
        }
    }

    // Format UCI : deux cases ("e2e4") avec éventuellement une lettre de promotion ("e7e8q").
    // Gera.Chess accepte aussi bien "e1g1" (roque façon UCI) que "e1h1" (roque façon
    // "roi vers la tour") : le format Lichess utilise systématiquement la première forme.
    private static Move ParseUciMove(string uci)
    {
        if (!UciMoveRegex.IsMatch(uci))
            throw new FormatException($"Coup UCI invalide : '{uci}'.");

        return new Move(uci[..2], uci.Substring(2, 2));
    }

    private static readonly System.Text.RegularExpressions.Regex UciMoveRegex =
        new("^[a-hA-H][1-8][a-hA-H][1-8][qrbnQRBN]?$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static string NormalizeUci(string uci) => uci.Trim().ToLowerInvariant();

    private static PromotionType ParsePromotion(string uci) => uci.Length == 5
        ? char.ToLowerInvariant(uci[4]) switch
        {
            'q' => PromotionType.ToQueen,
            'r' => PromotionType.ToRook,
            'b' => PromotionType.ToBishop,
            'n' => PromotionType.ToKnight,
            _ => PromotionType.Default,
        }
        : PromotionType.Default;
}
