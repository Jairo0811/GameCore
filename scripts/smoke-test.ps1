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
Write-Host "GameCore smoke test passed." -ForegroundColor Green
