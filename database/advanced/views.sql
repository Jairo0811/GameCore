USE GameCoreDB;
GO

CREATE OR ALTER VIEW dbo.vw_SaleSummary
AS
SELECT
    s.SaleId,
    s.SaleDate,
    s.Status,
    c.CustomerId,
    CONCAT(c.FirstName, N' ', c.LastName) AS CustomerName,
    e.EmployeeId,
    CASE
        WHEN e.EmployeeId IS NULL THEN NULL
        ELSE CONCAT(e.FirstName, N' ', e.LastName)
    END AS EmployeeName,
    SUM(sd.Quantity) AS TotalItems,
    CAST(SUM(sd.Quantity * sd.UnitPrice) AS decimal(14,2)) AS TotalAmount
FROM dbo.Sales s
INNER JOIN dbo.Customers c ON c.CustomerId = s.CustomerId
LEFT JOIN dbo.Employees e ON e.EmployeeId = s.EmployeeId
INNER JOIN dbo.SaleDetails sd ON sd.SaleId = s.SaleId
GROUP BY
    s.SaleId,
    s.SaleDate,
    s.Status,
    c.CustomerId,
    c.FirstName,
    c.LastName,
    e.EmployeeId,
    e.FirstName,
    e.LastName;
GO

CREATE OR ALTER VIEW dbo.vw_GameSalesPerformance
AS
SELECT
    g.GameId,
    g.Title,
    g.ReleaseDate,
    g.UnitPrice AS CurrentUnitPrice,
    COALESCE(SUM(CASE WHEN s.Status = 'Completed' THEN sd.Quantity ELSE 0 END), 0) AS UnitsSold,
    CAST(COALESCE(SUM(
        CASE WHEN s.Status = 'Completed'
             THEN sd.Quantity * sd.UnitPrice
             ELSE 0
        END
    ), 0) AS decimal(14,2)) AS Revenue
FROM dbo.Games g
LEFT JOIN dbo.SaleDetails sd ON sd.GameId = g.GameId
LEFT JOIN dbo.Sales s ON s.SaleId = sd.SaleId
GROUP BY
    g.GameId,
    g.Title,
    g.ReleaseDate,
    g.UnitPrice;
GO

CREATE OR ALTER VIEW dbo.vw_GameCatalog
AS
SELECT
    g.GameId,
    g.Title,
    ar.Code AS AgeRatingCode,
    ar.Name AS AgeRatingName,
    g.ReleaseDate,
    g.UnitPrice,
    g.IsActive,
    STRING_AGG(DISTINCT_GENRES.Name, N', ') WITHIN GROUP (ORDER BY DISTINCT_GENRES.Name) AS Genres
FROM dbo.Games g
LEFT JOIN dbo.AgeRatings ar ON ar.AgeRatingId = g.AgeRatingId
OUTER APPLY (
    SELECT DISTINCT ge.Name
    FROM dbo.GameGenres gg
    INNER JOIN dbo.Genres ge ON ge.GenreId = gg.GenreId
    WHERE gg.GameId = g.GameId
) DISTINCT_GENRES
GROUP BY
    g.GameId,
    g.Title,
    ar.Code,
    ar.Name,
    g.ReleaseDate,
    g.UnitPrice,
    g.IsActive;
GO

CREATE OR ALTER VIEW dbo.vw_DistributionSummary
AS
SELECT
    d.DistributionId,
    d.DistributionDate,
    d.Units,
    g.GameId,
    g.Title AS GameTitle,
    c.CountryId,
    c.Iso2,
    c.Name AS CountryName
FROM dbo.Distributions d
INNER JOIN dbo.Games g ON g.GameId = d.GameId
INNER JOIN dbo.Countries c ON c.CountryId = d.CountryId;
GO
