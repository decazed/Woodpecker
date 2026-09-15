namespace Woodpecker.Api.Contracts;

public record CreatePuzzleSetRequest(string Name, IReadOnlyList<Guid> PuzzleIds);

public record CreatePuzzleSetResponse(Guid Id);

public record CreatePuzzleSetFromTemplateRequest(string TemplateKey);
