# GameCore

GameCore is a modern reconstruction of an academic database project originally created as the final assignment for **SOF-006 — Introducción a las Bases de Datos**, taught by **Prof. Freidy Núñez**.

The original project modeled a video game company and focused on relational database fundamentals: requirements analysis, entity-relationship modeling, normalization, SQL Server, inserts, filters, joins, and queries.

## Restoration goal

The 2026 restoration preserves the academic origin while rebuilding the solution with technologies learned later:

- **Frontend:** React + TypeScript + Vite
- **Backend:** ASP.NET Core / .NET 10 Web API
- **ORM:** Entity Framework Core
- **Database:** SQL Server
- **Architecture:** Clean Architecture
- **Documentation:** original academic artifacts, ERD, migration notes, and technical decisions

## Scope

GameCore is a management system for a video game company. It is intentionally different from a retail POS: its focus is the company's catalog, customers, sales, employees, platforms, genres, countries, distribution, and reporting.

## Planned modules

- Dashboard
- Games
- Genres and platforms
- Customers
- Sales
- Employees
- Distribution
- Reports
- SQL Lab

## Project evolution

```text
SOF-006 academic project
        ↓
ER modeling + SQL Server
        ↓
Database redesign
        ↓
SQL Server Core
        ↓
Advanced SQL
        ↓
.NET 10 REST API
        ↓
React + TypeScript
        ↓
GameCore
```

## Repository structure

```text
GameCore/
├── src/
│   ├── backend/
│   └── frontend/
├── database/
│   ├── advanced/
│   ├── schema.sql
│   ├── seed.sql
│   ├── setup.sql
│   ├── reset.sql
│   └── validate.sql
├── docs/
│   ├── original/
│   ├── database/
│   ├── diagrams/
│   └── screenshots/
└── README.md
```

## Restoration phases

0. ✅ Legacy Preservation
1. ✅ Database Redesign
2. ✅ SQL Server Core
3. ✅ Advanced SQL
4. ✅ .NET Foundation
5. REST API
6. React Foundation
7. Game Management
8. Customers & Sales
9. Employees & Distribution
10. Dashboard & Reports
11. Security & Validation
12. Portfolio Hardening
13. Release 1.0

## Academic origin

- **Course:** SOF-006 — Introducción a las Bases de Datos
- **Professor:** Freidy Núñez
- **Institution:** ITLA
- **Type:** Final academic project

> The restored version does not claim that React or .NET were part of the original submission. They are technologies incorporated later as part of the project's modernization.

## Legacy preservation

The original SQL is preserved in `docs/original/TAREA-FINAL.sql`. The restored schema is intentionally separate so the project history remains visible instead of overwriting the original work.

## Privacy

Any personal data contained in the historical academic material must be anonymized before being reused as public seed data. The modern `database/seed.sql` retains the legacy game catalog while replacing sensitive personal identifiers with safe demo values.

## Status

**Phase 4 — .NET Foundation: complete.**

The repository now contains a .NET 10 Clean Architecture foundation with Domain, Application, Infrastructure, and WebAPI projects, plus Entity Framework Core mapping for GameCoreDB.

Next: **Phase 5 — REST API.**
