# Currency Watchlist & Alert Service

[![CI](https://github.com/hkaab/currency-watchlist/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/hkaab/currency-watchlist/actions/workflows/ci.yml)
[![Backend coverage](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/hkaab/currency-watchlist/main/.github/badges/backend-coverage.json)](https://github.com/hkaab/currency-watchlist/actions/workflows/ci.yml)
[![Frontend coverage](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/hkaab/currency-watchlist/main/.github/badges/frontend-coverage.json)](https://github.com/hkaab/currency-watchlist/actions/workflows/ci.yml)

Create currency watchlists, track currency pairs, fetch live exchange rates from [Frankfurter](https://frankfurter.dev/), and get alerted when a rate crosses a threshold — with live updates pushed to the browser instead of polling.

- **Backend**: .NET 10 / ASP.NET Core, Clean Architecture (Domain → Application → Infrastructure → Api), SQLite via EF Core, Swagger/OpenAPI.
- **Frontend**: Next.js 16 (App Router, TypeScript), functional components, plain hooks for state (no Redux).
- **Event-driven, front-to-back**: the backend publishes in-process domain events (`RatesRefreshedEvent`, `AlertTriggeredEvent`) that drive alert evaluation and push live updates to the frontend over SignalR; the frontend subscribes and updates the UI reactively.
- **Self-refreshing**: a scheduled background job refreshes every tracked currency pair every 5 minutes (configurable) — rates and alerts stay current without anyone clicking "Refresh Rates."

See [`docs/architecture.md`](docs/architecture.md) for two diagrams: this system, and how it would look at enterprise scale.

## Design decisions

- **Clean Architecture, dependencies point inward.** `Domain` (entities, enums, domain event contracts) has zero dependencies. `Application` (services, DTOs, validators, repository/event interfaces) depends only on `Domain`. `Infrastructure` (EF Core, the Frankfurter HTTP client) implements Application's interfaces. `Api` is the composition root that wires concrete implementations into DI. Controllers depend on `Application` interfaces, never on EF Core or HttpClient directly — swapping SQLite for Postgres, or Frankfurter for another provider, touches `Infrastructure` only.
- **SOLID isn't just a slogan here** — concretely: repositories are split narrowly per aggregate rather than one fat `IRepository<T>` (ISP); `IRateProvider` and the event-handler interfaces mean new behavior (a second rate source, a new reaction to a rate refresh) plugs in via a new class + DI registration, not by editing existing code (OCP); every service depends on interfaces injected via the constructor, never `new`s up a concrete repository or HTTP client (DIP).
- **DTOs in, DTOs out — entities never cross the API boundary.** Controllers and services exchange `CreateWatchlistRequest`/`WatchlistResponse`/etc., mapped from entities via small extension methods (`MappingExtensions.cs`). This keeps EF Core's tracking/lazy-loading concerns out of the wire format and means the JSON shape doesn't accidentally change when the schema does.
- **Event-driven backend, not just "async."** A rate refresh doesn't call an "evaluate alerts" method directly — it publishes a `RatesRefreshedEvent`, and independent handlers (`EvaluateAlertsOnRateRefreshHandler`, `PushRateUpdateHandler`) react to it, unaware of each other. Adding a third reaction (e.g. an email notifier) means adding a handler class, not modifying the refresh code path. See **Tradeoffs** below for why this is in-process rather than a message broker.
- **Real-time via SignalR, not polling.** The frontend doesn't refetch on a timer to see if rates changed elsewhere — the backend pushes `RatesUpdated`/`AlertTriggered` to whichever browser tabs are viewing the affected watchlist. This is the actual "event-driven front-to-back" requirement, not just a buzzword: verified in `e2e/golden-path.spec.ts` with a test that refreshes in one browser tab and asserts the update lands in a second tab with no reload. The frontend has **zero** `setInterval`/`setTimeout` anywhere — every state change is either a direct result of a user action or a server-pushed event.
- **Auto-refresh lives on the server, not as frontend polling.** `RateRefreshBackgroundService` (an `IHostedService`) ticks on a timer and calls the exact same `IRateService.RefreshAsync` that the "Refresh Rates" button calls — so a scheduled tick drives the identical `RatesRefreshedEvent` → alert evaluation → SignalR push pipeline as a manual refresh. This keeps the "self-refreshing app" requirement satisfied without contradicting "event-driven, not polling": one job refreshes for every connected client at once, instead of N browser tabs each independently asking "anything new?" on their own timers. Interval defaults to 5 minutes (configurable via `RateRefresh:IntervalMinutes`) — deliberately not shorter, since Frankfurter's underlying ECB reference rates only update once a day, so refreshing every 30 seconds would just re-fetch the same number.
- **Currency codes are validated against Frankfurter's real supported list, not just a 3-letter regex.** `ICurrencyCatalog`/`FrankfurterCurrencyCatalog` fetch and cache Frankfurter's `/currencies` endpoint (a set of ~30 codes, not full ISO 4217), and `CreateWatchlistItemRequestValidator` checks both codes against it via a `MustAsync` rule that only runs once the format check already passed (`DependentRules` — no point calling out to the catalog for input that's already garbage). The list is cached for 24 hours since it essentially never changes, and if Frankfurter can't be reached, validation **fails open** (treats the code as valid) rather than blocking every watchlist mutation on a third-party outage — a genuinely bad code still gets caught the moment a real rate fetch is attempted (400/502, same as today).
- **Manual FluentValidation, not the `FluentValidation.AspNetCore` auto-validation package.** That package's own maintainers recommend against MVC auto-validation (it has had correctness issues and is unmaintained for that use case). Instead, a small `ValidationFilter` (`IAsyncActionFilter`) resolves the right `IValidator<T>` per action argument and returns a `ValidationProblemDetails` on failure — same developer experience, no inherited footgun.
- **Retry with backoff + a circuit breaker around the Frankfurter client, not a bare `HttpClient`.** `Microsoft.Extensions.Http.Resilience` (Polly) wraps the rate-provider `HttpClient`: transient failures (5xx, 408, timeouts, connection errors) get 3 retries with exponential backoff and jitter; a 400/404 for an unrecognized currency is *not* retried, since retrying won't fix a currency code that doesn't exist. A circuit breaker trips after repeated failures so a genuinely down upstream fails fast instead of every request individually waiting out its own retries. `FrankfurterRateProvider` itself needed zero changes — the policy lives entirely in the DI wiring (`AddFrankfurterResilience`), and `RateProviderResilienceTests.cs` proves it end-to-end against a scripted transport (retries-then-succeeds, exhausts-then-throws, and non-transient-doesn't-retry).
- **One exception hierarchy, one place that maps it to HTTP.** Domain/Application code throws `NotFoundException`, `UnknownCurrencyException`, `RateProviderUnavailableException` — plain, testable exceptions with no HTTP knowledge. A single `GlobalExceptionHandler` (`IExceptionHandler`) is the only place that knows `NotFoundException` → 404, `UnknownCurrencyException` → 400, `RateProviderUnavailableException` → 502. Services and controllers stay free of `try/catch`-to-status-code boilerplate.
- **Frontend: hooks own data, components own rendering.** `useWatchlists`/`useWatchlistDetail`/`useAlerts` each own one slice of server state (loading/error/data) and the mutations that touch it; components just render props and call callbacks. No Redux, per the brief — at this scale a global store would add ceremony without solving a problem plain hooks don't already solve. Each dynamic route (`/watchlists/[id]`) is a thin Server Component that only resolves the route param and hands off to a client component — everything interactive is explicitly a client component, not the default.
- **Three tiers of backend tests, deliberately, each catching a different class of bug.** `Application.Tests` unit-tests business logic in isolation (services, validators, event handlers) with every dependency mocked via NSubstitute — fast, and pinpoints exactly what broke. `Api.Tests` are real integration tests through `WebApplicationFactory` against an isolated in-memory SQLite database, with `IRateProvider`/`ICurrencyCatalog` swapped for fakes — so the real DI graph, EF Core mappings, and HTTP pipeline are all actually exercised, not assumed. A third, separate tier (`FrankfurterLiveIntegrationTests`) calls the *real* Frankfurter API with nothing mocked, because "our code correctly handles the response shape we assumed Frankfurter uses" and "Frankfurter actually returns that shape" are two different claims — the first two tiers only ever prove the former. Kept out of the default `dotnet test` run and out of CI, same reasoning as e2e: a live third-party network call is a source of flakiness CI shouldn't carry.
- **Coverage badges read a JSON file in the repo, not a third-party service.** Codecov/Coveralls would need an account and a trust relationship to this specific GitHub repo before a badge could go live. CI instead writes the real coverage percentage to `.github/badges/*.json` on every push to `main`, and the README badges point shields.io at that raw file — live numbers, zero external accounts.

## Running it

### With Docker (fastest way to try it)

Prerequisites: Docker with Compose v2.

```bash
docker compose up --build
```

- Frontend: `http://localhost:3000`
- Backend: `http://localhost:5289` · Swagger UI: `http://localhost:5289/swagger`

What's happening under the hood:

- `backend/Dockerfile` is a multi-stage build (`dotnet publish` on the SDK image, then the slim `aspnet` runtime image). The SQLite file is written to a named volume (`backend-data`) mounted at `/data`, so data survives `docker compose restart`/`down` (not `down -v`).
- `frontend/Dockerfile` builds Next.js's standalone output on `node:22-alpine`. `NEXT_PUBLIC_API_BASE_URL` is a **build arg**, not just a runtime env var — Next.js inlines `NEXT_PUBLIC_*` values into the client bundle at build time, and since the browser (not the frontend container) calls the API directly, that value has to be the host-reachable address (`http://localhost:5289`), not an in-network service name.
- `docker-compose.yml` wires both together; `Cors__AllowedOrigins__0` and `ConnectionStrings__Default` are overridden via environment variables using ASP.NET Core's `__` config-key convention.

> **Not seeing a change you just pulled?** Compose doesn't detect source changes on its own — `docker compose up` alone reuses whatever image was last built. Always run `docker compose up --build` after pulling new code (add `--no-cache` to `docker compose build` if you suspect stale layers even with `--build`).

Stop everything with `docker compose down` (add `-v` to also delete the SQLite volume).

### Without Docker

#### Backend

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
cd backend/src/CurrencyWatchlist.Api
dotnet run
```

- API: `http://localhost:5289`
- Swagger UI: `http://localhost:5289/swagger`
- A SQLite database (`currencywatchlist.db`) is created and migrated automatically on first run.
- Every tracked pair refreshes automatically every 5 minutes (a background job, not frontend polling — see Design decisions). Override with the `RateRefresh__IntervalMinutes` environment variable (or `RateRefresh:IntervalMinutes` in `appsettings.json`), e.g. `RateRefresh__IntervalMinutes=1 dotnet run` to see it happen faster while testing.

#### Frontend

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
dotnet test --filter "Category!=Live"                        # 85 tests: unit (Application) + integration (Api, isolated SQLite + mocked rate provider + resilience pipeline + background refresh)
dotnet test --filter "Category=Live"                         # 5 tests against the real api.frankfurter.app - no mocking (see below)
dotnet test --collect:"XPlat Code Coverage" --filter "Category!=Live"   # produces coverage.cobertura.xml per project
reportgenerator "-reports:**/coverage.cobertura.xml" "-targetdir:coverage-results/report" "-reporttypes:TextSummary"  # requires: dotnet tool install -g dotnet-reportgenerator-globaltool
```

Current backend line coverage: **95.2%**.

`FrankfurterLiveIntegrationTests` calls the real Frankfurter API with nothing mocked, to prove the HTTP client, JSON mapping, and resilience wiring genuinely work against the live service — not just our assumptions about its response shape. It's excluded from the default `dotnet test` run and from CI (`ci.yml` filters it out too) for the same reason the e2e suite isn't in CI: a real third-party network dependency is a source of flakiness CI shouldn't carry. Run it explicitly with the command above.

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
- Currency codes are validated in two stages: format first (exactly 3 letters, normalized to uppercase), then against Frankfurter's own supported-currency list (its `/currencies` endpoint) — not a static ISO 4217 list, since Frankfurter only supports a few dozen major currencies, not all ~180 ISO codes, and a code that's real-but-unsupported would still fail every rate fetch anyway. See Design decisions for the caching/fail-open behavior.
- A watchlist item's `(BaseCurrency, QuoteCurrency)` pair is unique per watchlist but not globally — the same pair can appear in multiple watchlists, sharing the same `RateSnapshot` history.

## Tradeoffs

- **In-process events, not a message broker.** `IEventPublisher`/`IDomainEventHandler<T>` are a ~60-line hand-rolled abstraction resolved via DI, not MediatR (recent versions require a commercial license for most organizational use) and not RabbitMQ/Kafka. This is genuinely event-driven (multiple independent handlers react to the same event; new handlers plug in without touching publishers) without the operational cost of running a broker for a single-process app. The enterprise diagram shows what replaces it at scale.
- **SQLite, not a server database.** Fine for this exercise and for a single-instance deployment; would not survive concurrent writes at real scale (see architecture doc).
- **No caching layer.** Every "latest rate" read hits SQLite directly; at real traffic this is the first thing to add (Redis, or even an in-memory `IMemoryCache` with a short TTL).
- **Client-rendered frontend, not server components fetching data.** The backend is a separate service with its own CORS and a SignalR connection the browser owns directly; server-rendering data that a client needs to live-patch over WebSocket anyway would add complexity without much payoff here. Everything above the two page shells is a client component.
- **No pagination** on watchlists/alerts lists — fine at this scale, wouldn't be at real scale.
- **No fallback rate provider.** The retry/circuit-breaker policy (see Design decisions) handles Frankfurter being flaky, but if it's genuinely down there's no second source to fall back to — the request fails with a 502 either way, just after a fair retry attempt instead of immediately.

## Future improvements

- Authentication/authorization (per-user watchlists) and multi-tenancy.
- A fallback rate provider for when Frankfurter's circuit breaker is open.
- Only auto-refresh pairs that someone is actually watching (active SignalR group members) or that have an active alert, instead of every distinct pair unconditionally — matters once the pair count is large enough for that to be wasted work, not at this app's scale.
- Move alert evaluation for bulk refreshes off the request thread (a background queue) once refresh volume grows.
- Soft-delete / audit trail on watchlists instead of hard delete, given `AlertEvent` history has real value.
- E2E coverage for delete flows and the "provider unavailable" path (currently covered at the integration-test level with a mocked provider, not via Playwright).

### What I'd do differently in production

Everything in the "enterprise-scale" half of `docs/architecture.md`: decouple rate ingestion into its own scheduled worker so it's never on the request path; put a real message broker between ingestion and alert evaluation so evaluation scales independently and survives restarts; add a Redis SignalR backplane so live updates work across multiple API instances instead of one; move to a managed Postgres with read replicas; add an API gateway and identity provider for real multi-tenant auth; and add OpenTelemetry tracing/metrics plus centralized logging, since debugging a distributed system by reading console output stops working the moment there's more than one instance.

## Repository layout

```
backend/            .NET 10 solution (Domain, Application, Infrastructure, Api + tests)
frontend/           Next.js app (App Router, TypeScript)
docs/               Architecture diagrams
docker-compose.yml  Runs both containers together (see "With Docker" above)
.github/workflows/  CI (build, test, coverage badges)
```
