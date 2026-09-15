namespace Woodpecker.Domain;

// Entrée de la table de jointure PuzzleSet <-> Puzzle. Pas d'Id propre (n'hérite pas
// d'Entity) : son identité, c'est la combinaison (PuzzleSet, PuzzleId), une clé composite
// classique pour une table de jointure. Position préserve l'ordre des puzzles dans le set.
public class PuzzleSetItem
{
    public Guid PuzzleId { get; }
    public int Position { get; }

    private PuzzleSetItem(Guid puzzleId, int position)
    {
        PuzzleId = puzzleId;
        Position = position;
    }

    internal static PuzzleSetItem Create(Guid puzzleId, int position) => new(puzzleId, position);
}
