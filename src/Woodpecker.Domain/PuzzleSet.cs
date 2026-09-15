namespace Woodpecker.Domain;

public class PuzzleSet : Entity
{
    private readonly List<PuzzleSetItem> _items = new();

    public string Name { get; }
    public Guid OwnerId { get; }

    // Un PuzzleSet référence des puzzles par Id plutôt que par objet : les puzzles sont
    // un catalogue partagé (importé une fois depuis la base Lichess), potentiellement
    // réutilisé dans plusieurs sets, donc ce n'est pas PuzzleSet qui en est propriétaire.
    // L'ordre est préservé via PuzzleSetItem.Position.
    public IReadOnlyList<Guid> PuzzleIds =>
        _items.OrderBy(i => i.Position).Select(i => i.PuzzleId).ToList();

    private PuzzleSet(Guid id, string name, Guid ownerId) : base(id)
    {
        Name = name;
        OwnerId = ownerId;
    }

    // La liste de puzzles est figée à la création : un set est par définition une
    // collection fixe (cf. méthode Woodpecker), il n'y a volontairement pas de
    // AddPuzzle/RemovePuzzle après coup.
    //
    // Le constructeur ne prend pas puzzleIds directement : EF Core doit pouvoir lier
    // chaque paramètre de constructeur à une propriété scalaire mappée, or _items est
    // une vraie relation (table PuzzleSetItems), pas une colonne. On construit donc
    // l'objet puis on peuple _items ensuite, comme pour TrainingCycle.RecordAttempt.
    public static PuzzleSet Create(string name, Guid ownerId, IEnumerable<Guid> puzzleIds)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Le nom du set ne peut pas être vide.", nameof(name));

        var ids = puzzleIds.ToList();
        if (ids.Count == 0)
            throw new ArgumentException("Un set doit contenir au moins un puzzle.", nameof(puzzleIds));

        if (ids.Distinct().Count() != ids.Count)
            throw new ArgumentException("Un set ne peut pas contenir le même puzzle en double.", nameof(puzzleIds));

        var puzzleSet = new PuzzleSet(Guid.NewGuid(), name, ownerId);
        puzzleSet._items.AddRange(ids.Select((puzzleId, index) => PuzzleSetItem.Create(puzzleId, index)));

        return puzzleSet;
    }
}
