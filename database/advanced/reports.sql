USE GameCoreDB;
GO

-- 1. Top-selling games
SELECT TOP (10)
    GameId,
    Title,
    UnitsSold,
    Revenue
FROM dbo.vw_GameSalesPerformance
ORDER BY UnitsSold DESC, Revenue DESC, Title;
GO

-- 2. Completed sales by month
SELECT
    DATEFROMPARTS(YEAR(SaleDate), MONTH(SaleDate), 1) AS SalesMonth,
    COUNT(*) AS SalesCount,
    CAST(SUM(TotalAmount) AS decimal(14,2)) AS Revenue
FROM dbo.vw_SaleSummary
WHERE Status = 'Completed'
GROUP BY DATEFROMPARTS(YEAR(SaleDate), MONTH(SaleDate), 1)
ORDER BY SalesMonth;
GO

-- 3. Customer lifetime value
SELECT
    c.CustomerId,
    CONCAT(c.FirstName, N' ', c.LastName) AS CustomerName,
    dbo.fn_CustomerLifetimeValue(c.CustomerId) AS LifetimeValue
FROM dbo.Customers c
ORDER BY LifetimeValue DESC, CustomerName;
GO

-- 4. Distribution by country
SELECT
    CountryName,
    SUM(Units) AS DistributedUnits
FROM dbo.vw_DistributionSummary
GROUP BY CountryName
ORDER BY DistributedUnits DESC, CountryName;
GO

-- 5. Catalog with platforms
SELECT
    g.GameId,
    g.Title,
    STRING_AGG(p.Name, N', ') WITHIN GROUP (ORDER BY p.Name) AS Platforms
FROM dbo.Games g
LEFT JOIN dbo.GamePlatforms gp ON gp.GameId = g.GameId
LEFT JOIN dbo.Platforms p ON p.PlatformId = gp.PlatformId
GROUP BY g.GameId, g.Title
ORDER BY g.Title;
GO
