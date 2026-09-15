using Woodpecker.Application.Puzzles;

namespace Woodpecker.Application.Abstractions;

public record LichessPuzzleImportFilter(int? MinRating, int? MaxRating, string? Theme);

public interface ILichessPuzzleCsvParser
{
    // Format Lichess puzzle DB : PuzzleId,FEN,Moves,Rating,RatingDeviation,Popularity,NbPlays,Themes,GameUrl,OpeningTags
    // (cf. https://database.lichess.org/#puzzles, open data).
    IReadOnlyList<PuzzleImportItem> Parse(Stream csvStream, LichessPuzzleImportFilter filter);
}
