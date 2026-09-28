param(
    [string]$ApiUrl = "http://localhost:5152"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

function Step([string]$message) {
    Write-Host ""
    Write-Host "==> $message" -ForegroundColor Cyan
}

Push-Location $root
try {
    Step "Building .NET solution in Release"
    dotnet restore src\backend\GameCore.sln
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }

    dotnet build src\backend\GameCore.sln -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }

    Step "Checking NuGet vulnerabilities"
    $nugetOutput = dotnet list src\backend\GameCore.sln package --vulnerable --include-transitive 2>&1
    $nugetOutput | Write-Host
    if ($LASTEXITCODE -ne 0) { throw "NuGet vulnerability check failed." }
    if (($nugetOutput -join [Environment]::NewLine) -match "has the following vulnerable packages") {
        throw "Vulnerable NuGet packages were detected."
    }

    Step "Installing frontend dependencies"
    Push-Location src\frontend
    try {
        npm ci
        if ($LASTEXITCODE -ne 0) { throw "npm ci failed." }

        Step "Building frontend for production"
        npm run build
        if ($LASTEXITCODE -ne 0) { throw "Frontend build failed." }

        Step "Checking npm vulnerabilities"
        npm audit --audit-level=high
        if ($LASTEXITCODE -ne 0) { throw "npm audit found high-severity vulnerabilities." }
    }
    finally {
        Pop-Location
    }

    Step "Running API smoke test"
    & "$PSScriptRoot\smoke-test.ps1" -ApiUrl $ApiUrl

    Write-Host ""
    Write-Host "GAMECORE v1.0.0 RELEASE CHECK PASSED" -ForegroundColor Green
}
finally {
    Pop-Location
}
