# GameVault

**GameVault is my personal hands-on DAPR sandbox.** It is a deliberately minimal microservices platform, a runnable scaffold I reach for whenever I want to experiment in a real distributed-system environment: DAPR workflows, resiliency patterns, failure-handling scenarios, pub/sub behaviour, service invocation, and anything else that needs actual sidecars and infra to poke at. It is not a production system; the goal is to keep it simple enough that the infrastructure behaviour stays front and centre.

**Stack:** .NET 10 · DAPR 1.17 · PostgreSQL 16 · Keycloak 26 · RabbitMQ · YARP · Serilog → Seq · Docker Compose

### First-time setup (one-time only)

**1. Copy the config templates and fill them in**

```bash
cp infra/.env.example               infra/.env
cp infra/dapr/secrets.json.example  infra/dapr/secrets.json
```

Open `infra/.env` and fill in every blank value — DB credentials (choose anything, e.g. `devpassword`), `KEYCLOAK_ADMIN_PASSWORD`, and leave `KEYCLOAK_CLIENT_ID` / `KEYCLOAK_CLIENT_SECRET` for after step 3.

Set `rabbitmq-connection-string` in `infra/dapr/secrets.json` to `amqp://guest:guest@rabbitmq:5672` (RabbitMQ's built-in default; nothing to create). Leave the Keycloak admin-client fields blank for now.

**2. Start the backing infrastructure**

```bash
docker compose -f infra/docker-compose.yml up -d
```

**3. Configure Keycloak** (http://localhost:8180 — log in with `admin` / `KEYCLOAK_ADMIN_PASSWORD` from `.env`)

**a) Create the realm**
- Top-left dropdown → **Create realm** → name it `gamevault` → **Create**

**b) Create the gateway client** — WebEdge uses this to proxy password-grant token requests

- **Clients** → **Create client** → Client ID: `gamevault-gateway`
- **Next** → Client authentication: **ON** → enable **Direct access grants** only → **Save**
- **Credentials** tab → copy the Client secret
- Set in `.env`: `KEYCLOAK_CLIENT_ID=gamevault-gateway`, `KEYCLOAK_CLIENT_SECRET=<copied secret>`

**c) Create the admin service-account client** — Customer service uses this to create/delete users

- **Clients** → **Create client** → Client ID: `gamevault-admin`
- **Next** → Client authentication: **ON** → enable **Service accounts roles** only → **Save**
- **Credentials** tab → copy the Client secret
- Set in `infra/dapr/secrets.json`: `keycloak-admin-client-id: gamevault-admin`, `keycloak-admin-client-secret: <copied secret>`
- **Service accounts roles** tab → **Assign role** → filter by clients → `realm-management` → select **manage-users** → **Assign**

Keycloak state is stored in the `keycloak-data` named Docker volume. It survives `docker compose stop/start` and is only lost on `docker compose down -v`.

---

### Start it (after first-time setup)

```bash
docker compose -f infra/docker-compose.yml up -d
docker compose -f infra/docker-compose.services.yml up -d
```

Then open the interactive API reference at http://localhost:5000/scalar.

---

## High-Level Design

```mermaid
flowchart TD
    Client["Client / curl / Scalar"]
    WebEdge["WebEdge\n(YARP gateway · :5000)"]
    Catalog["Catalog\n(:5007)"]
    Customer["Customer\n(:5008)"]
    Order["Order\n(:5009)"]
    Notifications["Notifications\n(no public port)"]
    RabbitMQ[("RabbitMQ\npub/sub transport")]
    Keycloak["Keycloak\n(:8180)"]
    Seq["Seq\n(:5380)"]

    Client -->|HTTP| WebEdge
    WebEdge -->|proxy| Catalog
    WebEdge -->|proxy| Customer
    WebEdge -->|proxy| Order
    Order -->|"Dapr service invocation\n(sync, reserve/release stock)"| Catalog
    Order -->|"Dapr pub/sub\ntopic: order-completed"| RabbitMQ
    RabbitMQ -->|deliver| Notifications
    WebEdge -.->|JWT verify| Keycloak
    Customer -.->|Admin API| Keycloak
    Catalog & Customer & Order & Notifications -.->|structured logs| Seq
```

Inter-service communication: DAPR (pub/sub + service invocation). Identity: Keycloak (OIDC / JWT Bearer).

Each service owns its own PostgreSQL database. Services never share a database. Cross-service references carry no FK constraints; idempotency is enforced at the application level.

### Architecture per service

All services follow the same Clean Architecture layer structure:

```
Domain          ← POCOs, enums, domain logic. Zero external dependencies.
Application     ← Use cases, ports/interfaces, DTOs
Infrastructure  ← EF Core, external clients, implements Application ports
Api             ← Composition root, DI wiring, controllers
```

Shared building blocks live in `src/SharedLibraries/GameVault.Common`:

| Project | Purpose |
|---|---|
| `GameVault.SharedKernel` | `Result<T>`, `Error`, base value objects, exceptions |
| `GameVault.Core` | Cross-cutting ASP.NET Core concerns (JWT Bearer setup, etc.) |
| `GameVault.Contracts` | Shared DTOs and enums for service boundaries |

## Services

| Service | Port | Description |
|---|---|---|
| `GameVault.WebEdge.Api` | `5000` | API gateway: routes public catalog and customer endpoints |
| `GameVault.Catalog.Api` | `5007` | Game catalog: products and internal stock reservation endpoints |
| `GameVault.Customer.Api` | `5008` | Customer management, Keycloak integration |
| `GameVault.Order.Api` | `5009` | Order processing, invokes Catalog reservation endpoints via DAPR |
| `GameVault.Notifications.Api` | (none) | Async event consumer that subscribes to `order-completed` via Dapr pub/sub (RabbitMQ) and logs a structured Serilog entry to Seq. No public endpoints; no database. |

## Pub/Sub Flow (Order → Notifications)

When a payment succeeds, `PayOrderHandler` marks the order as `Paid`, saves it to the database, then publishes an `OrderCompletedEvent` to the `order-completed` topic on the `gamevault-pubsub` Dapr component (backed by RabbitMQ). The publish uses the Dapr sidecar's HTTP API directly (`POST /v1.0/publish/...`) via a named `IHttpClientFactory` client, with 3 retries and 200 ms fixed backoff between attempts.

`GameVault.Notifications.Api` subscribes to the same topic via its Dapr sidecar. The sidecar calls `POST /notifications/order-completed` on the Notifications app, which logs one structured Serilog `Information` entry, visible in Seq alongside all other service logs.

**Known gap:** the `X-Correlation-Id` from the original client request propagates through WebEdge → Order → Catalog (via `CorrelationIdMiddleware` + `CatalogClient.PropagateCorrelationId`), but breaks at the pub/sub boundary. Dapr delivers the message to Notifications as a fresh internal POST with no original client headers, so the Notifications side generates a new correlation ID. The end-to-end Seq trace is therefore two separate correlation IDs for the Order → Notifications leg. This can be closed by embedding the correlation ID in the event payload (tracked as a follow-up).

**Documented trade-off:** the `SaveChangesAsync` (marking the order `Paid`) and the `PublishEventAsync` are not in the same transaction. If RabbitMQ is unreachable and all retries are exhausted, the order stays `Paid` with no notification delivered. The HTTP response to the client is unaffected; the payment is not rolled back. A transactional outbox would close this gap and is flagged as a Phase 5 stretch goal.

## Gateway Routing Conventions

`GameVault.WebEdge.Api`'s YARP routes wire each service differently on purpose; this is not an inconsistency to fix:

- **Catalog (public)**: two explicit GET-only routes proxy `GET /catalog/api/products` and `GET /catalog/api/products/{id}`, stripping the `/catalog` prefix before forwarding. Reservation endpoints (`POST /api/products/{id}/reservations`, `POST /api/orders/.../confirm`, `POST /api/orders/.../release`) have **no matching YARP route**; they are reachable only through the DAPR sidecar and are never exposed externally.
- **Customer**: the `/customers/{**catch-all}` route has no transform at all; the path is forwarded unchanged, so `CustomersController` uses a plain `[controller]` route (`Customers`) to match. Public URL: `/customers/{id}`.

Making both controllers share one route-attribute convention would require either copying Catalog's redundant `/api` segment into Customer's public URLs (`/customers/api/Customers/{id}`) or simplifying Catalog's gateway wiring instead (a change to the template service, not Customer). Neither is worth it just to make the two `[Route(...)]` attributes read the same; each is correct for its own gateway route.

## Cross-Service Consistency

There is no distributed transaction across a service's own database and another service/system it depends on, so each such reference documents its own idempotency and compensation strategy explicitly (per repo convention; see `StockReservation.OrderId` in Catalog for the DB-level example).

### Customer ↔ Keycloak (identity)

- **Register**: the Keycloak user is created first, then the local `Customer` row is written. If the local write fails after Keycloak succeeded, the handler compensates by deleting the just-created Keycloak user before rethrowing. Keycloak's own email-uniqueness rejection (409) is surfaced as an expected `EmailConflict` result, not an exception.
- **Known gap — no idempotency key on Register**: if a client retries `POST /customers/register` after a timeout (the first attempt actually succeeded but the response was lost), there is nothing that recognizes the retry as the same logical request. Today the retry just calls Keycloak again, which happens to reject it with a 409 on the same email, so it doesn't create a duplicate account, but it also doesn't return the original success (the client gets `EmailConflict` instead of the `customerId` it already has). A proper fix needs a client-supplied idempotency key (e.g. an `Idempotency-Key` header) plus a new unique, nullable column on `Customer` to record and replay it (a schema change, not yet implemented, tracked as a follow-up).
- **Delete**: the Keycloak user is deleted first, then the local row is soft-deleted. If the Keycloak call fails, nothing is written locally; the customer stays active on both sides rather than ending up soft-deleted locally while still able to authenticate via Keycloak. `DeleteUserAsync` treats an already-deleted (404) Keycloak user as success, so a retried delete request can still complete the local write.
- **Known gap**: in the narrow window where the Keycloak deletion succeeds but the subsequent local `SaveChangesAsync` fails (DB unavailable, concurrency conflict), the customer can no longer authenticate but the local record still shows active. This is retry-safe (the Keycloak call is idempotent) but not automatically reconciled today. Permanently closing this window requires an outbox pattern: a durable "pending Keycloak deletion" record written in the same local transaction, processed by a background worker with retries, which is not yet implemented and is tracked as a follow-up.
- **Account lifecycle**: `RegistrationStatus` currently has a single member (`Completed`), with no email verification or suspend states. This is a deliberate MVP scope decision, not an oversight; the enum is left in place to extend once those states are needed.
- **Reactivate**: `POST /customers/reactivate` (anonymous) accepts `{ email, password }`. The handler finds the soft-deleted local row by email, calls Keycloak's Admin API to re-create the user with the *same UUID* as the original `Customer.Id` (Keycloak accepts an explicit `"id"` in the create payload), then clears `IsDeleted`/`DeletedAt` on the local row. The customer's profile data (name, phone) is preserved from before deletion. If the local `SaveChangesAsync` fails after Keycloak succeeds, the handler compensates by deleting the just-created Keycloak user before rethrowing, mirroring the Register compensation pattern.
- **Known gap — no email-change endpoint**: `UpdateCustomerRequest` only covers `FirstName`/`LastName`/`PhoneNumber`; there is no way to change a registered email either locally or in Keycloak today. Tracked as a follow-up.
- **Known gap — no password reset/change flow**: `IKeycloakAdminClient` only exposes create/delete; there is no self-service password reset or change capability. Tracked as a follow-up.

### Order → Catalog (stock reservation)

`POST /api/orders` is the system's first cross-service write. The handler calls Catalog synchronously via Dapr service invocation, because the response to the customer must include an immediate reserve/reject outcome.

- **Idempotency on write**: Catalog's `UX_StockReservations_OrderId_ProductId` unique index prevents duplicate reservations for the same order+product pair. A retried `POST /api/orders` from the client that generates a new `OrderId` is *not* idempotent; client-side idempotency (an `Idempotency-Key` header + nullable column on `Order`) is a tracked follow-up.
- **Compensation on partial failure**: if any line's reservation fails, the handler releases all prior successful reservations synchronously, with up to three retries per release call. The order is marked `Failed` if all releases succeed, or `CompensationFailed` if any release exhausts all retries. The client receives `409 PlacementFailed` in both cases; the distinction is stored in the database for a future reconciliation worker to act on.
- **Stale reservations**: `StockReservation.ExpiresAt` is written at reservation time but not yet enforced by any background job. A future eviction pass will sweep expired, unreleased reservations; the column is the hook for that step.

## Tech Stack

| Concern | Choice |
|---|---|
| Runtime | .NET 10 |
| Database | PostgreSQL 16 + EF Core 10 (`Npgsql`) |
| API Gateway | YARP 2.2 |
| Inter-service comms | DAPR 1.17 (sidecar model): service invocation (sync) + pub/sub via RabbitMQ (async) |
| Identity | Keycloak 26.2 (OIDC / JWT Bearer) |
| Logging | Serilog → Seq |
| Testing | xUnit + NSubstitute + Testcontainers |

## Infrastructure

All infrastructure is Docker-based. Two compose files:

- `infra/docker-compose.yml`: backing services (PostgreSQL, Keycloak, Seq, DAPR placement/scheduler)
- `infra/docker-compose.services.yml`: application containers

### Running locally

```bash
# Start backing infrastructure
docker compose -f infra/docker-compose.yml up -d

# Start application services
docker compose -f infra/docker-compose.services.yml up -d
```

Key local endpoints:

| Service | URL |
|---|---|
| API Gateway | http://localhost:5000 |
| API Reference (Scalar) | http://localhost:5000/scalar |
| Aggregated OpenAPI spec | http://localhost:5000/openapi/v1.json |
| Seq (log UI) | http://localhost:5380 |
| Keycloak | http://localhost:8180 |
| RabbitMQ (management UI) | http://localhost:15672 (guest / guest) |

## Solution Structure

```
GameVaultEnviroment/
├── GameVault.slnx                          ← root solution
├── infra/                                  ← Docker Compose, DAPR components
└── src/
    ├── Gateway/
    │   └── GameVault.WebEdge.Api/
    ├── Services/
    │   ├── GameVault.Catalog/              ← reference implementation
    │   ├── GameVault.Customer/
    │   ├── GameVault.Order/
    │   └── GameVault.Notifications/
    └── SharedLibraries/
        └── GameVault.Common/
```

`GameVault.Catalog` is the canonical template service. Any new service must mirror its structure and conventions.

## Testing

Each service has two test projects under `tests/`:

| Project | Scope | Key dependencies |
|---|---|---|
| `*.UnitTests` | Domain invariants, Application handler logic | xUnit + NSubstitute |
| `*.IntegrationTests` | HTTP contracts, DB filters, EF configuration | xUnit + NSubstitute + WebApplicationFactory + Testcontainers (PostgreSQL) |

**Running unit tests** (no Docker required):
```bash
dotnet test src/Services/GameVault.Catalog/tests/GameVault.Catalog.UnitTests
dotnet test src/Services/GameVault.Customer/tests/GameVault.Customer.UnitTests
dotnet test src/Services/GameVault.Order/tests/GameVault.Order.UnitTests
dotnet test src/Services/GameVault.Notifications/tests/GameVault.Notifications.UnitTests
```

**Running integration tests** (Docker Desktop must be running):
```bash
dotnet test src/Services/GameVault.Catalog/tests/GameVault.Catalog.IntegrationTests
dotnet test src/Services/GameVault.Customer/tests/GameVault.Customer.IntegrationTests
dotnet test src/Services/GameVault.Order/tests/GameVault.Order.IntegrationTests
dotnet test src/Services/GameVault.Notifications/tests/GameVault.Notifications.IntegrationTests
```

Integration tests spin up a dedicated PostgreSQL container per test run, apply migrations automatically, and tear the container down when done. See `CLAUDE.md §7b` for the full authoring guide.

## Configuration

No secrets are committed to source control. `appsettings.json` holds structure and defaults only. Secrets are supplied via:

- `dotnet user-secrets` for local development
- Environment variables in Docker / CI
