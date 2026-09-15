using MediatR;
using Woodpecker.Application.Abstractions;
using Woodpecker.Application.Common.Exceptions;

namespace Woodpecker.Application.Puzzles;

// Ne contient volontairement pas SolutionMoves : ne jamais exposer la solution au client
// avant qu'il ait soumis une tentative (SubmitPuzzleAttemptCommand fait la comparaison
// côté serveur). PlayableFen est la position après le coup de mise en place automatique
// (SolutionMoves[0]) : c'est elle que le client doit afficher, pas Fen brut.
public record PuzzleDto(Guid Id, string Fen, string PlayableFen, int Rating);

public record GetPuzzleByIdQuery(Guid Id) : IRequest<PuzzleDto>;

public class GetPuzzleByIdHandler(IApplicationDbContext context, IMoveValidator moveValidator)
    : IRequestHandler<GetPuzzleByIdQuery, PuzzleDto>
{
    public async Task<PuzzleDto> Handle(GetPuzzleByIdQuery request, CancellationToken cancellationToken)
    {
        var puzzle = await context.Puzzles.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException($"Puzzle {request.Id} introuvable.");

        var playableFen = moveValidator.GetPlayablePosition(puzzle);

        return new PuzzleDto(puzzle.Id, puzzle.Fen, playableFen, puzzle.Rating);
    }
}
