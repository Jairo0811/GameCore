/*
 Non-destructive, idempotent upgrade of an existing GameCoreDB (created before v1.0.0) to the v1.0.0 rules.
 It does NOT drop the database or delete data. Safe to run more than once.

   .\scripts\setup-db.ps1 -Upgrade
   or: sqlcmd -S <server> -E -C -I -f 65001 -i database/upgrade-v1.sql

 Changes:
   1. Customers/Employees.Email: UNIQUE constraint -> filtered unique index (several NULL emails allowed).
   2. Adds the Manager and Viewer demo accounts if they are missing.
   3. Replaces the four demo employee names carried over from the academic project with fictional ones
      (only the rows identified by their original demo emails).
*/

:on error exit

IF DB_ID(N'GameCoreDB') IS NULL
    THROW 54000, 'GameCoreDB does not exist. Run database/setup.sql instead.', 1;
GO

USE GameCoreDB;
GO

-- Required for filtered indexes (and for writing to tables that have them).
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

-- 1. Optional emails ------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_Customers_Email' AND parent_object_id = OBJECT_ID(N'dbo.Customers'))
    ALTER TABLE dbo.Customers DROP CONSTRAINT UQ_Customers_Email;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Customers_Email' AND object_id = OBJECT_ID(N'dbo.Customers'))
    CREATE UNIQUE INDEX UX_Customers_Email ON dbo.Customers(Email) WHERE Email IS NOT NULL;

IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_Employees_Email' AND parent_object_id = OBJECT_ID(N'dbo.Employees'))
    ALTER TABLE dbo.Employees DROP CONSTRAINT UQ_Employees_Email;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Employees_Email' AND object_id = OBJECT_ID(N'dbo.Employees'))
    CREATE UNIQUE INDEX UX_Employees_Email ON dbo.Employees(Email) WHERE Email IS NOT NULL;

-- 2. Demo accounts (password GameCore123!) ----------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.AppUsers WHERE Email = 'manager@gamecore.local')
    INSERT INTO dbo.AppUsers (Email, PasswordHash, Role)
    VALUES ('manager@gamecore.local', '100000.xlGoRvtaISeq/Od4WeU40A==.R6DUej70fwHDjkG9fOb7nn6XLxv4UFEnygJk2ZG9SfA=', 'Manager');

IF NOT EXISTS (SELECT 1 FROM dbo.AppUsers WHERE Email = 'viewer@gamecore.local')
    INSERT INTO dbo.AppUsers (Email, PasswordHash, Role)
    VALUES ('viewer@gamecore.local', '100000.3nkbRVAAxblCHYXxh31EGw==.7+PoKwwEoNEeCpdcpRs8HrmHPnNEXLoGHEVDse3KOzc=', 'Viewer');

-- 3. Fictional demo employees (NCHAR keeps the accents independent of file encoding) --
UPDATE dbo.Employees SET FirstName = N'Valeria', LastName = N'Pe' + NCHAR(241) + N'a', Email = 'valeria.demo@gamecore.local'
WHERE Email = 'jairo.demo@gamecore.local';
UPDATE dbo.Employees SET FirstName = N'Andr' + NCHAR(233) + N's', LastName = N'Ib' + NCHAR(225) + NCHAR(241) + N'ez', Email = 'andres.demo@gamecore.local'
WHERE Email = 'ismael.demo@gamecore.local';
UPDATE dbo.Employees SET FirstName = N'Sof' + NCHAR(237) + N'a', LastName = N'Ram' + NCHAR(237) + N'rez', Email = 'sofia.demo@gamecore.local'
WHERE Email = 'luis.demo@gamecore.local';
UPDATE dbo.Employees SET FirstName = N'Tom' + NCHAR(225) + N's', LastName = N'Herrera', Email = 'tomas.demo@gamecore.local'
WHERE Email = 'francis.demo@gamecore.local';

COMMIT TRANSACTION;
GO

PRINT 'GameCoreDB upgraded to v1.0.0 rules.';
GO
