using FluentValidation;
using MediatR;
using Woodpecker.Application.Abstractions;
using Woodpecker.Domain;

namespace Woodpecker.Application.PuzzleSets;

public record CreatePuzzleSetCommand(string Name, Guid OwnerId, IReadOnlyList<Guid> PuzzleIds) : IRequest<Guid>;

public class CreatePuzzleSetValidator : AbstractValidator<CreatePuzzleSetCommand>
{
    public CreatePuzzleSetValidator()
    {
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.OwnerId).NotEqual(Guid.Empty);
        RuleFor(c => c.PuzzleIds).NotEmpty();
    }
}

public class CreatePuzzleSetHandler(IApplicationDbContext context) : IRequestHandler<CreatePuzzleSetCommand, Guid>
{
    public async Task<Guid> Handle(CreatePuzzleSetCommand request, CancellationToken cancellationToken)
    {
        var puzzleSet = PuzzleSet.Create(request.Name, request.OwnerId, request.PuzzleIds);

        context.PuzzleSets.Add(puzzleSet);
        await context.SaveChangesAsync(cancellationToken);

        return puzzleSet.Id;
    }
}
