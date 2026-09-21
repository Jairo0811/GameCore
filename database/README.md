# GameCoreDB

This folder contains the SQL Server implementation of the GameCore database.

## Files

- `schema.sql` — creates `GameCoreDB`, tables, constraints, and indexes.
- `seed.sql` — inserts safe demo data inspired by the original SOF-006 project.
- `validate.sql` — validates core tables, seed data, and constraints.
- `reset.sql` — development-only script that drops `GameCoreDB`.
- `setup.sql` — SQLCMD bootstrap that executes schema, seed, and validation in order.

## Requirements

- Microsoft SQL Server
- SQL Server Management Studio with **SQLCMD Mode**, or the `sqlcmd` command-line utility.

## First setup

From the repository root:

```powershell
sqlcmd -S localhost -E -i database/setup.sql
```

Replace `localhost` with the SQL Server instance used on your machine.

For a named SQL Server Express instance, for example:

```powershell
sqlcmd -S .\SQLEXPRESS -E -i database/setup.sql
```

## Rebuild during development

First run:

```powershell
sqlcmd -S localhost -E -i database/reset.sql
```

Then:

```powershell
sqlcmd -S localhost -E -i database/setup.sql
```

> `reset.sql` deletes the entire GameCoreDB database. It is intended only for local development.

## Design guarantees

The Phase 2 schema enforces:

- Primary and foreign keys
- Unique catalog values
- Non-negative game prices and distribution quantities
- Positive sale quantities
- Controlled sale statuses
- Two-letter uppercase country codes
- Non-blank required names
- Indexes on important FK/query paths

Advanced views, stored procedures, functions, and reporting queries intentionally belong to **Phase 3 — Advanced SQL**.
