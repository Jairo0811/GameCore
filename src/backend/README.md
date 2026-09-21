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
└── GameCore.WebAPI/
```

## Architecture

### GameCore.Domain

Contains the core domain entities. It has no dependency on EF Core, ASP.NET Core, or infrastructure concerns.

### GameCore.Application

Reserved for application use cases, contracts, DTOs, validation, and orchestration. Phase 4 only establishes the project boundary; business use cases begin in Phase 5.

### GameCore.Infrastructure

Contains SQL Server and EF Core integration. `GameCoreDbContext` maps the existing `GameCoreDB` schema using Fluent API.

### GameCore.WebAPI

ASP.NET Core host responsible for configuration, dependency injection, OpenAPI, and HTTP endpoints.

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
- `GET /health` — basic process health
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

## Phase boundary

Phase 4 establishes architecture, domain entities, EF Core mapping, dependency injection, and the API host.

CRUD endpoints, application use cases, DTOs, request validation, and API business behavior belong to **Phase 5 — REST API**.
