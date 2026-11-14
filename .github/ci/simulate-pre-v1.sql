-- CI only: turns a v1.0.0 GameCoreDB back into the pre-v1.0.0 shape to exercise database/upgrade-v1.sql.
:on error exit
USE GameCoreDB;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO
BEGIN TRANSACTION;
DROP INDEX UX_Customers_Email ON dbo.Customers;
ALTER TABLE dbo.Customers ADD CONSTRAINT UQ_Customers_Email UNIQUE (Email);
DROP INDEX UX_Employees_Email ON dbo.Employees;
ALTER TABLE dbo.Employees ADD CONSTRAINT UQ_Employees_Email UNIQUE (Email);
DELETE FROM dbo.AppUsers WHERE Role IN ('Manager', 'Viewer');
UPDATE dbo.Employees SET FirstName = N'Demo', LastName = N'Legacy', Email = 'jairo.demo@gamecore.local'
WHERE Email = 'valeria.demo@gamecore.local';
COMMIT TRANSACTION;
GO
PRINT 'Simulated pre-v1.0.0 GameCoreDB.';
GO
