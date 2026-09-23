param([string]$ApiUrl = "https://localhost:5001")
$ErrorActionPreference = "Stop"
$health = Invoke-RestMethod -Uri "$ApiUrl/health" -SkipCertificateCheck
if ($health.status -ne "Healthy") { throw "Health endpoint failed." }

$body = @{ email="admin@gamecore.local"; password="GameCore123!" } | ConvertTo-Json
$session = Invoke-RestMethod -Method Post -Uri "$ApiUrl/api/auth/login" -ContentType "application/json" -Body $body -SkipCertificateCheck
if (-not $session.token) { throw "Login failed." }

$headers = @{ Authorization="Bearer $($session.token)" }
$dashboard = Invoke-RestMethod -Uri "$ApiUrl/api/dashboard" -Headers $headers -SkipCertificateCheck
if ($null -eq $dashboard.games) { throw "Dashboard endpoint failed." }
Write-Host "GameCore smoke test passed." -ForegroundColor Green
