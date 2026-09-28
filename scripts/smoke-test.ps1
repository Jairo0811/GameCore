param([string]$ApiUrl = "http://localhost:5152")
$ErrorActionPreference = "Stop"

$health = Invoke-RestMethod -Uri "$ApiUrl/health"
if ($health.status -ne "Healthy") { throw "Health endpoint failed." }

$body = @{ email="admin@gamecore.local"; password="GameCore123!" } | ConvertTo-Json
$session = Invoke-RestMethod -Method Post -Uri "$ApiUrl/api/auth/login" -ContentType "application/json" -Body $body
if (-not $session.token) { throw "Login failed." }

$headers = @{ Authorization="Bearer $($session.token)" }

$dashboard = Invoke-RestMethod -Uri "$ApiUrl/api/dashboard" -Headers $headers
if ($null -eq $dashboard.games) { throw "Dashboard endpoint failed." }

$reports = Invoke-RestMethod -Uri "$ApiUrl/api/reports" -Headers $headers
if ($null -eq $reports.monthlySales) { throw "Reports endpoint failed." }

$null = Invoke-RestMethod -Uri "$ApiUrl/api/games" -Headers $headers
$null = Invoke-RestMethod -Uri "$ApiUrl/api/customers" -Headers $headers
$null = Invoke-RestMethod -Uri "$ApiUrl/api/sales" -Headers $headers
$null = Invoke-RestMethod -Uri "$ApiUrl/api/employees" -Headers $headers
$null = Invoke-RestMethod -Uri "$ApiUrl/api/distributions" -Headers $headers

Write-Host "GameCore smoke test passed." -ForegroundColor Green
Write-Host "Dashboard, Reports, Games, Customers, Sales, Employees and Distributions responded successfully." -ForegroundColor DarkGreen
