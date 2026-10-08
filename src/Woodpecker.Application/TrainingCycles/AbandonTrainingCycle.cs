using FluentValidation;
using MediatR;
using Woodpecker.Application.Abstractions;
using Woodpecker.Application.Common.Exceptions;

namespace Woodpecker.Application.TrainingCycles;

public record AbandonTrainingCycleCommand(Guid CycleId, Guid UserId) : IRequest;

public class AbandonTrainingCycleValidator : AbstractValidator<AbandonTrainingCycleCommand>
{
    public AbandonTrainingCycleValidator()
    {
        RuleFor(c => c.CycleId).NotEqual(Guid.Empty);
        RuleFor(c => c.UserId).NotEqual(Guid.Empty);
    }
}

public class AbandonTrainingCycleHandler(IApplicationDbContext context)
    : IRequestHandler<AbandonTrainingCycleCommand>
{
    public async Task Handle(AbandonTrainingCycleCommand request, CancellationToken cancellationToken)
    {
        var cycle = await context.TrainingCycles.FindAsync([request.CycleId], cancellationToken)
            ?? throw new NotFoundException($"Cycle {request.CycleId} introuvable.");

        if (cycle.UserId != request.UserId)
            throw new NotFoundException($"Cycle {request.CycleId} introuvable.");

        cycle.Abandon(DateTime.UtcNow);
        await context.SaveChangesAsync(cancellationToken);
    }
}
