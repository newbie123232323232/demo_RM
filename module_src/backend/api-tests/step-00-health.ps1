# Step 0 — smoke API (mục đích: xác nhận server sống sau khi dotnet run)
# Chạy: .\step-00-health.ps1
# Hoặc: $env:BASE_URL='http://localhost:5093'; .\step-00-health.ps1

$ErrorActionPreference = 'Stop'
$base = if ($env:BASE_URL) { $env:BASE_URL.TrimEnd('/') } else { 'http://localhost:5093' }

Write-Host "GET $base/health"
$r = Invoke-RestMethod -Uri "$base/health" -Method Get
if ($r.status -ne 'ok') { throw "Expected status=ok, got: $($r | ConvertTo-Json)" }
Write-Host "OK:" ($r | ConvertTo-Json -Compress)

Write-Host "GET $base/ (first 80 chars)"
$t = Invoke-WebRequest -Uri "$base/" -Method Get -UseBasicParsing
if ($t.StatusCode -ne 200) { throw "Expected 200, got $($t.StatusCode)" }
$preview = $t.Content.Substring(0, [Math]::Min(80, $t.Content.Length))
Write-Host "OK: $preview..."

Write-Host "All Step 0 checks passed."
