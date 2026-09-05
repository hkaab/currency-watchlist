# Architecture

Two diagrams: the system as built for this exercise, and how it would look as a real, production, multi-tenant service.

## 1. This system

Clean-architecture .NET backend (Domain → Application → Infrastructure → Api) with an in-process event dispatcher that drives alert evaluation and pushes live updates to the Next.js frontend over SignalR, on top of normal REST calls for everything else.

```mermaid
flowchart TB
    subgraph Browser["Browser"]
        UI["Next.js App Router UI<br/>(client components + hooks)"]
    end

    subgraph API["ASP.NET Core 10 Web API"]
        Controllers["Controllers<br/>(thin, DTO in/out)"]
        Services["Application Services<br/>WatchlistService / RateService / AlertService"]
        Repos["Repositories<br/>(EF Core)"]
        Publisher["In-process Event Publisher"]
        Handlers["Event Handlers<br/>EvaluateAlertsOnRateRefresh · PushRateUpdate · PushAlertNotification"]
        Hub["SignalR Hub<br/>/hubs/notifications"]
        RateProvider["FrankfurterRateProvider<br/>(IRateProvider)"]
    end

    DB[("SQLite<br/>Watchlists · Items · RateSnapshots · AlertRules · AlertEvents")]
    Frankfurter["Frankfurter API<br/>api.frankfurter.app"]

    UI -- "REST (fetch)<br/>CRUD, refresh, evaluate" --> Controllers
    Controllers --> Services
    Services --> Repos
    Repos --> DB
    Services -- "GetLatestRatesAsync" --> RateProvider
    RateProvider -- "GET /latest?from=&to=" --> Frankfurter
    Services -- "publish RatesRefreshed /<br/>AlertTriggered" --> Publisher
    Publisher --> Handlers
    Handlers --> Repos
    Handlers -- "IRealtimeNotifier" --> Hub
    Hub -- "WebSocket push<br/>RatesUpdated / AlertTriggered" --> UI
```

**Why this shape:** the frontend and backend are event-driven without needing external infrastructure — an in-process publisher/handler pair inside the API drives the alert-evaluation workflow, and the same events are relayed live to any browser tab viewing the affected watchlist via SignalR, instead of the UI polling for changes. See the root README for the tradeoffs behind this choice (and why not MediatR/a message broker at this scale).

### Sequence: a refresh triggers an alert, pushed live to every open tab

One request (`POST /api/rates/refresh`) fans out through the event pipeline above and reaches a second, completely passive browser tab without it ever polling.

```mermaid
sequenceDiagram
    actor User as User (Tab A)
    participant TabA as Browser · Tab A
    participant Api as RateService
    participant Fx as Frankfurter API
    participant DB as SQLite
    participant Pub as Event Publisher
    participant Eval as EvaluateAlertsOnRateRefreshHandler
    participant Push as PushRateUpdateHandler /<br/>PushAlertNotificationHandler
    participant Hub as SignalR Hub
    participant TabB as Browser · Tab B<br/>(same watchlist, idle)

    User->>TabA: Click "Refresh Rates"
    TabA->>Api: POST /api/rates/refresh?watchlistId=1
    Api->>Fx: GET /latest?from=USD&to=AUD,EUR
    Fx-->>Api: latest rates
    Api->>DB: save RateSnapshot(s)
    Api-->>TabA: 200 OK (refreshed snapshots)
    Api->>Pub: publish RatesRefreshedEvent

    Pub->>Eval: HandleAsync(RatesRefreshedEvent)
    Eval->>DB: load active AlertRules for the refreshed pairs
    alt threshold crossed
        Eval->>DB: save AlertEvent
        Eval->>Pub: publish AlertTriggeredEvent
    end

    Pub->>Push: HandleAsync(RatesRefreshedEvent)
    Push->>Hub: NotifyRatesUpdatedAsync(watchlistId, snapshots)
    Hub-->>TabA: RatesUpdated (WebSocket push)
    Hub-->>TabB: RatesUpdated (WebSocket push)

    opt an alert was triggered
        Pub->>Push: HandleAsync(AlertTriggeredEvent)
        Push->>Hub: NotifyAlertTriggeredAsync(watchlistId, alertEvent)
        Hub-->>TabA: AlertTriggered (WebSocket push)
        Hub-->>TabB: AlertTriggered (WebSocket push)
    end

    Note over TabB: Tab B took no action and made no request -<br/>it just received the live push.
```

## 2. Real-world, enterprise-scale version

Same domain, but built for multiple tenants, high availability, and a much larger user base: ingestion is decoupled from the API via a message broker, evaluation runs as its own scalable consumer, and SignalR gets a backplane so it works across many API instances.

```mermaid
flowchart TB
    subgraph Client["Clients"]
        Web["Next.js Web App<br/>(CDN-hosted, e.g. Vercel)"]
        Mobile["Mobile app (future)"]
    end

    Gateway["API Gateway / BFF<br/>(authN/authZ, rate limiting, routing)"]
    Identity["Identity Provider<br/>(OIDC - Auth0 / Entra ID)"]

    subgraph Core["Watchlist & Alert Platform (containers, auto-scaled)"]
        ApiSvc["Watchlist API<br/>(stateless, N replicas)"]
        AlertSvc["Alert Evaluation Service<br/>(consumer)"]
        NotifySvc["Notification Service<br/>(SignalR + email/push)"]
    end

    Ingestion["Rate Ingestion Worker<br/>(scheduled poll, e.g. every minute)"]
    Broker[["Message Broker<br/>(Kafka / Azure Service Bus)"]]
    Backplane[("Redis<br/>SignalR backplane + hot-rate cache")]
    Postgres[("Managed Postgres<br/>primary + read replicas")]
    RateApi["External Rate Provider(s)<br/>(Frankfurter / paid FX API, with fallback)"]
    Observability["Observability<br/>OpenTelemetry traces · metrics · centralized logs"]

    Web --> Gateway
    Mobile --> Gateway
    Gateway --> Identity
    Gateway --> ApiSvc
    ApiSvc --> Postgres
    ApiSvc --> Backplane
    ApiSvc -- "on-demand refresh" --> RateApi

    Ingestion -- "poll on schedule" --> RateApi
    Ingestion -- "publish RatesRefreshed" --> Broker
    Broker --> AlertSvc
    AlertSvc --> Postgres
    AlertSvc -- "publish AlertTriggered" --> Broker
    Broker --> NotifySvc
    NotifySvc -- "push via backplane" --> Backplane
    Backplane -- "WebSocket" --> Web

    ApiSvc -.-> Observability
    AlertSvc -.-> Observability
    Ingestion -.-> Observability
    NotifySvc -.-> Observability
```

**What changes and why:**
- **Rate ingestion becomes its own service** on a schedule, independent of user traffic, so a slow/rate-limited external provider never blocks API requests.
- **A real message broker** (Kafka/Service Bus) replaces the in-process publisher so alert evaluation scales independently, survives API restarts, and can be retried/replayed.
- **SignalR gets a Redis backplane** so live updates work correctly across many API instances, not just one process.
- **Postgres with read replicas** replaces SQLite for concurrent write throughput and durability.
- **An API gateway + identity provider** add multi-tenant auth, which this exercise deliberately has none of.
- **Observability** (traces/metrics/logs) becomes essential once there's more than one instance to debug.
