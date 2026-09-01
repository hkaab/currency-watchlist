# Currency Watchlist & Alert Service

[![CI](https://github.com/hkaab/currency-watchlist/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/hkaab/currency-watchlist/actions/workflows/ci.yml)
[![Backend coverage](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/hkaab/currency-watchlist/main/.github/badges/backend-coverage.json)](https://github.com/hkaab/currency-watchlist/actions/workflows/ci.yml)
[![Frontend coverage](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/hkaab/currency-watchlist/main/.github/badges/frontend-coverage.json)](https://github.com/hkaab/currency-watchlist/actions/workflows/ci.yml)

Create currency watchlists, track currency pairs, fetch live exchange rates from [Frankfurter](https://frankfurter.dev/), and get alerted when a rate crosses a threshold — with live updates pushed to the browser instead of polling.

- **Backend**: .NET 10 / ASP.NET Core, Clean Architecture (Domain → Application → Infrastructure → Api), SQLite via EF Core, Swagger/OpenAPI.
- **Frontend**: Next.js 16 (App Router, TypeScript), functional components, plain hooks for state (no Redux).
- **Event-driven, front-to-back**: the backend publishes in-process domain events (`RatesRefreshedEvent`, `AlertTriggeredEvent`) that drive alert evaluation and push live updates to the frontend over SignalR; the frontend subscribes and updates the UI reactively.

See [`docs/architecture.md`](docs/architecture.md) for two diagrams: this system, and how it would look at enterprise scale.

## Running it

### Backend

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
cd backend/src/CurrencyWatchlist.Api
dotnet run
```

- API: `http://localhost:5289`
- Swagger UI: `http://localhost:5289/swagger`
- A SQLite database (`currencywatchlist.db`) is created and migrated automatically on first run.

### Frontend

Prerequisites: Node 20+.

```bash
cd frontend
npm install
cp .env.local.example .env.local   # points at http://localhost:5289 by default
npm run dev
```

Open `http://localhost:3000`. The backend must be running first (CORS is locked to `http://localhost:3000`).

### Tests

Backend (from `backend/`):

```bash
dotnet test                                                  # 74 tests: unit (Application) + integration (Api, isolated SQLite + mocked rate provider)
dotnet test --collect:"XPlat Code Coverage"                  # produces coverage.cobertura.xml per project
reportgenerator "-reports:**/coverage.cobertura.xml" "-targetdir:coverage-results/report" "-reporttypes:TextSummary"  # requires: dotnet tool install -g dotnet-reportgenerator-globaltool
```

Current backend line coverage: **95.7%**.

Frontend (from `frontend/`):

```bash
npm test              # Vitest + React Testing Library (73 tests)
npm run test:coverage  # coverage report
npm run test:e2e       # Playwright, against the real backend + frontend dev servers (both must be running)
```

Current frontend line coverage: **93.7%** (unit). All three Playwright specs pass against the live stack, including a two-tab test that confirms a rate refresh in one tab pushes live to another tab viewing the same watchlist via SignalR — the actual event-driven behavior, not a mock of it.

### CI/CD

[`.github/workflows/ci.yml`](.github/workflows/ci.yml) runs on every push/PR to `main`:

- **Backend job**: `dotnet restore` → `build` (Release) → `test` with coverage collection.
- **Frontend job**: `npm ci` → `lint` → `build` → `test:coverage`.
- **Badges job** (push to `main` only): reads both coverage numbers and commits updated badge JSON to `.github/badges/`, which the README badges above read live via shields.io's endpoint badge — no third-party coverage service or account needed.

E2E (Playwright) isn't run in CI since it exercises the real Frankfurter API against two live dev servers — see "Tests" above to run it locally.

## Assumptions

- **No authentication** — single implicit user, matching the assignment's scope. Every watchlist is visible to everyone hitting the API.
- **`POST /api/rates/refresh`** accepts an optional `watchlistId` query parameter to scope the refresh to one watchlist's pairs (used by the frontend's per-watchlist "Refresh Rates" button); omitted, it refreshes every distinct pair across all watchlists, matching the brief's literal wording.
- **`GET /api/rates/history`** is served entirely from our own stored `RateSnapshot` table, not a live call to Frankfurter — the snapshot table exists specifically to build history over time, and Frankfurter's own historical-range endpoint would just be a second, redundant source of truth.
- **`POST /api/alerts/{id}/evaluate`** is scoped strictly to the requested rule: it fetches the latest rate, checks only that rule's condition, and persists a `RateSnapshot` either way. A bulk `/api/rates/refresh`, by contrast, evaluates *every* active alert for the pairs it touches (via the event pipeline) — a manual "check this one alert" action deliberately doesn't have the side effect of silently triggering sibling alerts on the same pair.
- Currency codes are validated as exactly 3 letters and normalized to uppercase; no validation against a real ISO-4217 list (Frankfurter itself is the source of truth for whether a code is real — an unrecognized code surfaces as a 400 from `/api/rates/refresh`, not from input validation).
- A watchlist item's `(BaseCurrency, QuoteCurrency)` pair is unique per watchlist but not globally — the same pair can appear in multiple watchlists, sharing the same `RateSnapshot` history.

## Tradeoffs

- **In-process events, not a message broker.** `IEventPublisher`/`IDomainEventHandler<T>` are a ~60-line hand-rolled abstraction resolved via DI, not MediatR (recent versions require a commercial license for most organizational use) and not RabbitMQ/Kafka. This is genuinely event-driven (multiple independent handlers react to the same event; new handlers plug in without touching publishers) without the operational cost of running a broker for a single-process app. The enterprise diagram shows what replaces it at scale.
- **SQLite, not a server database.** Fine for this exercise and for a single-instance deployment; would not survive concurrent writes at real scale (see architecture doc).
- **No caching layer.** Every "latest rate" read hits SQLite directly; at real traffic this is the first thing to add (Redis, or even an in-memory `IMemoryCache` with a short TTL).
- **Client-rendered frontend, not server components fetching data.** The backend is a separate service with its own CORS and a SignalR connection the browser owns directly; server-rendering data that a client needs to live-patch over WebSocket anyway would add complexity without much payoff here. Everything above the two page shells is a client component.
- **No retry/circuit-breaker policy on the Frankfurter HTTP client** (e.g. Polly). A single timeout/failure surfaces immediately as a 502. Acceptable for a free, unauthenticated public API in a take-home; not for a production dependency.
- **No pagination** on watchlists/alerts lists — fine at this scale, wouldn't be at real scale.

## Future improvements

- Authentication/authorization (per-user watchlists) and multi-tenancy.
- A scheduled background job (e.g. `IHostedService` / Azure Function) to refresh rates automatically instead of only on button click or manual API call.
- Polly-based retry/circuit-breaker around the external rate provider, and a fallback provider.
- Move alert evaluation for bulk refreshes off the request thread (a background queue) once refresh volume grows.
- Soft-delete / audit trail on watchlists instead of hard delete, given `AlertEvent` history has real value.
- E2E coverage for delete flows and the "provider unavailable" path (currently covered at the integration-test level with a mocked provider, not via Playwright).

### What I'd do differently in production

Everything in the "enterprise-scale" half of `docs/architecture.md`: decouple rate ingestion into its own scheduled worker so it's never on the request path; put a real message broker between ingestion and alert evaluation so evaluation scales independently and survives restarts; add a Redis SignalR backplane so live updates work across multiple API instances instead of one; move to a managed Postgres with read replicas; add an API gateway and identity provider for real multi-tenant auth; and add OpenTelemetry tracing/metrics plus centralized logging, since debugging a distributed system by reading console output stops working the moment there's more than one instance.

## Repository layout

```
backend/    .NET 10 solution (Domain, Application, Infrastructure, Api + tests)
frontend/   Next.js app (App Router, TypeScript)
docs/       Architecture diagrams
```
