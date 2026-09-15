using MediatR;
using Microsoft.EntityFrameworkCore;
using Woodpecker.Application.Abstractions;
using Woodpecker.Application.Common.Exceptions;

namespace Woodpecker.Application.PuzzleSets;

public record PuzzleSetDto(Guid Id, string Name, IReadOnlyList<Guid> PuzzleIds);

public record GetPuzzleSetByIdQuery(Guid Id, Guid UserId) : IRequest<PuzzleSetDto>;

public class GetPuzzleSetByIdHandler(IApplicationDbContext context) : IRequestHandler<GetPuzzleSetByIdQuery, PuzzleSetDto>
{
    public async Task<PuzzleSetDto> Handle(GetPuzzleSetByIdQuery request, CancellationToken cancellationToken)
    {
        // Include("_items") explicite : PuzzleIds est calculé depuis le champ privé _items,
        // qu'EF ne charge pas automatiquement sur un DbContext "frais" (pas d'eager loading
        // par défaut). Sans ça, PuzzleIds reviendrait vide sur une requête HTTP normale.
        var puzzleSet = await context.PuzzleSets
            .Include("_items")
            .FirstOrDefaultAsync(ps => ps.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Set {request.Id} introuvable.");

        // Un set appartient à un seul utilisateur : on renvoie 404 (pas 403) à qui n'en
        // est pas propriétaire, pour ne pas révéler qu'un set portant cet Id existe.
        if (puzzleSet.OwnerId != request.UserId)
            throw new NotFoundException($"Set {request.Id} introuvable.");

        return new PuzzleSetDto(puzzleSet.Id, puzzleSet.Name, puzzleSet.PuzzleIds);
    }
}
