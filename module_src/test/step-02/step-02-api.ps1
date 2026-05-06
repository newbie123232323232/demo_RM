param(
    [string]$BaseUrl = "http://localhost:5093"
)

$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition,[string]$Message)
    if (-not $Condition) { throw "Assertion failed: $Message" }
}

try {
    $okBody = @{
        buyerId = 2
        lines = @(
            @{ productId = 1; qty = 2; unitPriceVnd = 45000 },
            @{ productId = 2; qty = 1; unitPriceVnd = 320000 }
        )
        voucherType = "percent"
        voucherValue = 5
        vipPointsUsed = 7
    } | ConvertTo-Json -Depth 6

    $ok = Invoke-RestMethod -Uri "$BaseUrl/api/bills/preview" -Method POST -ContentType 'application/json' -Body $okBody
    Assert-True ($ok.payable -ge 0) "payable must be >= 0"
    Assert-True ($ok.baseBeforeVip -ge $ok.payable) "baseBeforeVip must be >= payable"

    $badVipBody = @{
        buyerId = 2
        lines = @(@{ productId = 1; qty = 1; unitPriceVnd = 45000 })
        voucherType = "none"
        voucherValue = 0
        vipPointsUsed = 3
    } | ConvertTo-Json -Depth 6

    $badVipCode = 0
    try {
        Invoke-WebRequest -Uri "$BaseUrl/api/bills/preview" -Method POST -ContentType 'application/json' -Body $badVipBody -ErrorAction Stop | Out-Null
    } catch {
        $badVipCode = $_.Exception.Response.StatusCode.value__
    }
    Assert-True ($badVipCode -eq 400) "vipPointsUsed=3 must return HTTP 400"

    Write-Host "[STEP-02] PASS" -ForegroundColor Green
    exit 0
}
catch {
    Write-Host "[STEP-02] FAIL: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
