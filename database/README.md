# GameCoreDB

This folder contains the SQL Server implementation of the GameCore database.

## Files

- `schema.sql` — creates `GameCoreDB`, tables, constraints, and indexes.
- `seed.sql` — inserts safe demo data inspired by the original SOF-006 project.
- `validate.sql` — validates core tables, seed data, and constraints.
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

From the repository root:

```powershell
sqlcmd -S localhost -E -i database/setup.sql
```

For SQL Server Express:

```powershell
sqlcmd -S .\SQLEXPRESS -E -i database/setup.sql
```

## Rebuild during development

```powershell
sqlcmd -S localhost -E -i database/reset.sql
sqlcmd -S localhost -E -i database/setup.sql
```

> `reset.sql` deletes the entire GameCoreDB database. It is intended only for local development.

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
