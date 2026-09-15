using FluentValidation;
using MediatR;

namespace Woodpecker.Application.Common.Behaviors;

// S'exécute avant chaque handler MediatR. S'il existe un ou plusieurs IValidator<TRequest>
// pour la requête en cours, ils sont tous exécutés ; en cas d'échec, on lève avant même
// d'atteindre le handler -> les handlers n'ont jamais à revalider leurs propres entrées.
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var failures = new List<FluentValidation.Results.ValidationFailure>();

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            failures.AddRange(result.Errors);
        }

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}
