namespace Woodpecker.Application.Common.Exceptions;

// Recouvre aussi bien "la ressource n'existe pas" que "la ressource existe mais
// n'appartient pas à l'utilisateur courant" : on ne distingue pas les deux côté HTTP
// (404 dans les deux cas) pour ne pas révéler l'existence d'une ressource à un tiers.
public class NotFoundException(string message) : Exception(message);
