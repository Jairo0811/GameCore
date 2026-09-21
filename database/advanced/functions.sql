USE GameCoreDB;
GO

CREATE OR ALTER FUNCTION dbo.fn_SaleTotal (@SaleId bigint)
RETURNS decimal(14,2)
AS
BEGIN
    DECLARE @Total decimal(14,2);

    SELECT @Total = CAST(COALESCE(SUM(Quantity * UnitPrice), 0) AS decimal(14,2))
    FROM dbo.SaleDetails
    WHERE SaleId = @SaleId;

    RETURN COALESCE(@Total, 0);
END;
GO

CREATE OR ALTER FUNCTION dbo.fn_CustomerLifetimeValue (@CustomerId int)
RETURNS decimal(14,2)
AS
BEGIN
    DECLARE @Total decimal(14,2);

    SELECT @Total = CAST(COALESCE(SUM(sd.Quantity * sd.UnitPrice), 0) AS decimal(14,2))
    FROM dbo.Sales s
    INNER JOIN dbo.SaleDetails sd ON sd.SaleId = s.SaleId
    WHERE s.CustomerId = @CustomerId
      AND s.Status = 'Completed';

    RETURN COALESCE(@Total, 0);
END;
GO
