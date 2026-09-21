USE GameCoreDB;
GO

DECLARE @MissingObjects TABLE (ObjectName sysname NOT NULL);

IF OBJECT_ID(N'dbo.vw_SaleSummary', N'V') IS NULL INSERT INTO @MissingObjects VALUES (N'dbo.vw_SaleSummary');
IF OBJECT_ID(N'dbo.vw_GameSalesPerformance', N'V') IS NULL INSERT INTO @MissingObjects VALUES (N'dbo.vw_GameSalesPerformance');
IF OBJECT_ID(N'dbo.vw_GameCatalog', N'V') IS NULL INSERT INTO @MissingObjects VALUES (N'dbo.vw_GameCatalog');
IF OBJECT_ID(N'dbo.vw_DistributionSummary', N'V') IS NULL INSERT INTO @MissingObjects VALUES (N'dbo.vw_DistributionSummary');

IF OBJECT_ID(N'dbo.fn_SaleTotal', N'FN') IS NULL INSERT INTO @MissingObjects VALUES (N'dbo.fn_SaleTotal');
IF OBJECT_ID(N'dbo.fn_CustomerLifetimeValue', N'FN') IS NULL INSERT INTO @MissingObjects VALUES (N'dbo.fn_CustomerLifetimeValue');

IF OBJECT_ID(N'dbo.usp_GetCustomerPurchaseHistory', N'P') IS NULL INSERT INTO @MissingObjects VALUES (N'dbo.usp_GetCustomerPurchaseHistory');
IF OBJECT_ID(N'dbo.usp_RecordDistribution', N'P') IS NULL INSERT INTO @MissingObjects VALUES (N'dbo.usp_RecordDistribution');
IF OBJECT_ID(N'dbo.usp_CreateSale', N'P') IS NULL INSERT INTO @MissingObjects VALUES (N'dbo.usp_CreateSale');

IF EXISTS (SELECT 1 FROM @MissingObjects)
BEGIN
    SELECT ObjectName AS MissingObject FROM @MissingObjects;
    THROW 53000, 'Advanced SQL validation failed: expected objects are missing.', 1;
END;
GO

IF dbo.fn_SaleTotal(1) < 0
    THROW 53001, 'Advanced SQL validation failed: invalid sale total.', 1;
GO

PRINT 'Advanced SQL validation passed.';
GO
