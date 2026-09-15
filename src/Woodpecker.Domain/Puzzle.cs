
namespace Woodpecker.Domain;

public class Puzzle : Entity
{
    public string Fen { get; private set; }
    public IReadOnlyList<string> SolutionMoves { get; private set; }
    public int Rating { get; private set; }

    private Puzzle(Guid id, string fen, IReadOnlyList<string> solutionMoves, int rating) : base(id)
    {
        Fen = fen;
        SolutionMoves = solutionMoves;
        Rating = rating;
    }

    public static Puzzle Create(string fen, IReadOnlyList<string> solutionMoves, int rating)
    {
        if (string.IsNullOrWhiteSpace(fen))
            throw new ArgumentException("Le FEN ne peut pas être vide.", nameof(fen));

        // Format Lichess : le premier coup est la mise en place jouée automatiquement par
        // l'adversaire, il faut donc au moins un coup de plus à trouver pour le joueur.
        if (solutionMoves == null || solutionMoves.Count < 2)
            throw new ArgumentException(
                "La solution doit contenir au moins deux coups (le coup de mise en place adverse, puis au moins un coup à trouver).",
                nameof(solutionMoves));

        if (rating <= 0)
            throw new ArgumentException("Le rating doit être strictement positif.", nameof(rating));

        return new Puzzle(Guid.NewGuid(), fen, solutionMoves, rating);
    }
}
