using Woodpecker.Domain;

namespace Woodpecker.Application.Abstractions;

// IsLegal : le coup respecte les règles d'échecs dans la position courante (indépendamment
// de la solution attendue). IsCorrect : en plus d'être légal, c'est le coup attendu par
// Puzzle.SolutionMoves[moveIndex]. ResultingFen : renseigné uniquement quand IsCorrect est
// vrai et qu'il reste un coup adverse à jouer (SolutionMoves[moveIndex + 1]) ; c'est la
// position après ce coup adverse automatique, que le client n'a plus qu'à réafficher (pas
// besoin de logique d'échecs côté Blazor).
public record MoveValidationResult(bool IsLegal, bool IsCorrect, bool IsLastMove, string? ResultingFen);

public interface IMoveValidator
{
    // Position à partir de laquelle le joueur doit trouver son premier coup : le format
    // Lichess place en SolutionMoves[0] le coup de mise en place joué par l'adversaire,
    // qu'il faut appliquer automatiquement au FEN brut du puzzle avant de l'afficher.
    string GetPlayablePosition(Puzzle puzzle);

    MoveValidationResult ValidateMove(Puzzle puzzle, int moveIndex, string submittedMove);
}
