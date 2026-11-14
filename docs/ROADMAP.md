# GameCore Restoration Roadmap

Status legend: ✅ done · ⏳ in progress

## Phase 0 — Legacy Preservation ✅
Preserve the original academic material and document the project history.

## Phase 1 — Database Redesign ✅
Rebuild the ER model, cardinalities, naming, normalization, and constraints.

## Phase 2 — SQL Server Core ✅
Create the production-ready relational schema, indexes, constraints, and seed data.

## Phase 3 — Advanced SQL ✅
Add views, stored procedures, transactions, reports, and documented SQL exercises.

## Phase 4 — .NET Foundation ✅
Create the .NET 10 solution with Domain, Application, Infrastructure, and Web API layers.

## Phase 5 — REST API ✅
Expose the core business modules through validated REST endpoints.

## Phase 6 — React Foundation ✅
Create the React + TypeScript frontend, routing, API client, and design system.

## Phase 7 — Game Management ✅
Implement games, genres, platforms, ratings, and related catalog features.

## Phase 8 — Customers & Sales ✅
Implement customers, sales, and sale details.

## Phase 9 — Employees & Distribution ✅
Implement employees, positions, countries, and distribution management.

## Phase 10 — Dashboard & Reports ✅
Add KPIs, charts, business reports, and SQL-driven analytics.

## Phase 11 — Security & Validation ✅
Add authentication, authorization, roles and hardened validation.

Delivered: JWT authentication; Admin / Manager / Viewer roles enforced by authorization policies
(Viewer is read-only); validation of every write request before it reaches SQL Server (400/404/409
as ProblemDetails); global exception handling without stack traces; health check that verifies
GameCoreDB; configurable CORS. Audit logging and user administration are out of scope for v1.

## Phase 12 — Portfolio Hardening ✅
Add tests, Docker, CI, documentation and setup scripts.

Delivered: xUnit API tests (SQLite) plus opt-in SQL Server integration tests; Vitest frontend tests;
reproducible docker compose stack (SQL Server healthcheck, database init, API, frontend);
GitHub Actions running builds, tests, NuGet/npm audits and a full-stack container smoke test;
setup-db / smoke-test / release-check scripts. Screenshots are not part of v1.

## Phase 13 — Release 1.0 ⏳
Finalize documentation and publish the restored project as a completed portfolio piece.

Pending: green CI (including the `full-stack` job) and a local `scripts/release-check.ps1` run on the
release branch, then the `v1.0.0` tag (see docs/RELEASE.md).
