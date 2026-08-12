# GameVault

A microservices-based game store platform built on .NET 10, following Clean Architecture principles throughout.

## High-Level Design

```
Client
  │
  ▼
GameVault.WebEdge.Api   ← YARP reverse proxy / API gateway
  │                       Handles authentication at the edge
  ├──► GameVault.Catalog.Api    (game catalog)
  ├──► GameVault.Customer.Api   (customer management)
  └──► GameVault.Order.Api      (order processing)

Inter-service communication: DAPR (pub/sub + service invocation)
Identity: Keycloak (OIDC / JWT Bearer)
```

Each service owns its own PostgreSQL database. Services never share a database. Cross-service references carry no FK constraints — idempotency is enforced at the application level.

### Architecture per service

All services follow the same Clean Architecture layer structure:

```
Domain          ← POCOs, enums, domain logic — zero external dependencies
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
| `GameVault.WebEdge.Api` | `5000` | API gateway — routes public catalog and customer endpoints |
| `GameVault.Catalog.Api` | `5007` | Game catalog — products and internal stock reservation endpoints |
| `GameVault.Customer.Api` | `5008` | Customer management, Keycloak integration |
| `GameVault.Order.Api` | `5009` | Order processing, invokes Catalog reservation endpoints via DAPR |

## Gateway Routing Conventions

`GameVault.WebEdge.Api`'s YARP routes wire each service differently on purpose — this is not an inconsistency to fix:

- **Catalog (public)**: two explicit GET-only routes proxy `GET /catalog/api/products` and `GET /catalog/api/products/{id}`, stripping the `/catalog` prefix before forwarding. Reservation endpoints (`POST /api/products/{id}/reservations`, `POST /api/orders/.../confirm`, `POST /api/orders/.../release`) have **no matching YARP route** — they are reachable only through the DAPR sidecar and are never exposed externally.
- **Customer**: the `/customers/{**catch-all}` route has no transform at all — the path is forwarded unchanged, so `CustomersController` uses a plain `[controller]` route (`Customers`) to match. Public URL: `/customers/{id}`.

Making both controllers share one route-attribute convention would require either copying Catalog's redundant `/api` segment into Customer's public URLs (`/customers/api/Customers/{id}`) or simplifying Catalog's gateway wiring instead (a change to the template service, not Customer). Neither is worth it just to make the two `[Route(...)]` attributes read the same — each is correct for its own gateway route.

## Cross-Service Consistency

There is no distributed transaction across a service's own database and another service/system it depends on, so each such reference documents its own idempotency and compensation strategy explicitly (per repo convention — see `StockReservation.OrderId` in Catalog for the DB-level example).

### Customer ↔ Keycloak (identity)

- **Register**: the Keycloak user is created first, then the local `Customer` row is written. If the local write fails after Keycloak succeeded, the handler compensates by deleting the just-created Keycloak user before rethrowing. Keycloak's own email-uniqueness rejection (409) is surfaced as an expected `EmailConflict` result, not an exception.
- **Known gap — no idempotency key on Register**: if a client retries `POST /customers/register` after a timeout (the first attempt actually succeeded but the response was lost), there is nothing that recognizes the retry as the same logical request. Today the retry just calls Keycloak again, which happens to reject it with a 409 on the same email — so it doesn't create a duplicate account, but it also doesn't return the original success (the client gets `EmailConflict` instead of the `customerId` it already has). A proper fix needs a client-supplied idempotency key (e.g. an `Idempotency-Key` header) plus a new unique, nullable column on `Customer` to record and replay it — a schema change, not yet implemented, tracked as a follow-up.
- **Delete**: the Keycloak user is deleted first, then the local row is soft-deleted. If the Keycloak call fails, nothing is written locally — the customer stays active on both sides rather than ending up soft-deleted locally while still able to authenticate via Keycloak. `DeleteUserAsync` treats an already-deleted (404) Keycloak user as success, so a retried delete request can still complete the local write.
- **Known gap**: in the narrow window where the Keycloak deletion succeeds but the subsequent local `SaveChangesAsync` fails (DB unavailable, concurrency conflict), the customer can no longer authenticate but the local record still shows active. This is retry-safe (the Keycloak call is idempotent) but not automatically reconciled today. Permanently closing this window requires an outbox pattern — a durable "pending Keycloak deletion" record written in the same local transaction, processed by a background worker with retries — which is not yet implemented and is tracked as a follow-up.
- **Account lifecycle**: `RegistrationStatus` currently has a single member (`Completed`) — no email verification or suspend states. This is a deliberate MVP scope decision, not an oversight; the enum is left in place to extend once those states are needed.
- **Reactivate**: `POST /customers/reactivate` (anonymous) accepts `{ email, password }`. The handler finds the soft-deleted local row by email, calls Keycloak's Admin API to re-create the user with the *same UUID* as the original `Customer.Id` (Keycloak accepts an explicit `"id"` in the create payload), then clears `IsDeleted`/`DeletedAt` on the local row. The customer's profile data (name, phone) is preserved from before deletion. If the local `SaveChangesAsync` fails after Keycloak succeeds, the handler compensates by deleting the just-created Keycloak user before rethrowing, mirroring the Register compensation pattern.
- **Known gap — no email-change endpoint**: `UpdateCustomerRequest` only covers `FirstName`/`LastName`/`PhoneNumber`; there is no way to change a registered email either locally or in Keycloak today. Tracked as a follow-up.
- **Known gap — no password reset/change flow**: `IKeycloakAdminClient` only exposes create/delete; there is no self-service password reset or change capability. Tracked as a follow-up.

## Tech Stack

| Concern | Choice |
|---|---|
| Runtime | .NET 10 |
| Database | PostgreSQL 16 + EF Core 10 (`Npgsql`) |
| API Gateway | YARP 2.2 |
| Inter-service comms | DAPR 1.17 (sidecar model) |
| Identity | Keycloak 26.2 (OIDC / JWT Bearer) |
| Logging | Serilog → Seq |
| Testing | xUnit + NSubstitute + Testcontainers |

## Infrastructure

All infrastructure is Docker-based. Two compose files:

- `infra/docker-compose.yml` — backing services (PostgreSQL, Keycloak, Seq, DAPR placement/scheduler)
- `infra/docker-compose.services.yml` — application containers

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
| Seq (log UI) | http://localhost:5380 |
| Keycloak | http://localhost:8180 |

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
    │   └── GameVault.Order/
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
```

**Running integration tests** (Docker Desktop must be running):
```bash
dotnet test src/Services/GameVault.Catalog/tests/GameVault.Catalog.IntegrationTests
dotnet test src/Services/GameVault.Customer/tests/GameVault.Customer.IntegrationTests
```

Integration tests spin up a dedicated PostgreSQL container per test run, apply migrations automatically, and tear the container down when done. See `CLAUDE.md §7b` for the full authoring guide.

## Configuration

No secrets are committed to source control. `appsettings.json` holds structure and defaults only. Secrets are supplied via:

- `dotnet user-secrets` for local development
- Environment variables in Docker / CI
