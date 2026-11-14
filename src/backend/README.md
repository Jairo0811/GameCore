# GameCore Backend

The backend is built with **.NET 10**, ASP.NET Core Web API, Entity Framework Core, and SQL Server.

## Solution structure

```text
src/backend/
├── GameCore.sln
├── Directory.Build.props
├── GameCore.Domain/
├── GameCore.Application/
├── GameCore.Infrastructure/
├── GameCore.WebAPI/
└── GameCore.Tests/
```

## Architecture

### GameCore.Domain

Contains the core domain entities. It has no dependency on EF Core, ASP.NET Core, or infrastructure concerns.

### GameCore.Application

Reserved for application use cases, contracts, DTOs, validation, and orchestration. Phase 4 only establishes the project boundary; business use cases begin in Phase 5.

### GameCore.Infrastructure

Contains SQL Server and EF Core integration. `GameCoreDbContext` maps the existing `GameCoreDB` schema using Fluent API.

### GameCore.WebAPI

ASP.NET Core host responsible for configuration, dependency injection, OpenAPI and HTTP endpoints:

- `ApiEndpoints.cs` — minimal API endpoints and request records;
- `Security/` — role names, authorization policies (`GameCore.Read`: Admin/Manager/Viewer; `GameCore.Write`: Admin/Manager) and PBKDF2 password hashing;
- `Validation/RequestValidator.cs` — shape validation of write requests (lengths mirror `database/schema.sql`);
- `Errors/` — ProblemDetails helpers and the global exception handler (no stack traces; SQL unique/FK races mapped to 409/400);
- `Health/` — `/health` check that verifies GameCoreDB connectivity (HTTP 503 when unreachable).

### GameCore.Tests

xUnit tests. API tests run the real pipeline (JWT, policies, validation, ProblemDetails) against an isolated in-memory SQLite database created from the EF model, with a retrying execution strategy like production. `SqlServer/` contains integration tests that run only when `GAMECORE_TEST_SQLSERVER` is set (CI sets it against the Docker stack).

## Database source of truth

During the database-first restoration stages, the SQL files under `database/` remain the authoritative schema definition.

Entity Framework Core is currently used to map and access that schema. EF migrations are intentionally not introduced in Phase 4 to avoid competing schema histories.

## Requirements

- .NET 10 SDK
- SQL Server
- GameCoreDB created through `database/setup.sql`

## Restore and build

From the repository root:

```powershell
dotnet restore src/backend/GameCore.sln
dotnet build src/backend/GameCore.sln --configuration Release
```

## Run the API

```powershell
dotnet run --project src/backend/GameCore.WebAPI/GameCore.WebAPI.csproj
```

The foundation exposes:

- `GET /` — service metadata
- `GET /health` — health including GameCoreDB connectivity (503 when unhealthy)
- `/api/...` — business endpoints (see the main README for routes and roles)
- `/openapi/v1.json` — OpenAPI document in Development

## Connection string

The repository contains a local Windows-authentication default:

```text
Server=localhost;Database=GameCoreDB;Trusted_Connection=True;TrustServerCertificate=True
```

For machine-specific or credential-based values, prefer .NET User Secrets rather than committing credentials.

From `src/backend/GameCore.WebAPI`:

```powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:GameCoreDb" "Server=YOUR_SERVER;Database=GameCoreDB;Trusted_Connection=True;TrustServerCertificate=True"
```

Environment variables are also supported by ASP.NET Core configuration:

```text
ConnectionStrings__GameCoreDb
```

## Tests

```powershell
dotnet test src/backend/GameCore.sln -c Release
```

## Other configuration

- `Jwt:Key` — at least 32 bytes; in `Production` it may not start with `CHANGE-ME`.
- `Cors:AllowedOrigins` — array of allowed browser origins (default `http://localhost:5173`; `*` is rejected). Environment form: `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, …
