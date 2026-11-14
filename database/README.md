# GameCoreDB

This folder contains the SQL Server implementation of the GameCore database.

## Files

- `schema.sql` — creates `GameCoreDB`, tables, constraints, and indexes.
- `seed.sql` — inserts safe demo data inspired by the original SOF-006 project.
- `security.sql` — `AppUsers` table and local demo accounts (Admin, Manager, Viewer).
- `validate.sql` — validates core tables, seed data, Unicode text, demo roles, constraints and the filtered unique email indexes (it inserts test rows inside transactions that are always rolled back).
- `reset.sql` — development-only script that drops `GameCoreDB`.
- `setup.sql` — SQLCMD bootstrap that installs the full database.
- `advanced/views.sql` — reporting and catalog views.
- `advanced/functions.sql` — reusable database functions.
- `advanced/procedures.sql` — transactional and query stored procedures.
- `advanced/reports.sql` — portfolio/reporting query examples.
- `advanced/validate.sql` — verifies the Phase 3 database objects.

## Requirements

- Microsoft SQL Server with JSON support (SQL Server 2016+)
- SQL Server Management Studio with **SQLCMD Mode**, or the `sqlcmd` command-line utility.

## First setup

From the repository root (recommended):

```powershell
.\scripts\setup-db.ps1              # creates GameCoreDB if missing, otherwise only validates it
.\scripts\setup-db.ps1 -SqlExpress
```

Equivalent raw command (`-C` trusts the local certificate; `-f 65001` reads the scripts as UTF-8):

```powershell
sqlcmd -S localhost -E -C -f 65001 -i database/setup.sql
```

## Rebuild during development

```powershell
.\scripts\setup-db.ps1 -Reset       # asks for confirmation, then drops and recreates GameCoreDB
```

> `reset.sql` deletes the entire GameCoreDB database. It is intended only for local development.

## Optional emails

`Customers.Email` and `Employees.Email` are optional. Uniqueness is enforced by filtered unique indexes
(`UX_Customers_Email`, `UX_Employees_Email` … `WHERE Email IS NOT NULL`), so any number of rows may have no
email while non-NULL values stay unique. `AppUsers.Email` is mandatory and keeps a normal UNIQUE constraint.

### SQLCMD compatibility

`schema.sql` explicitly enables the SQL Server session options required by filtered indexes, including `QUOTED_IDENTIFIER`, `ANSI_NULLS`, `ANSI_WARNINGS`, `ARITHABORT`, and related settings. This prevents environment-dependent failures when running the bootstrap from `sqlcmd` or SSMS.

## Advanced SQL objects

### Views

- `dbo.vw_SaleSummary`
- `dbo.vw_GameSalesPerformance`
- `dbo.vw_GameCatalog`
- `dbo.vw_DistributionSummary`

### Functions

- `dbo.fn_SaleTotal(@SaleId)`
- `dbo.fn_CustomerLifetimeValue(@CustomerId)`

### Stored procedures

- `dbo.usp_GetCustomerPurchaseHistory`
- `dbo.usp_RecordDistribution`
- `dbo.usp_CreateSale`

### Transactional sale example

```sql
EXEC dbo.usp_CreateSale
    @CustomerId = 1,
    @EmployeeId = 1,
    @ItemsJson = N'[
        {"gameId": 1, "quantity": 1},
        {"gameId": 5, "quantity": 2}
    ]';
```

The procedure validates the payload and inserts `Sales` and `SaleDetails` atomically.

## Reports

Run `database/advanced/reports.sql` separately when you want the example reports. It is intentionally not part of `setup.sql` because reports read data rather than install database objects.

## Phase 3 coverage

The database now demonstrates:

- INNER and LEFT JOIN
- aggregation and GROUP BY
- `STRING_AGG`
- views
- scalar functions
- stored procedures
- JSON parsing with `OPENJSON`
- explicit transactions
- `XACT_ABORT`
- validation with `THROW`
- reusable business/reporting queries


## Recommended UTF-8 bootstrap

To avoid mojibake in names such as `Pokémon`, `Peña`, `Ibáñez`, `México`, and `República Dominicana`, run SQLCMD explicitly as UTF-8 (`validate.sql` fails if the seed was decoded incorrectly):

```powershell
.\scripts\setup-db.ps1
```

For SQL Server Express:

```powershell
.\scripts\setup-db.ps1 -SqlExpress
```

The wrapper uses `sqlcmd -f 65001` so Unicode literals in the seed are read correctly before they reach the `nvarchar` columns.
