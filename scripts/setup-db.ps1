<#
.SYNOPSIS
  Installs or validates GameCoreDB using the official UTF-8 bootstrap (database/setup.sql).

.DESCRIPTION
  - GameCoreDB missing  -> runs database/setup.sql (schema, seed, AppUsers, advanced SQL, validations).
  - GameCoreDB present  -> does NOT modify it; runs database/validate.sql and database/advanced/validate.sql.
  - -Reset              -> DESTRUCTIVE: drops GameCoreDB (database/reset.sql) and rebuilds it.
                           Asks for confirmation unless -Force is also given.

.EXAMPLE
  .\scripts\setup-db.ps1
  .\scripts\setup-db.ps1 -SqlExpress
  .\scripts\setup-db.ps1 -Server localhost,1433 -User sa -Password 'GameCore_Local_2026!'
  .\scripts\setup-db.ps1 -Reset            # drops and recreates GameCoreDB (asks first)
#>
param(
    [string]$Server = "localhost",
    [switch]$SqlExpress,
    [string]$User,
    [string]$Password,
    [switch]$Reset,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
if ($SqlExpress) { $Server = ".\SQLEXPRESS" }
$root = Split-Path -Parent $PSScriptRoot

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw "sqlcmd was not found. Install the SQL Server command-line tools (sqlcmd) and try again."
}

# -C trusts the local self-signed certificate (required by ODBC Driver 18 / go-sqlcmd defaults).
$auth = if ($User) { @("-U", $User, "-P", $Password) } else { @("-E") }

function Invoke-SqlFile([string]$File) {
    & sqlcmd -S $Server @auth -C -b -f 65001 -i $File
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed while running $File." }
}

Push-Location $root
try {
    $result = & sqlcmd -S $Server @auth -C -b -h -1 -W -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID(N'GameCoreDB') IS NULL THEN 0 ELSE 1 END"
    if ($LASTEXITCODE -ne 0) { throw "Could not connect to SQL Server '$Server'." }
    $exists = (($result | Out-String).Trim() -eq "1")

    if ($Reset) {
        if ($exists) {
            Write-Host ""
            Write-Host "WARNING: -Reset permanently DELETES GameCoreDB on '$Server' and all of its data." -ForegroundColor Yellow
            if (-not $Force) {
                $answer = Read-Host "Type GameCoreDB to confirm"
                if ($answer -cne "GameCoreDB") { throw "Reset cancelled. GameCoreDB was not modified." }
            }
            Invoke-SqlFile "database\reset.sql"
        }
        $exists = $false
    }

    if ($exists) {
        Write-Host "GameCoreDB already exists on $Server. Validating without changes..." -ForegroundColor Cyan
        Invoke-SqlFile "database\validate.sql"
        Invoke-SqlFile "database\advanced\validate.sql"
        Write-Host "GameCoreDB is valid." -ForegroundColor Green
        Write-Host "(To rebuild it from scratch, which deletes its data, run: .\scripts\setup-db.ps1 -Reset)" -ForegroundColor DarkGray
    }
    else {
        Write-Host "Creating GameCoreDB on $Server using UTF-8..." -ForegroundColor Cyan
        Invoke-SqlFile "database\setup.sql"
        Write-Host "GameCoreDB is ready." -ForegroundColor Green
    }
}
finally {
    Pop-Location
}
