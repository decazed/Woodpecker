using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Woodpecker.Application.Abstractions;
using Woodpecker.Application.Common.Exceptions;
using Woodpecker.Domain;

namespace Woodpecker.Application.PuzzleSets;

// PuzzleCount à null : on prend le nombre par défaut du modèle.
public record CreatePuzzleSetFromTemplateCommand(string TemplateKey, Guid UserId, int? PuzzleCount = null) : IRequest<Guid>;

public class CreatePuzzleSetFromTemplateValidator : AbstractValidator<CreatePuzzleSetFromTemplateCommand>
{
    public CreatePuzzleSetFromTemplateValidator()
    {
        RuleFor(c => c.TemplateKey).NotEmpty();
        RuleFor(c => c.UserId).NotEqual(Guid.Empty);
        // Une valeur absente (null) est valide, on prendra le défaut du modèle.
        RuleFor(c => c.PuzzleCount!.Value)
            .Must(n => SetTemplates.PuzzleCountChoices.Contains(n))
            .When(c => c.PuzzleCount is not null)
            .WithMessage("Le nombre de puzzles doit être un multiple de 10 entre 10 et 150.");
    }
}

public class CreatePuzzleSetFromTemplateHandler(IApplicationDbContext context)
    : IRequestHandler<CreatePuzzleSetFromTemplateCommand, Guid>
{
    public async Task<Guid> Handle(CreatePuzzleSetFromTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = SetTemplates.Find(request.TemplateKey)
            ?? throw new NotFoundException($"Modèle de set '{request.TemplateKey}' introuvable.");

        var puzzleCount = request.PuzzleCount ?? template.PuzzleCount;

        var candidateIds = await context.Puzzles
            .Where(p => p.Rating >= template.MinRating && p.Rating <= template.MaxRating)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (candidateIds.Count < puzzleCount)
            throw new InvalidOperationException(
                $"Pas assez de puzzles dans le catalogue pour le niveau {template.Name} ({candidateIds.Count}/{puzzleCount}).");

        // Tirage en mémoire plutôt qu'ORDER BY random() : le catalogue reste petit et ça
        // évite une traduction SQL dépendante du provider (InMemory dans les tests).
        var picked = candidateIds
            .OrderBy(_ => Random.Shared.Next())
            .Take(puzzleCount)
            .ToList();

        var puzzleSet = PuzzleSet.Create(template.Name, request.UserId, picked);
        context.PuzzleSets.Add(puzzleSet);
        await context.SaveChangesAsync(cancellationToken);

        return puzzleSet.Id;
    }
}
