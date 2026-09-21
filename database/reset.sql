/*
 Development-only reset script.
 This irreversibly deletes GameCoreDB and all of its data.
*/

USE master;
GO

IF DB_ID(N'GameCoreDB') IS NOT NULL
BEGIN
    ALTER DATABASE GameCoreDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE GameCoreDB;
    PRINT 'GameCoreDB dropped.';
END
ELSE
BEGIN
    PRINT 'GameCoreDB does not exist. Nothing to drop.';
END;
GO
