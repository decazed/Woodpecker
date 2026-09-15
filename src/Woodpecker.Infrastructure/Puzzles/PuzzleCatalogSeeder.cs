using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Woodpecker.Application.Abstractions;
using Woodpecker.Domain;

namespace Woodpecker.Infrastructure.Puzzles;

// Initialise le catalogue partagé au premier démarrage depuis un CSV Lichess embarqué :
// l'application est utilisable sans compte admin ni import manuel. Ne fait rien si des
// puzzles existent déjà (un catalogue chargé via /api/puzzles/import-csv n'est jamais écrasé).
public class PuzzleCatalogSeeder(
    WoodpeckerDbContext context,
    ILichessPuzzleCsvParser parser,
    ILogger<PuzzleCatalogSeeder> logger)
{
    public async Task SeedIfEmptyAsync(string csvPath, CancellationToken cancellationToken = default)
    {
        if (await context.Puzzles.AnyAsync(cancellationToken))
            return;

        if (!File.Exists(csvPath))
        {
            logger.LogWarning("Fichier de puzzles initial introuvable ({Path}) : catalogue laissé vide", csvPath);
            return;
        }

        await using var stream = File.OpenRead(csvPath);
        var items = parser.Parse(stream, new LichessPuzzleImportFilter(null, null, null));

        context.Puzzles.AddRange(items.Select(i => Puzzle.Create(i.Fen, i.SolutionMoves, i.Rating)));
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Catalogue initialisé avec {Count} puzzles depuis {Path}", items.Count, csvPath);
    }
}
