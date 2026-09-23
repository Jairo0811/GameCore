# GameCore

**GameCore** is the 2026 restoration of a final project originally created for **SOF-006 — Introducción a las Bases de Datos**, taught by **Prof. Freidy Núñez** at ITLA.

The original work focused on relational modeling and SQL Server. The restored version preserves that history while extending the same domain into a full management application.

## Stack

- React 19.3 + TypeScript 7 + Vite 8
- ASP.NET Core / .NET 10
- Entity Framework Core + SQL Server
- JWT authentication
- Docker + GitHub Actions

The frontend versions are pinned to current stable releases verified in September 2026.

## Modules

Dashboard · Videojuegos · Clientes · Ventas · Empleados · Distribución · Catálogos · Advanced SQL

## Architecture

```text
React + TypeScript
       ↓
ASP.NET Core Web API
       ↓
Domain / Application / Infrastructure
       ↓
Entity Framework Core
       ↓
GameCoreDB (SQL Server)
```

## Restoration phases

0. ✅ Legacy Preservation
1. ✅ Database Redesign
2. ✅ SQL Server Core
3. ✅ Advanced SQL
4. ✅ .NET Foundation
5. ✅ REST API
6. ✅ React Foundation
7. ✅ Game Management
8. ✅ Customers & Sales
9. ✅ Employees & Distribution
10. ✅ Dashboard & Reports
11. ✅ Security & Validation
12. ✅ Portfolio Hardening
13. ✅ Release 1.0 implementation

## Local setup

### Database
```powershell
sqlcmd -S localhost -E -i database/setup.sql
```

### Backend
```powershell
dotnet restore src/backend/GameCore.sln
dotnet build src/backend/GameCore.sln -c Release
dotnet run --project src/backend/GameCore.WebAPI/GameCore.WebAPI.csproj
```

### Frontend
```powershell
cd src/frontend
npm install
npm run dev
```

### Demo login

- Email: `admin@gamecore.local`
- Password: `GameCore123!`

The demo account and JWT key are local-only defaults and must be replaced before any real deployment.

## Academic preservation

The original SQL remains in `docs/original/TAREA-FINAL.sql`; the restoration does not overwrite the historical submission. Personal identifiers from the original work are not reused in modern seed data.

## Verification status

The implementation is complete. **Runtime verification and the actual `v1.0.0` tag are intentionally deferred until the final local validation pass.** See `docs/RELEASE.md`.
