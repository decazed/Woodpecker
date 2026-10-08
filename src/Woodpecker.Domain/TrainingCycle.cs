namespace Woodpecker.Domain;

public class TrainingCycle : Entity
{
    private readonly List<PuzzleAttempt> _attempts = new();

    public Guid PuzzleSetId { get; }
    public Guid UserId { get; }
    public int CycleNumber { get; }
    public DateTime StartedAt { get; }
    public DateTime? CompletedAt { get; private set; }
    public bool IsCompleted => CompletedAt.HasValue;
    public DateTime? AbandonedAt { get; private set; }
    public bool IsAbandoned => AbandonedAt.HasValue;

    // En cours = ni terminé, ni abandonné : c'est le seul état qui bloque le démarrage
    // d'un nouveau cycle sur le même set.
    public bool IsActive => !IsCompleted && !IsAbandoned;
    public IReadOnlyList<PuzzleAttempt> Attempts => _attempts.AsReadOnly();

    private TrainingCycle(Guid id, Guid puzzleSetId, Guid userId, int cycleNumber, DateTime startedAt)
        : base(id)
    {
        PuzzleSetId = puzzleSetId;
        UserId = userId;
        CycleNumber = cycleNumber;
        StartedAt = startedAt;
    }

    public static TrainingCycle Start(Guid puzzleSetId, Guid userId, int cycleNumber, DateTime startedAt)
    {
        if (cycleNumber <= 0)
            throw new ArgumentException("Le numéro de cycle doit être strictement positif.", nameof(cycleNumber));

        return new TrainingCycle(Guid.NewGuid(), puzzleSetId, userId, cycleNumber, startedAt);
    }

    public void RecordAttempt(Guid puzzleId, bool isSuccess, TimeSpan duration, DateTime attemptedAt)
    {
        if (!IsActive)
            throw new InvalidOperationException("Impossible d'enregistrer une tentative sur un cycle clôturé ou abandonné.");

        _attempts.Add(PuzzleAttempt.Create(Id, puzzleId, isSuccess, duration, attemptedAt));
    }

    // expectedPuzzleCount vient du PuzzleSet associé : TrainingCycle ne connaît PuzzleSet
    // que par son Id (pas de référence directe), donc c'est à l'appelant (couche Application)
    // de fournir le nombre de puzzles attendus.
    public void Complete(int expectedPuzzleCount, DateTime completedAt)
    {
        if (!IsActive)
            throw new InvalidOperationException("Ce cycle est déjà clôturé ou abandonné.");

        var distinctPuzzlesAttempted = _attempts.Select(a => a.PuzzleId).Distinct().Count();
        if (distinctPuzzlesAttempted < expectedPuzzleCount)
            throw new InvalidOperationException(
                "Tous les puzzles du set doivent avoir été tentés avant de clôturer le cycle.");

        CompletedAt = completedAt;
    }

    // Abandon : le cycle reste en base (historique) mais n'est plus "en cours" et est exclu des
    // statistiques de progression, puisqu'il n'a pas été mené à terme.
    public void Abandon(DateTime abandonedAt)
    {
        if (!IsActive)
            throw new InvalidOperationException("Seul un cycle en cours peut être abandonné.");

        AbandonedAt = abandonedAt;
    }
}
