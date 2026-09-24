param(
    [string]$Server = "localhost",
    [switch]$SqlExpress
)

$ErrorActionPreference = "Stop"

if ($SqlExpress) {
    $Server = ".\SQLEXPRESS"
}

Write-Host "Rebuilding GameCoreDB on $Server using UTF-8..." -ForegroundColor Cyan

sqlcmd -S $Server -E -f 65001 -i database\reset.sql
if ($LASTEXITCODE -ne 0) { throw "GameCoreDB reset failed." }

sqlcmd -S $Server -E -f 65001 -i database\setup.sql
if ($LASTEXITCODE -ne 0) { throw "GameCoreDB setup failed." }

Write-Host "GameCoreDB is ready." -ForegroundColor Green
