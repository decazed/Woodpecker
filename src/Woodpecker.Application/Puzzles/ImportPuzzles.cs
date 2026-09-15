using FluentValidation;
using MediatR;
using Woodpecker.Application.Abstractions;
using Woodpecker.Domain;

namespace Woodpecker.Application.Puzzles;

public record PuzzleImportItem(string Fen, IReadOnlyList<string> SolutionMoves, int Rating);

public record ImportPuzzlesCommand(IReadOnlyList<PuzzleImportItem> Puzzles) : IRequest<IReadOnlyList<Guid>>;

public class ImportPuzzlesValidator : AbstractValidator<ImportPuzzlesCommand>
{
    public ImportPuzzlesValidator()
    {
        RuleFor(c => c.Puzzles).NotEmpty();

        RuleForEach(c => c.Puzzles).ChildRules(item =>
        {
            item.RuleFor(p => p.Fen).NotEmpty();
            item.RuleFor(p => p.SolutionMoves)
                .Must(moves => moves is { Count: >= 2 })
                .WithMessage("La solution doit contenir au moins deux coups (mise en place adverse + au moins un coup à trouver).");
            item.RuleFor(p => p.Rating).GreaterThan(0);
        });
    }
}

public class ImportPuzzlesHandler(IApplicationDbContext context)
    : IRequestHandler<ImportPuzzlesCommand, IReadOnlyList<Guid>>
{
    public async Task<IReadOnlyList<Guid>> Handle(ImportPuzzlesCommand request, CancellationToken cancellationToken)
    {
        var puzzles = request.Puzzles
            .Select(item => Puzzle.Create(item.Fen, item.SolutionMoves, item.Rating))
            .ToList();

        context.Puzzles.AddRange(puzzles);
        await context.SaveChangesAsync(cancellationToken);

        return puzzles.Select(p => p.Id).ToList();
    }
}
