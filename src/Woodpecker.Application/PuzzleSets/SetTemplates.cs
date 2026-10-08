using MediatR;
using Microsoft.EntityFrameworkCore;
using Woodpecker.Application.Abstractions;

namespace Woodpecker.Application.PuzzleSets;

// Sets "prêts à l'emploi" : une tranche de rating et un nombre de puzzles, tirés au hasard
// dans le catalogue au moment où l'utilisateur clique. Définis en dur plutôt qu'en base :
// trois niveaux suffisent et ça évite une table + un écran d'admin pour les gérer.
public record SetTemplate(string Key, string Name, string Description, int MinRating, int MaxRating, int PuzzleCount);

public static class SetTemplates
{
    // Nombre de puzzles proposé à l'utilisateur : 10, 20, ... 150 (150 = taille d'une tranche du catalogue seed).
    public const int PuzzleCountStep = 10;
    public const int MaxPuzzleCount = 150;

    public static readonly IReadOnlyList<int> PuzzleCountChoices =
        Enumerable.Range(1, MaxPuzzleCount / PuzzleCountStep).Select(i => i * PuzzleCountStep).ToList();

    public static readonly IReadOnlyList<SetTemplate> All =
    [
        new("debutant", "Débutant", "Mats en un coup et tactiques simples, jusqu'à 1300.", 0, 1299, 10),
        new("intermediaire", "Intermédiaire", "Fourches, clouages, combinaisons courtes, 1300 à 1700.", 1300, 1699, 10),
        new("avance", "Avancé", "Combinaisons plus profondes, à partir de 1700.", 1700, int.MaxValue, 10),
    ];

    public static SetTemplate? Find(string key) =>
        All.FirstOrDefault(t => string.Equals(t.Key, key, StringComparison.OrdinalIgnoreCase));
}

// PuzzleCount est le nombre par défaut ; PuzzleCountChoices liste les valeurs proposées à l'utilisateur.
// AvailablePuzzleCount permet au client de griser un template si le catalogue est trop petit.
public record SetTemplateDto(
    string Key,
    string Name,
    string Description,
    int PuzzleCount,
    int AvailablePuzzleCount,
    IReadOnlyList<int> PuzzleCountChoices);

public record GetSetTemplatesQuery : IRequest<IReadOnlyList<SetTemplateDto>>;

public class GetSetTemplatesHandler(IApplicationDbContext context)
    : IRequestHandler<GetSetTemplatesQuery, IReadOnlyList<SetTemplateDto>>
{
    public async Task<IReadOnlyList<SetTemplateDto>> Handle(GetSetTemplatesQuery request, CancellationToken cancellationToken)
    {
        var ratings = await context.Puzzles.Select(p => p.Rating).ToListAsync(cancellationToken);

        return SetTemplates.All
            .Select(t => new SetTemplateDto(
                t.Key,
                t.Name,
                t.Description,
                t.PuzzleCount,
                ratings.Count(r => r >= t.MinRating && r <= t.MaxRating),
                SetTemplates.PuzzleCountChoices))
            .ToList();
    }
}
