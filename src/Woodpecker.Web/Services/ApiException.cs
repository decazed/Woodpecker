using System.Net;

namespace Woodpecker.Web.Services;

// Remplace EnsureSuccessStatusCode : porte le message métier renvoyé par l'API (ProblemDetails
// "detail" ou "title") pour l'afficher tel quel, plutôt qu'un "400 Bad Request" opaque. Les
// pages doivent l'attraper : une exception non gérée dans un handler d'événement Blazor
// Server tue tout le circuit (page figée), pas seulement l'action en cours.
public class ApiException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
