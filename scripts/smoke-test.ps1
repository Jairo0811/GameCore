<#
.SYNOPSIS
  Read-only smoke test of a running GameCore API (it does not create or modify data).

.DESCRIPTION
  Verifies: health (including GameCoreDB), login for Admin/Manager/Viewer, invalid login,
  anonymous access, every main read endpoint (dashboard, reports, games, customers, sales,
  employees, distributions, catalogs), Viewer write protection and pre-database validation.
  Works with Windows PowerShell 5.1 and PowerShell 7+.
#>
param(
    [string]$ApiUrl = "http://localhost:5152",
    [string]$Password = "GameCore123!"
)
$ErrorActionPreference = "Stop"
$ApiUrl = $ApiUrl.TrimEnd('/')

function Step([string]$message) { Write-Host "  - $message" -ForegroundColor DarkCyan }

function Get-StatusCode([scriptblock]$Request) {
    try {
        $null = & $Request
        return 200
    }
    catch {
        if ($_.Exception.Response) { return [int]$_.Exception.Response.StatusCode }
        throw
    }
}

function Assert-Status([int]$Expected, [int]$Actual, [string]$What) {
    if ($Actual -ne $Expected) { throw "$What returned HTTP $Actual (expected $Expected)." }
}

function Login([string]$Email) {
    $body = @{ email = $Email; password = $Password } | ConvertTo-Json
    $session = Invoke-RestMethod -Method Post -Uri "$ApiUrl/api/auth/login" -ContentType "application/json" -Body $body
    if (-not $session.token) { throw "Login failed for $Email." }
    if (-not $session.role) { throw "Login response for $Email has no role." }
    return @{ Authorization = "Bearer $($session.token)" }
}

Step "Health (API + GameCoreDB)"
try { $health = Invoke-RestMethod -Uri "$ApiUrl/health" }
catch { throw "Health endpoint failed (is SQL Server/GameCoreDB reachable?): $($_.Exception.Message)" }
if ($health.status -ne "Healthy") { throw "Health endpoint reported '$($health.status)'." }
$db = @($health.checks) | Where-Object { $_.name -eq "database" }
if (-not $db -or $db.status -ne "Healthy") { throw "Health endpoint does not report a healthy database check." }

Step "Authentication"
$admin = Login "admin@gamecore.local"
$manager = Login "manager@gamecore.local"
$viewer = Login "viewer@gamecore.local"
$badBody = @{ email = "admin@gamecore.local"; password = "not-the-password" } | ConvertTo-Json
Assert-Status 401 (Get-StatusCode { Invoke-RestMethod -Method Post -Uri "$ApiUrl/api/auth/login" -ContentType "application/json" -Body $badBody }) "Invalid login"
Assert-Status 401 (Get-StatusCode { Invoke-RestMethod -Uri "$ApiUrl/api/games" }) "Anonymous GET /api/games"

Step "Dashboard and reports"
$dashboard = Invoke-RestMethod -Uri "$ApiUrl/api/dashboard" -Headers $admin
if ($null -eq $dashboard.games -or $null -eq $dashboard.revenue) { throw "Dashboard endpoint failed." }
$reports = Invoke-RestMethod -Uri "$ApiUrl/api/reports" -Headers $admin
if ($null -eq $reports.monthlySales -or $null -eq $reports.topGames) { throw "Reports endpoint failed." }

Step "Modules (games, customers, sales, employees, distributions, catalogs)"
$games = @(Invoke-RestMethod -Uri "$ApiUrl/api/games" -Headers $admin)
$customers = @(Invoke-RestMethod -Uri "$ApiUrl/api/customers" -Headers $admin)
$null = Invoke-RestMethod -Uri "$ApiUrl/api/sales" -Headers $admin
$null = Invoke-RestMethod -Uri "$ApiUrl/api/employees" -Headers $admin
$null = Invoke-RestMethod -Uri "$ApiUrl/api/distributions" -Headers $admin
$catalogs = Invoke-RestMethod -Uri "$ApiUrl/api/catalogs" -Headers $admin
if ($games.Count -eq 0 -or $customers.Count -eq 0) { throw "Seed data (games/customers) is missing." }
if (@($catalogs.countries).Count -eq 0) { throw "Catalogs endpoint returned no countries." }

Step "Roles (Viewer read-only, Manager can read)"
Assert-Status 200 (Get-StatusCode { Invoke-RestMethod -Uri "$ApiUrl/api/games" -Headers $viewer }) "Viewer GET /api/games"
Assert-Status 200 (Get-StatusCode { Invoke-RestMethod -Uri "$ApiUrl/api/games" -Headers $manager }) "Manager GET /api/games"
$customerBody = @{ firstName = "Smoke"; lastName = "Viewer" } | ConvertTo-Json
Assert-Status 403 (Get-StatusCode { Invoke-RestMethod -Method Post -Uri "$ApiUrl/api/customers" -Headers $viewer -ContentType "application/json" -Body $customerBody }) "Viewer POST /api/customers"
Assert-Status 403 (Get-StatusCode { Invoke-RestMethod -Method Delete -Uri "$ApiUrl/api/games/$($games[0].gameId)" -Headers $viewer }) "Viewer DELETE /api/games/{id}"

Step "Validation before the database (duplicated GameId in a sale -> 400)"
$gameId = $games[0].gameId
$saleBody = @{ customerId = $customers[0].customerId; employeeId = $null; items = @(@{ gameId = $gameId; quantity = 1 }, @{ gameId = $gameId; quantity = 1 }) } | ConvertTo-Json -Depth 4
Assert-Status 400 (Get-StatusCode { Invoke-RestMethod -Method Post -Uri "$ApiUrl/api/sales" -Headers $manager -ContentType "application/json" -Body $saleBody }) "Sale with duplicated GameId"

Write-Host "GameCore smoke test passed." -ForegroundColor Green
Write-Host "Health, login, roles, Dashboard, Reports, Games, Customers, Sales, Employees, Distributions and Catalogs responded as expected." -ForegroundColor DarkGreen
