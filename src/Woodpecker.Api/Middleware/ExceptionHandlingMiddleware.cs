using System.Net;
using FluentValidation;
using Woodpecker.Application.Common.Exceptions;

namespace Woodpecker.Api.Middleware;

// Traduit les exceptions du domaine/application en réponses HTTP cohérentes (ProblemDetails),
// pour ne pas avoir à faire de try/catch dans chaque contrôleur.
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Échec de validation");
            await WriteValidationProblemAsync(context, ex);
        }
        catch (NotFoundException ex)
        {
            logger.LogWarning(ex, "Ressource introuvable");
            await WriteProblemAsync(context, HttpStatusCode.NotFound, "Ressource introuvable", ex.Message);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Argument invalide");
            await WriteProblemAsync(context, HttpStatusCode.BadRequest, "Requête invalide", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Opération invalide");
            await WriteProblemAsync(context, HttpStatusCode.Conflict, "Opération invalide", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Accès refusé");
            await WriteProblemAsync(context, HttpStatusCode.Unauthorized, "Non autorisé", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur non gérée");
            await WriteProblemAsync(context, HttpStatusCode.InternalServerError, "Erreur interne", "Une erreur inattendue est survenue.");
        }
    }

    private static Task WriteProblemAsync(HttpContext context, HttpStatusCode status, string title, string detail)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)status;

        var problem = new
        {
            title,
            status = (int)status,
            detail,
        };

        return context.Response.WriteAsJsonAsync(problem);
    }

    // Format ValidationProblemDetails standard (errors: { champ: [messages] }) plutôt qu'un
    // message texte unique, pour que le client puisse afficher l'erreur sous le bon champ.
    private static Task WriteValidationProblemAsync(HttpContext context, ValidationException ex)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        var problem = new
        {
            title = "Requête invalide",
            status = (int)HttpStatusCode.BadRequest,
            errors,
        };

        return context.Response.WriteAsJsonAsync(problem);
    }
}
