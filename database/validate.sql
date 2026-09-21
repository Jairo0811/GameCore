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

DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;
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
