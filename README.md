# Woodpecker

Entraînement tactique aux échecs selon la [Woodpecker Method](https://en.wikipedia.org/wiki/Woodpecker_Method) : résoudre un set fixe de puzzles, puis le refaire en cycles successifs, de plus en plus vite.

**Stack** : .NET 8, Clean Architecture, CQRS (MediatR + FluentValidation), EF Core + PostgreSQL, JWT, Blazor Server, Docker Compose, xUnit.

## Démarrer

```bash
cp .env.example .env
docker compose up -d --build
```

- Frontend : http://localhost:5090
- API + Swagger : http://localhost:5080/swagger

Au premier démarrage, l'API charge un catalogue de 450 puzzles Lichess (`src/Woodpecker.Api/Seed/puzzles.csv`, licence CC0). Il suffit ensuite de créer un compte, choisir un niveau, et jouer.

### Charger un catalogue plus gros (optionnel)

`POST /api/puzzles/import-csv` accepte l'export Lichess complet (`lichess_db_puzzle.csv`, décompressé), avec filtres `?minRating=&maxRating=&theme=`. Réservé au rôle `Admin` : promouvoir un compte avec `UPDATE "Users" SET "Role" = 'Admin' WHERE "Email" = '...'` en base, puis appeler l'endpoint depuis Swagger avec son token.

## Tests

```bash
dotnet test
```

## Structure

```
src/
  Woodpecker.Domain          # Entités et règles métier
  Woodpecker.Application     # Cas d'usage (CQRS), abstractions
  Woodpecker.Infrastructure  # EF Core, JWT, moteur d'échecs, parsing CSV Lichess
  Woodpecker.Api             # Contrôleurs, contrats, middleware
  Woodpecker.Web             # Frontend Blazor Server
tests/                       # Un projet de tests par couche
```
