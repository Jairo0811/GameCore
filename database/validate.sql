IF DB_ID(N'GameCoreDB') IS NULL
    THROW 51000, 'GameCoreDB does not exist.', 1;
GO

USE GameCoreDB;
GO

SET NOCOUNT ON;

DECLARE @ExpectedTables int = 15;
DECLARE @ActualTables int;

SELECT @ActualTables = COUNT(*)
FROM sys.tables
WHERE schema_id = SCHEMA_ID(N'dbo')
  AND name IN (
    N'Companies', N'Branches', N'JobPositions', N'Employees',
    N'Customers', N'AgeRatings', N'Games', N'Genres',
    N'GameGenres', N'Platforms', N'GamePlatforms', N'Countries',
    N'Distributions', N'Sales', N'SaleDetails'
  );

IF @ActualTables <> @ExpectedTables
    THROW 51001, 'GameCoreDB validation failed: one or more core tables are missing.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.Games)
    THROW 51002, 'GameCoreDB validation failed: seed games were not loaded.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.Companies)
    THROW 51003, 'GameCoreDB validation failed: seed company was not loaded.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.Customers)
    THROW 51004, 'GameCoreDB validation failed: seed customers were not loaded.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.Sales)
    THROW 51005, 'GameCoreDB validation failed: seed sales were not loaded.', 1;

-- Unicode: the seed must be loaded as UTF-8 (sqlcmd -f 65001). The expected values
-- are built with NCHAR so this check does not depend on how this file is decoded.
IF NOT EXISTS (SELECT 1 FROM dbo.Games WHERE Title = N'Pok' + NCHAR(233) + N'mon Sun')
   OR NOT EXISTS (SELECT 1 FROM dbo.Countries WHERE Name = N'M' + NCHAR(233) + N'xico')
    THROW 51006, 'GameCoreDB validation failed: Unicode seed data is corrupted. Re-run the bootstrap with sqlcmd -f 65001.', 1;

IF OBJECT_ID(N'dbo.AppUsers', N'U') IS NULL
    THROW 51007, 'GameCoreDB validation failed: dbo.AppUsers is missing (security.sql was not applied).', 1;

IF (SELECT COUNT(DISTINCT Role) FROM dbo.AppUsers WHERE IsActive = 1 AND Role IN ('Admin', 'Manager', 'Viewer')) <> 3
    THROW 51008, 'GameCoreDB validation failed: demo users for Admin, Manager and Viewer were not loaded.', 1;

-- Optional emails: uniqueness must be enforced by filtered unique indexes, not by
-- UNIQUE constraints (which would allow only a single NULL row).
IF EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE name IN (N'UQ_Customers_Email', N'UQ_Employees_Email')
)
    THROW 51009, 'GameCoreDB validation failed: legacy UNIQUE email constraints are still present. Recreate the database.', 1;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Customers') AND name = N'UX_Customers_Email'
      AND is_unique = 1 AND has_filter = 1 AND filter_definition = N'([Email] IS NOT NULL)'
)
OR NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Employees') AND name = N'UX_Employees_Email'
      AND is_unique = 1 AND has_filter = 1 AND filter_definition = N'([Email] IS NOT NULL)'
)
    THROW 51010, 'GameCoreDB validation failed: filtered unique email indexes are missing.', 1;

DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;
GO

-- Behavioural check of the email rules. Each check runs in its own transaction that
-- is always rolled back, so no validation rows are left behind.
SET NOCOUNT ON;
SET XACT_ABORT OFF;

DECLARE @BranchId int = (SELECT TOP (1) BranchId FROM dbo.Branches ORDER BY BranchId);
DECLARE @JobPositionId int = (SELECT TOP (1) JobPositionId FROM dbo.JobPositions ORDER BY JobPositionId);
DECLARE @Failure nvarchar(400) = NULL;

-- 1. Several customers and employees without email must be accepted.
BEGIN TRANSACTION;
BEGIN TRY
    INSERT INTO dbo.Customers (FirstName, LastName, Email)
    VALUES (N'Validation', N'NullEmailA', NULL), (N'Validation', N'NullEmailB', NULL);

    INSERT INTO dbo.Employees (BranchId, JobPositionId, FirstName, LastName, Email)
    VALUES (@BranchId, @JobPositionId, N'Validation', N'NullEmailA', NULL),
           (@BranchId, @JobPositionId, N'Validation', N'NullEmailB', NULL);
END TRY
BEGIN CATCH
    SET @Failure = N'multiple customers/employees without email were rejected: ' + ERROR_MESSAGE();
END CATCH;
IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

-- 2. A repeated non-NULL customer email must be rejected.
IF @Failure IS NULL
BEGIN
    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO dbo.Customers (FirstName, LastName, Email)
        VALUES (N'Validation', N'DuplicateA', 'validation.duplicate@gamecore.local'),
               (N'Validation', N'DuplicateB', 'validation.duplicate@gamecore.local');
        SET @Failure = N'duplicate customer emails were accepted.';
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() NOT IN (2601, 2627)
            SET @Failure = N'unexpected error while checking customer email uniqueness: ' + ERROR_MESSAGE();
    END CATCH;
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
END;

-- 3. A repeated non-NULL employee email must be rejected.
IF @Failure IS NULL
BEGIN
    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO dbo.Employees (BranchId, JobPositionId, FirstName, LastName, Email)
        VALUES (@BranchId, @JobPositionId, N'Validation', N'DuplicateA', 'validation.duplicate@gamecore.local'),
               (@BranchId, @JobPositionId, N'Validation', N'DuplicateB', 'validation.duplicate@gamecore.local');
        SET @Failure = N'duplicate employee emails were accepted.';
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() NOT IN (2601, 2627)
            SET @Failure = N'unexpected error while checking employee email uniqueness: ' + ERROR_MESSAGE();
    END CATCH;
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
END;

IF @Failure IS NOT NULL
BEGIN
    DECLARE @Message nvarchar(2048) = N'GameCoreDB validation failed: ' + @Failure;
    THROW 51011, @Message, 1;
END;
GO

SELECT
    (SELECT COUNT(*) FROM dbo.Companies)      AS Companies,
    (SELECT COUNT(*) FROM dbo.Branches)       AS Branches,
    (SELECT COUNT(*) FROM dbo.Employees)      AS Employees,
    (SELECT COUNT(*) FROM dbo.Customers)      AS Customers,
    (SELECT COUNT(*) FROM dbo.Games)          AS Games,
    (SELECT COUNT(*) FROM dbo.Genres)         AS Genres,
    (SELECT COUNT(*) FROM dbo.Platforms)      AS Platforms,
    (SELECT COUNT(*) FROM dbo.Countries)      AS Countries,
    (SELECT COUNT(*) FROM dbo.Distributions)  AS Distributions,
    (SELECT COUNT(*) FROM dbo.Sales)          AS Sales,
    (SELECT COUNT(*) FROM dbo.SaleDetails)    AS SaleDetails;
GO

PRINT 'GameCoreDB validation passed.';
GO
