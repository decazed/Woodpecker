using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Woodpecker.Application.Abstractions;
using Woodpecker.Application.Puzzles;

namespace Woodpecker.Infrastructure.Puzzles;

public class LichessPuzzleCsvParser : ILichessPuzzleCsvParser
{
    public IReadOnlyList<PuzzleImportItem> Parse(Stream csvStream, LichessPuzzleImportFilter filter)
    {
        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

        var items = new List<PuzzleImportItem>();

        foreach (var record in csv.GetRecords<LichessPuzzleRow>())
        {
            if (filter.MinRating is not null && record.Rating < filter.MinRating)
                continue;

            if (filter.MaxRating is not null && record.Rating > filter.MaxRating)
                continue;

            if (filter.Theme is not null && !record.Themes.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Contains(filter.Theme, StringComparer.OrdinalIgnoreCase))
                continue;

            var moves = record.Moves.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            items.Add(new PuzzleImportItem(record.FEN, moves, record.Rating));
        }

        return items;
    }

    // Une ligne du CSV Lichess. Seules les colonnes qui nous intéressent sont déclarées ;
    // CsvHelper ignore le reste par défaut (RatingDeviation, Popularity, GameUrl, etc.).
    private class LichessPuzzleRow
    {
        public string FEN { get; set; } = string.Empty;
        public string Moves { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Themes { get; set; } = string.Empty;
    }
}
