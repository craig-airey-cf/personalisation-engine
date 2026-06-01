# Personalisation Engine

A .NET 8 / C# personalisation and recommendation engine built for a 4-week learning programme. Set in an iGaming context: mock player profiles are fed through a responsible gaming rules engine; if a player is safe to target, the Anthropic Claude API generates personalised copy. Results are surfaced through a React/Vite admin UI.

## Architecture

```
POST /api/recommendations/{playerId}
  → RecommendationService
      → PlayerService           (load player from DB)
      → RulesEngine.Evaluate    (RG guardrail checks)
          blocked? → persist + return { safeToShow: false, blockReason }
          safe?    → ClaudeClient.GenerateRecommendationAsync
                         → persist + return { safeToShow: true, headline, message, … }
```

## Prerequisites

| Tool | Version |
|------|---------|
| Docker | 20+ |
| .NET SDK | 8.0 |
| Node.js | 22 |
| npm | 10+ |

## Local setup

```bash
# 1. Start PostgreSQL on port 5433
docker-compose up -d

# 2. Copy env template and fill in values
cp src/PersonalisationEngine.Api/.env.example src/PersonalisationEngine.Api/.env.local

# 3. Start the API (auto-migrates in Development, seeds demo players)
dotnet run --project src/PersonalisationEngine.Api
# → http://localhost:5080  (Swagger UI at http://localhost:5080/swagger)

# 4. Start the frontend dev server (proxies /api → :5080)
cd frontend && npm install && npm run dev
# → http://localhost:5173
```

## Configuration

Copy `.env.example` to `src/PersonalisationEngine.Api/.env.local` and set:

| Key | Description | Default |
|-----|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | see docker-compose |
| `Anthropic__ApiKey` | Anthropic API key — leave as `REPLACE_ME` for stub mode | `REPLACE_ME` |

**Stub mode**: if `Anthropic__ApiKey` is blank or `REPLACE_ME`, `ClaudeClient` returns a canned recommendation. No API call is made and no error is thrown.

## Running tests

```bash
# .NET tests (unit + integration via Testcontainers — requires Docker)
dotnet test

# With coverage report
dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory /tmp/pe-coverage

# Frontend tests
cd frontend && npm test
```

Current baseline: **48 .NET tests** (100% line, 90.9% branch) · **24 frontend tests**

## API overview

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/players` | List all players |
| `GET` | `/api/players/{playerId}` | Get a single player |
| `POST` | `/api/players` | Create a player |
| `PUT` | `/api/players/{playerId}` | Update a player |
| `DELETE` | `/api/players/{playerId}` | Delete a player |
| `POST` | `/api/recommendations/{playerId}` | Generate a recommendation |

### Responsible gaming guardrails (evaluated in order)

1. `IsSelfExcluded == true` → blocked
2. `IsInCoolingOff == true` → blocked
3. `RiskLevel == High` → blocked
4. `LastLoginDaysAgo > 30` → blocked
5. All clear → safe; Claude generates personalised copy

### Demo players (seeded in Development)

| ID | Sport | Notes |
|----|-------|-------|
| P001 | Football | Safe — happy-path player |
| P002 | Horse Racing | Safe |
| P003 | Tennis | Safe — medium risk is allowed |
| P004 | Basketball | Safe — inactive 20 d (under 30 d threshold) |
| P005 | Football | Blocked — high risk |
| P006 | Football | Blocked — self-excluded |
| P007 | Rugby | Blocked — cooling-off |
| P008 | Golf | Blocked — inactive 45 d |

## Database

PostgreSQL 16 on **port 5433** (avoids clashing with other local services on 5432).

```
Database : personalisationengine
User     : persengine
Password : change-me  (local dev only)
```

EF Core migrations run automatically on startup **in Development only** (`RunMigrationsOnStartup: true`). For other environments, run migrations as a deployment step:

```bash
dotnet ef database update --project src/PersonalisationEngine.Api \
  -- --ConnectionStrings:DefaultConnection="<connection-string>"
```

## Production caveats

- **Authentication**: the recommendations endpoint is unauthenticated by default — add API-key or bearer token auth before exposing publicly (see [#21](../../issues/21)).
- **Migrations**: set `RunMigrationsOnStartup: false` in production and run migrations as a controlled deployment step.
- **Seed data**: demo players (P001–P008) are only seeded in Development. They will not appear in other environments.
- **CORS**: the `ViteDev` CORS policy (`http://localhost:5173`) is only applied in Development.
