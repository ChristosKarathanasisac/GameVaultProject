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
  └──► GameVault.Customer.Api   (customer management)

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
| `GameVault.WebEdge.Api` | `5000` | API gateway — routes `/catalog/**` and `/customers/**` |
| `GameVault.Catalog.Api` | `5007` | Game catalog management |
| `GameVault.Customer.Api` | `5008` | Customer management, Keycloak integration |

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
    │   └── GameVault.Customer/
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
dotnet test src/Services/GameVault.Customer/tests/GameVault.Customer.UnitTests
```

**Running integration tests** (Docker Desktop must be running):
```bash
dotnet test src/Services/GameVault.Customer/tests/GameVault.Customer.IntegrationTests
```

Integration tests spin up a dedicated PostgreSQL container per test run, apply migrations automatically, and tear the container down when done. See `CLAUDE.md §7b` for the full authoring guide.

## Configuration

No secrets are committed to source control. `appsettings.json` holds structure and defaults only. Secrets are supplied via:

- `dotnet user-secrets` for local development
- Environment variables in Docker / CI
