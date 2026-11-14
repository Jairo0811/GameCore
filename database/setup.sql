/*
 GameCoreDB bootstrap (creates the database; it fails if GameCoreDB already exists).

 Recommended (Windows, from the repository root):
   .\scripts\setup-db.ps1                 creates GameCoreDB if missing, otherwise only validates it
   .\scripts\setup-db.ps1 -Reset          drops and recreates it (asks for confirmation)

 Raw sqlcmd, from the repository root (-f 65001 = UTF-8, -C = trust the local certificate, -I = QUOTED_IDENTIFIER ON):
   sqlcmd -S <server> -E -C -I -f 65001 -i database/setup.sql
   sqlcmd -S <server> -U <user> -P <password> -C -I -f 65001 -i database/setup.sql

 SSMS: enable Query > SQLCMD Mode and open the file from the repository root.
 Linux/Docker: the ':r' includes below use Windows paths; scripts/docker/init-db.sh normalises them.
*/

:on error exit

PRINT '=== GameCoreDB setup started ===';
GO

:r .\database\schema.sql
:r .\database\seed.sql
:r .\database\security.sql
:r .\database\advanced\views.sql
:r .\database\advanced\functions.sql
:r .\database\advanced\procedures.sql
:r .\database\validate.sql
:r .\database\advanced\validate.sql

PRINT '=== GameCoreDB setup completed successfully ===';
GO
