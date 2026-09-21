/*
 GameCoreDB bootstrap
 Run this file from SQLCMD mode in SQL Server Management Studio
 or with sqlcmd from the repository root:

 sqlcmd -S <server> -E -i database/setup.sql

 For SQL authentication:
 sqlcmd -S <server> -U <user> -P <password> -i database/setup.sql
*/

:on error exit

PRINT '=== GameCoreDB setup started ===';
GO

:r .\database\schema.sql
:r .\database\seed.sql
:r .\database\advanced\views.sql
:r .\database\advanced\functions.sql
:r .\database\advanced\procedures.sql
:r .\database\validate.sql
:r .\database\advanced\validate.sql

PRINT '=== GameCoreDB setup completed successfully ===';
GO
