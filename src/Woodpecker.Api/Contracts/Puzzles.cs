namespace Woodpecker.Api.Contracts;

public record ImportPuzzlesRequest(IReadOnlyList<ImportPuzzleItemRequest> Puzzles);

public record ImportPuzzleItemRequest(string Fen, IReadOnlyList<string> SolutionMoves, int Rating);

public record ImportPuzzlesResponse(IReadOnlyList<Guid> Ids);
