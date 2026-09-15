namespace Woodpecker.Api.Tests;

// Un seul WebApplicationFactory pour tous les tests d'intégration de l'assembly.
// Program.cs utilise un ReloadableLogger Serilog statique (Log.Logger) : construire
// plusieurs hosts en parallèle (un par classe de test) le "gèle" après la première
// construction et fait planter les suivantes ("The logger is already frozen").
[CollectionDefinition("Api")]
public class ApiTestCollection : ICollectionFixture<WoodpeckerApiFactory>;
