<#
.SYNOPSIS
  GameCore v1.0.0 release check: database, backend, frontend, audits, API and smoke test in one command.

.DESCRIPTION
  1. Database   : installs GameCoreDB if missing, otherwise validates it (schema, Unicode, filtered email
                  indexes, roles, advanced SQL). Nothing is deleted unless -ResetDatabase is given.
  2. Backend    : dotnet restore, Release build, tests, NuGet vulnerability audit.
  3. Frontend   : npm ci, production build, tests, npm audit (high).
  4. API        : uses the API at -ApiUrl if it is already running; otherwise starts the Release build
                  for the duration of the check and stops it afterwards.
  5. Smoke test : scripts/smoke-test.ps1 (read-only).
  6. Optional   : -IncludeSqlServerTests runs the SQL Server integration tests (they WRITE demo rows;
                  use only with a disposable GameCoreDB, e.g. together with -ResetDatabase).

.EXAMPLE
  .\scripts\release-check.ps1
  .\scripts\release-check.ps1 -SqlExpress
  .\scripts\release-check.ps1 -UpgradeDatabase                         # GameCoreDB from before v1.0.0, data kept
  .\scripts\release-check.ps1 -ResetDatabase -IncludeSqlServerTests     # full check on a fresh database (asks first)
  .\scripts\release-check.ps1 -Server localhost,1433 -SqlUser sa -SqlPassword 'GameCore_Local_2026!'   # docker SQL Server
#>
param(
    [string]$ApiUrl = "http://localhost:5152",
    [string]$Server = "localhost",
    [switch]$SqlExpress,
    [string]$SqlUser,
    [string]$SqlPassword,
    [switch]$UpgradeDatabase,
    [switch]$ResetDatabase,
    [switch]$Force,
    [switch]$SkipDatabase,
    [switch]$IncludeSqlServerTests
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$ApiUrl = $ApiUrl.TrimEnd('/')
if ($SqlExpress) { $Server = ".\SQLEXPRESS" }

$connectionString = if ($SqlUser) {
    "Server=$Server;Database=GameCoreDB;User Id=$SqlUser;Password=$SqlPassword;TrustServerCertificate=True"
} else {
    "Server=$Server;Database=GameCoreDB;Trusted_Connection=True;TrustServerCertificate=True"
}

function Step([string]$message) {
    Write-Host ""
    Write-Host "==> $message" -ForegroundColor Cyan
}

function Exec([string]$What, [scriptblock]$Command) {
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$What failed (exit code $LASTEXITCODE)." }
}

function Test-ApiHealthy {
    try { return (Invoke-RestMethod -Uri "$ApiUrl/health" -TimeoutSec 5).status -eq "Healthy" }
    catch { return $false }
}

$apiProcess = $null
$previousConnection = $env:ConnectionStrings__GameCoreDb
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT

Push-Location $root
try {
    foreach ($tool in @("dotnet", "npm") + $(if ($SkipDatabase) { @() } else { @("sqlcmd") })) {
        if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) { throw "'$tool' was not found in PATH." }
    }

    if (-not $SkipDatabase) {
        Step "Database: install or validate GameCoreDB on $Server"
        $dbArgs = @{ Server = $Server }
        if ($SqlUser) { $dbArgs.User = $SqlUser; $dbArgs.Password = $SqlPassword }
        if ($UpgradeDatabase) { $dbArgs.Upgrade = $true }
        if ($ResetDatabase) { $dbArgs.Reset = $true }
        if ($Force) { $dbArgs.Force = $true }
        & (Join-Path $PSScriptRoot "setup-db.ps1") @dbArgs
    }

    Step "Backend: restore and Release build"
    Exec "dotnet restore" { dotnet restore src/backend/GameCore.sln }
    Exec "dotnet build" { dotnet build src/backend/GameCore.sln -c Release --no-restore }

    Step "Backend: tests"
    Exec "dotnet test" { dotnet test src/backend/GameCore.sln -c Release --no-build }

    Step "Backend: NuGet vulnerability audit"
    $nugetOutput = dotnet list src/backend/GameCore.sln package --vulnerable --include-transitive 2>&1
    $nugetOutput | Write-Host
    if ($LASTEXITCODE -ne 0) { throw "NuGet vulnerability check failed." }
    if (($nugetOutput -join [Environment]::NewLine) -match "has the following vulnerable packages") {
        throw "Vulnerable NuGet packages were detected."
    }

    Push-Location src/frontend
    try {
        Step "Frontend: npm ci"
        Exec "npm ci" { npm ci }
        Step "Frontend: production build"
        Exec "npm run build" { npm run build }
        Step "Frontend: tests"
        Exec "npm test" { npm test }
        Step "Frontend: npm audit (high)"
        Exec "npm audit" { npm audit --audit-level=high }
    }
    finally {
        Pop-Location
    }

    Step "API at $ApiUrl"
    if (Test-ApiHealthy) {
        Write-Host "Using the API that is already running."
    }
    else {
        $apiDir = Join-Path $root "src/backend/GameCore.WebAPI/bin/Release/net10.0"
        Write-Host "Starting the Release build from $apiDir ..."
        $env:ConnectionStrings__GameCoreDb = $connectionString
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        $apiProcess = Start-Process -FilePath "dotnet" -ArgumentList @("GameCore.WebAPI.dll", "--urls", $ApiUrl) `
            -WorkingDirectory $apiDir -PassThru -NoNewWindow
        $deadline = (Get-Date).AddSeconds(60)
        while (-not (Test-ApiHealthy)) {
            if ($apiProcess.HasExited) { throw "The API process exited with code $($apiProcess.ExitCode)." }
            if ((Get-Date) -gt $deadline) { throw "The API did not report Healthy within 60 seconds (check SQL Server and the connection string)." }
            Start-Sleep -Seconds 2
        }
    }

    Step "Smoke test (login, roles, dashboard, reports and every module)"
    & (Join-Path $PSScriptRoot "smoke-test.ps1") -ApiUrl $ApiUrl

    if ($IncludeSqlServerTests) {
        Step "SQL Server integration tests (write demo rows)"
        $env:GAMECORE_TEST_SQLSERVER = $connectionString
        try {
            Exec "SQL Server integration tests" { dotnet test src/backend/GameCore.sln -c Release --no-build --filter "FullyQualifiedName~GameCore.Tests.SqlServer" }
        }
        finally {
            Remove-Item Env:\GAMECORE_TEST_SQLSERVER -ErrorAction SilentlyContinue
        }
    }

    Write-Host ""
    Write-Host "GAMECORE v1.0.0 RELEASE CHECK PASSED" -ForegroundColor Green
}
finally {
    if ($apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
        Write-Host "Stopped the API started by this script." -ForegroundColor DarkGray
    }
    $env:ConnectionStrings__GameCoreDb = $previousConnection
    $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
    Pop-Location
}
