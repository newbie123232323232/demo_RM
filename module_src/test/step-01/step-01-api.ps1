param(
    [string]$BaseUrl = "http://localhost:5093"
)

$ErrorActionPreference = "Stop"

Write-Host "[STEP-01] Testing API at $BaseUrl" -ForegroundColor Cyan

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw "Assertion failed: $Message"
    }
}

try {
    $health = Invoke-RestMethod -Uri "$BaseUrl/health" -Method GET
    Assert-True ($health.status -eq "ok") "Health status must be ok"
    Assert-True ($health.database -eq "connected") "Database must be connected"

    $buyers = Invoke-RestMethod -Uri "$BaseUrl/api/buyers" -Method GET
    Assert-True ($buyers.Count -ge 3) "Buyers count must be >= 3"
    Assert-True (($buyers | Where-Object { $_.vipPoint -ge 5 }).Count -ge 1) "At least one buyer must have vipPoint >= 5"

    $products = Invoke-RestMethod -Uri "$BaseUrl/api/products" -Method GET
    Assert-True ($products.Count -ge 5) "Products count must be >= 5"
    Assert-True (($products | Where-Object { $_.unitPriceVnd -lt 0 }).Count -eq 0) "No product price can be negative"

    $buyer2 = Invoke-RestMethod -Uri "$BaseUrl/api/buyers/2" -Method GET
    Assert-True ($buyer2.id -eq 2) "GET /api/buyers/2 must return id=2"

    $notFoundOk = $false
    try {
        Invoke-RestMethod -Uri "$BaseUrl/api/buyers/9999" -Method GET | Out-Null
    }
    catch {
        if ($_.Exception.Response.StatusCode.value__ -eq 404) {
            $notFoundOk = $true
        }
    }
    Assert-True $notFoundOk "GET /api/buyers/9999 must return 404"

    Write-Host "[STEP-01] PASS" -ForegroundColor Green
    exit 0
}
catch {
    Write-Host "[STEP-01] FAIL: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
