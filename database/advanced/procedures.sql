USE GameCoreDB;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetCustomerPurchaseHistory
    @CustomerId int
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE CustomerId = @CustomerId)
        THROW 52001, 'Customer not found.', 1;

    SELECT
        s.SaleId,
        s.SaleDate,
        s.Status,
        g.GameId,
        g.Title,
        sd.Quantity,
        sd.UnitPrice,
        CAST(sd.Quantity * sd.UnitPrice AS decimal(14,2)) AS LineTotal
    FROM dbo.Sales s
    INNER JOIN dbo.SaleDetails sd ON sd.SaleId = s.SaleId
    INNER JOIN dbo.Games g ON g.GameId = sd.GameId
    WHERE s.CustomerId = @CustomerId
    ORDER BY s.SaleDate DESC, s.SaleId DESC, g.Title;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_RecordDistribution
    @GameId int,
    @CountryId int,
    @DistributionDate date,
    @Units int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Units < 0
        THROW 52010, 'Units cannot be negative.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Games WHERE GameId = @GameId)
        THROW 52011, 'Game not found.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Countries WHERE CountryId = @CountryId)
        THROW 52012, 'Country not found.', 1;

    BEGIN TRANSACTION;

    UPDATE dbo.Distributions
    SET Units = @Units
    WHERE GameId = @GameId
      AND CountryId = @CountryId
      AND DistributionDate = @DistributionDate;

    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO dbo.Distributions (GameId, CountryId, DistributionDate, Units)
        VALUES (@GameId, @CountryId, @DistributionDate, @Units);
    END;

    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_CreateSale
    @CustomerId int,
    @EmployeeId int = NULL,
    @ItemsJson nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE CustomerId = @CustomerId)
        THROW 52020, 'Customer not found.', 1;

    IF @EmployeeId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE EmployeeId = @EmployeeId AND IsActive = 1)
        THROW 52021, 'Active employee not found.', 1;

    IF ISJSON(@ItemsJson) <> 1
        THROW 52022, 'ItemsJson must be valid JSON.', 1;

    DECLARE @Items TABLE (
        GameId int NOT NULL,
        Quantity int NOT NULL
    );

    INSERT INTO @Items (GameId, Quantity)
    SELECT GameId, Quantity
    FROM OPENJSON(@ItemsJson)
    WITH (
        GameId int '$.gameId',
        Quantity int '$.quantity'
    );

    IF NOT EXISTS (SELECT 1 FROM @Items)
        THROW 52023, 'A sale requires at least one item.', 1;

    IF EXISTS (SELECT 1 FROM @Items WHERE GameId IS NULL OR Quantity IS NULL OR Quantity <= 0)
        THROW 52024, 'Every sale item requires a valid gameId and a positive quantity.', 1;

    IF EXISTS (
        SELECT GameId
        FROM @Items
        GROUP BY GameId
        HAVING COUNT(*) > 1
    )
        THROW 52025, 'Duplicate gameId values are not allowed in the sale payload.', 1;

    IF EXISTS (
        SELECT 1
        FROM @Items i
        LEFT JOIN dbo.Games g ON g.GameId = i.GameId AND g.IsActive = 1
        WHERE g.GameId IS NULL
    )
        THROW 52026, 'One or more games do not exist or are inactive.', 1;

    DECLARE @SaleId bigint;

    BEGIN TRANSACTION;

    INSERT INTO dbo.Sales (CustomerId, EmployeeId, SaleDate, Status)
    VALUES (@CustomerId, @EmployeeId, SYSUTCDATETIME(), 'Completed');

    SET @SaleId = SCOPE_IDENTITY();

    INSERT INTO dbo.SaleDetails (SaleId, GameId, Quantity, UnitPrice)
    SELECT
        @SaleId,
        g.GameId,
        i.Quantity,
        g.UnitPrice
    FROM @Items i
    INNER JOIN dbo.Games g ON g.GameId = i.GameId;

    COMMIT TRANSACTION;

    SELECT
        @SaleId AS SaleId,
        dbo.fn_SaleTotal(@SaleId) AS TotalAmount;
END;
GO
