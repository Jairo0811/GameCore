<#
.SYNOPSIS
  Installs or validates GameCoreDB using the official UTF-8 bootstrap (database/setup.sql).

.DESCRIPTION
  - GameCoreDB missing  -> runs database/setup.sql (schema, seed, AppUsers, advanced SQL, validations).
  - GameCoreDB present  -> does NOT modify it; runs database/validate.sql and database/advanced/validate.sql.
  - -Upgrade            -> non-destructive: applies database/upgrade-v1.sql to a GameCoreDB created
                           before v1.0.0 (filtered email indexes, Manager/Viewer users), then validates.
  - -Reset              -> DESTRUCTIVE: drops GameCoreDB (database/reset.sql) and rebuilds it.
                           Asks for confirmation unless -Force is also given.

.EXAMPLE
  .\scripts\setup-db.ps1
  .\scripts\setup-db.ps1 -SqlExpress
  .\scripts\setup-db.ps1 -Server localhost,1433 -User sa -Password 'GameCore_Local_2026!'
  .\scripts\setup-db.ps1 -Upgrade          # upgrades an older GameCoreDB in place, keeping its data
  .\scripts\setup-db.ps1 -Reset            # drops and recreates GameCoreDB (asks first)
#>
param(
    [string]$Server = "localhost",
    [switch]$SqlExpress,
    [string]$User,
    [string]$Password,
    [switch]$Upgrade,
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
# -I enables QUOTED_IDENTIFIER, as SSMS and SqlClient do (needed for tables with filtered indexes).
$auth = if ($User) { @("-U", $User, "-P", $Password) } else { @("-E") }

function Invoke-SqlFile([string]$File) {
    & sqlcmd -S $Server @auth -C -I -b -f 65001 -i $File
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
            Invoke-SqlFile "database/reset.sql"
        }
        $exists = $false
    }

    if ($exists) {
        if ($Upgrade) {
            Write-Host "Upgrading the existing GameCoreDB on $Server (no data is deleted)..." -ForegroundColor Cyan
            Invoke-SqlFile "database/upgrade-v1.sql"
        }
        Write-Host "GameCoreDB already exists on $Server. Validating..." -ForegroundColor Cyan
        try {
            Invoke-SqlFile "database/validate.sql"
            Invoke-SqlFile "database/advanced/validate.sql"
        }
        catch {
            Write-Host "Validation failed. If this GameCoreDB was created before v1.0.0, run: .\scripts\setup-db.ps1 -Upgrade" -ForegroundColor Yellow
            throw
        }
        Write-Host "GameCoreDB is valid." -ForegroundColor Green
        Write-Host "(To rebuild it from scratch, which deletes its data, run: .\scripts\setup-db.ps1 -Reset)" -ForegroundColor DarkGray
    }
    else {
        Write-Host "Creating GameCoreDB on $Server using UTF-8..." -ForegroundColor Cyan
        $setupFile = "database/setup.sql"
        if ($PSVersionTable.PSEdition -eq "Core" -and -not $IsWindows) {
            # setup.sql uses Windows-style ':r .\database\...' includes; normalise them for Linux/macOS sqlcmd.
            $setupFile = Join-Path ([System.IO.Path]::GetTempPath()) "gamecore-setup.sql"
            (Get-Content "database/setup.sql" -Raw -Encoding utf8) -replace '\\', '/' | Set-Content $setupFile -Encoding utf8
        }
        Invoke-SqlFile $setupFile
        Write-Host "GameCoreDB is ready." -ForegroundColor Green
    }
}
finally {
    Pop-Location
}
