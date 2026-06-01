# CLAUDE.md

This file provides guidance to Claude Code when working with code in this repository.

## Project

`personalisation-engine` is a .NET 8 / C# personalisation and recommendation engine built for a 4-week learning programme. It is set in an iGaming context: mock player profiles are consumed by a rules engine that applies responsible gaming (RG) guardrails, and if safe, the Claude API generates personalised copy. Results are surfaced through a React/Vite admin UI.

**Stack**: .NET 8, ASP.NET Core, Entity Framework Core 8, PostgreSQL 16 (port 5433), Swashbuckle (Swagger), Serilog, xUnit, Moq — React 19 / TypeScript / Vite frontend.

## Key commands

```bash
# Start local PostgreSQL (port 5433 — avoids clash with other projects on 5432)
docker-compose up -d

# Run API on http://localhost:5080 (auto-migrates on startup in Development)
dotnet run --project src/PersonalisationEngine.Api

# Run all tests (47 tests — unit tests use in-memory DB; integration tests spin up PostgreSQL via Testcontainers)
dotnet test

# Run tests with coverage report (excludes migrations; requires Docker for Testcontainers)
dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory /tmp/pe-coverage
# Current: 100% line coverage, 90.9% branch coverage

# Frontend dev server on http://localhost:5173 (proxies /api → :5080)
cd frontend && npm install && npm run dev

# Generate a new EF Core migration
export PATH="$PATH:$HOME/.dotnet/tools"
dotnet ef migrations add <MigrationName> --project src/PersonalisationEngine.Api

# Apply migrations manually (also runs automatically on startup)
dotnet ef database update --project src/PersonalisationEngine.Api \
  -- --ConnectionStrings:DefaultConnection="Host=localhost;Port=5433;Database=personalisationengine;Username=persengine;Password=change-me"
```

## Project structure

```
src/PersonalisationEngine.Api/
├── Program.cs                         # DI wiring, Serilog, middleware pipeline, startup migration
├── appsettings.json                   # Config defaults (model, max tokens)
├── appsettings.Development.json       # Dev overrides (CORS, log levels)
├── .env.local                         # Local secrets — NOT committed (see .env.example)
├── Controllers/
│   ├── PlayersController.cs           # GET/POST/PUT/DELETE /api/players
│   └── RecommendationsController.cs   # POST /api/recommendations/{playerId}
├── Services/
│   ├── PlayerService.cs               # Player CRUD against EF Core
│   ├── RecommendationService.cs       # Orchestrator: rules → Claude → persist
│   ├── Rules/
│   │   └── RulesEngine.cs             # 4 guardrail checks (self-excluded, cooling-off, high-risk, inactive)
│   └── Claude/
│       └── ClaudeClient.cs            # Typed HttpClient to Anthropic API; stub mode if no key
├── Data/
│   ├── AppDbContext.cs                # EF Core DbContext (Players, Recommendations)
│   └── Migrations/                    # InitialCreate → SeedPlayers → AddRecommendations
├── Models/
│   ├── Player.cs                      # Player entity + RiskLevel enum
│   └── Recommendation.cs              # Recommendation entity (rules result + Claude copy)
├── DTOs/
│   ├── Players/                       # PlayerRequest, PlayerResponse
│   └── Recommendations/               # RulesResult, RecommendationResponse, ClaudeRecommendation
└── Middleware/
    └── GlobalExceptionHandler.cs      # Exception → HTTP status mapping; custom exception types

tests/PersonalisationEngine.Tests/
├── Infrastructure/
│   ├── PostgresContainerFixture.cs    # xUnit ICollectionFixture — starts/stops PostgreSQL via Testcontainers
│   ├── PersonalisationEngineFactory.cs # WebApplicationFactory — swaps DB + Claude with test doubles
│   ├── StubClaudeClient.cs            # Returns deterministic recommendation; no API key needed
│   └── IntegrationTestBase.cs         # Base class — wires factory, resets DB between tests
├── Integration/
│   ├── Players/
│   │   └── PlayersApiTests.cs         # 10 tests — full CRUD over HTTP against real PostgreSQL
│   └── Recommendations/
│       └── RecommendationsApiTests.cs # 12 tests — all 4 guardrail scenarios, persistence, 404
├── Unit/
│   ├── ClaudeClientTests.cs           # 6 tests — stub mode, live success, HTTP errors, malformed JSON
│   └── GlobalExceptionHandlerTests.cs # 5 tests — all exception types + pass-through
├── Rules/
│   └── RulesEngineTests.cs            # 10 tests — all guardrail conditions + safe options
└── Recommendations/
    └── RecommendationServiceTests.cs  # 5 tests — mocked Claude/rules engine, in-memory DB
# Coverage: 100% line, 90.9% branch (47 tests total)

frontend/src/
├── api/
│   ├── types.ts                       # Player + RecommendationResponse TypeScript types
│   └── client.ts                      # Fetch wrapper — getPlayers, getPlayer, generateRecommendation
└── pages/
    ├── PlayerList.tsx                 # Table of all players, click-through to detail
    └── PlayerDetail.tsx               # Profile view + Generate Recommendation button + result display
```

## Environment / configuration

Copy `.env.example` to `src/PersonalisationEngine.Api/.env.local` and fill in values. The app loads it at startup from the project content root.

| Key | Description |
|-----|-------------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string (double underscore = section separator) |
| `Anthropic__ApiKey` | Anthropic API key — leave as `REPLACE_ME` for stub mode |

**Stub mode**: if `Anthropic__ApiKey` is blank or `REPLACE_ME`, `ClaudeClient` returns a canned recommendation instead of calling the API. No error is thrown.

## Architecture overview

```
POST /api/recommendations/{playerId}
  → RecommendationService
      → PlayerService (load player from DB)
      → RulesEngine.Evaluate(player)
          → blocked? persist + return { safeToShow: false, blockReason: "..." }
          → safe?    call ClaudeClient.GenerateRecommendationAsync(playerJson, safeOptions)
                         → returns ClaudeRecommendation or null (if unavailable)
                     persist + return { safeToShow: true, headline, message, ... }
```

## Seed players (demo scenarios)

| PlayerId | Sport | Risk | Notes |
|---|---|---|---|
| P001 | Football / Scotland | Low | Happy-path demo player |
| P002 | Horse Racing | Low | Second safe example |
| P003 | Tennis | Medium | Safe — medium risk is allowed |
| P004 | Basketball | Low | Safe — inactive 20d (under 30d threshold) |
| P005 | Football | **High** | Blocked — high risk |
| P006 | Football | Medium | Blocked — **self-excluded** |
| P007 | Rugby | Low | Blocked — **cooling-off** |
| P008 | Golf | Low | Blocked — **inactive 45d** (over 30d threshold) |

## RG guardrail rule order

Rules are evaluated in this order; first match wins:
1. `IsSelfExcluded == true` → blocked
2. `IsInCoolingOff == true` → blocked
3. `RiskLevel == High` → blocked
4. `LastLoginDaysAgo > 30` → blocked
5. All clear → safe, build safe options from sport + bet type + favourite team

## Database

PostgreSQL 16 on **port 5433** (not 5432, to avoid clashing with `claude-twaddle-not-java`).
Database name: `personalisationengine`. User: `persengine`. Password: `change-me` (local dev only).

## Notes

- JSON is serialised as camelCase with string enums (e.g. `"riskLevel": "Low"` not `0`).
- `DateTime` values are stored as UTC and serialised with a `Z` suffix.
- EF Core migrations run automatically on startup via `db.Database.MigrateAsync()`.
- Serilog replaces the default ASP.NET Core logger; structured console output.
- The frontend proxies `/api` to `http://localhost:5080` via Vite's `server.proxy` config.
