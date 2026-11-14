/*
 Development-only reset script.
 This irreversibly deletes GameCoreDB and all of its data.

 Other sessions (for example a running API with pooled connections) can reconnect between
 SINGLE_USER and DROP, so the drop kills remaining sessions and retries a few times. If it still
 cannot drop the database, it restores MULTI_USER and fails instead of leaving it locked.
*/

USE master;
GO

SET NOCOUNT ON;

IF DB_ID(N'GameCoreDB') IS NULL
BEGIN
    PRINT 'GameCoreDB does not exist. Nothing to drop.';
    RETURN;
END;

DECLARE @Attempt int = 0;
DECLARE @Session int;
DECLARE @Kill nvarchar(30);
DECLARE @LastError nvarchar(2048) = NULL;

WHILE DB_ID(N'GameCoreDB') IS NOT NULL AND @Attempt < 10
BEGIN
    SET @Attempt += 1;
    BEGIN TRY
        ALTER DATABASE GameCoreDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

        -- Whoever grabbed the single allowed connection in the meantime is disconnected.
        SET @Session = NULL;
        SELECT TOP (1) @Session = session_id
        FROM sys.dm_exec_sessions
        WHERE database_id = DB_ID(N'GameCoreDB') AND session_id <> @@SPID;
        WHILE @Session IS NOT NULL
        BEGIN
            SET @Kill = N'KILL ' + CAST(@Session AS nvarchar(10));
            BEGIN TRY EXEC (@Kill); END TRY BEGIN CATCH SET @LastError = ERROR_MESSAGE(); END CATCH;
            SET @Session = NULL;
            SELECT TOP (1) @Session = session_id
            FROM sys.dm_exec_sessions
            WHERE database_id = DB_ID(N'GameCoreDB') AND session_id <> @@SPID;
        END;

        DROP DATABASE GameCoreDB;
    END TRY
    BEGIN CATCH
        SET @LastError = ERROR_MESSAGE();
        IF DB_ID(N'GameCoreDB') IS NOT NULL WAITFOR DELAY '00:00:01';
    END CATCH;
END;

IF DB_ID(N'GameCoreDB') IS NOT NULL
BEGIN
    BEGIN TRY ALTER DATABASE GameCoreDB SET MULTI_USER; END TRY BEGIN CATCH SET @LastError = COALESCE(@LastError, ERROR_MESSAGE()); END CATCH;
    DECLARE @Message nvarchar(2048) = N'GameCoreDB could not be dropped after 10 attempts: ' + COALESCE(@LastError, N'unknown error');
    THROW 50100, @Message, 1;
END;

PRINT 'GameCoreDB dropped.';
GO
