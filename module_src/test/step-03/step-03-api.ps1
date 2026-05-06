param(
    [string]$BaseUrl = "http://localhost:5093"
)

$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition,[string]$Message)
    if (-not $Condition) { throw "Assertion failed: $Message" }
}

try {
    $confirmBody = @{
        buyerId = 3
        lines = @(@{ productId = 1; qty = 1; unitPriceVnd = 45000 })
        voucherType = "none"
        voucherValue = 0
        vipPointsUsed = 0
    } | ConvertTo-Json -Depth 6

    $confirm = Invoke-RestMethod -Uri "$BaseUrl/api/bills/confirm" -Method POST -ContentType 'application/json' -Body $confirmBody
    Assert-True ($confirm.status -eq "Pending") "Confirm must return Pending"

    $billId = $confirm.billId
    $detail = Invoke-RestMethod -Uri "$BaseUrl/api/bills/$billId" -Method GET
    Assert-True ($detail.billId -eq $billId) "GET bill must return same id"

    $updateBody = @{
        buyerId = 3
        lines = @(@{ productId = 1; qty = 2; unitPriceVnd = 45000 })
        voucherType = "percent"
        voucherValue = 5
        vipPointsUsed = 5
    } | ConvertTo-Json -Depth 6
    $updated = Invoke-RestMethod -Uri "$BaseUrl/api/bills/$billId/pending" -Method PUT -ContentType 'application/json' -Body $updateBody
    Assert-True ($updated.status -eq "Pending") "Pending update must keep status Pending"

    $complete = Invoke-RestMethod -Uri "$BaseUrl/api/bills/$billId/complete" -Method POST -ContentType 'application/json' -Body '{}'
    Assert-True ($complete.status -eq "Completed") "Complete must return Completed"

    $code = 0
    try {
        Invoke-WebRequest -Uri "$BaseUrl/api/bills/$billId/complete" -Method POST -ContentType 'application/json' -Body '{}' -ErrorAction Stop | Out-Null
    }
    catch {
        $code = $_.Exception.Response.StatusCode.value__
    }
    Assert-True ($code -eq 409) "Repeat complete must return 409"

    Write-Host "[STEP-03] PASS" -ForegroundColor Green
    exit 0
}
catch {
    Write-Host "[STEP-03] FAIL: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
