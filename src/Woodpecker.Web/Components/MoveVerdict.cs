namespace Woodpecker.Web.Components;

// Verdict du serveur sur le dernier coup joué, affiché en badge sur la case d'arrivée.
public record MoveVerdict(string Square, bool IsCorrect);
