param([string]$BaseUrl = "http://localhost:5093")
$ErrorActionPreference = "Stop"

function Assert-True { param([bool]$Condition,[string]$Message) if(-not $Condition){ throw "Assertion failed: $Message" } }

try {
  $all = Invoke-RestMethod -Uri "$BaseUrl/api/bills" -Method GET
  Assert-True ($all.items.Count -ge 1) "Expected at least one bill"
  Assert-True ($all.pageSize -eq 5) "Default pageSize must be 5"

  $filtered = Invoke-RestMethod -Uri "$BaseUrl/api/bills?status=completed&payableMaxVnd=100000000&productIds=1" -Method GET
  Assert-True ($filtered.items.Count -ge 1) "Expected filtered bills >= 1"
  Assert-True (($filtered.items | Where-Object { $_.status -ne "Completed" }).Count -eq 0) "Filtered result must be only Completed"

  $csv = Invoke-WebRequest -Uri "$BaseUrl/api/bills/export.csv" -UseBasicParsing
  Assert-True ($csv.Content.Contains("BillId,BuyerId,Status")) "CSV header missing"

  $rows = $csv.Content | ConvertFrom-Csv
  Assert-True ($rows.Count -ge 1) "CSV must contain at least one data row"
  $requiredColumns = @("BillId","BuyerId","Status","PendingAtUtc","Subtotal","Payable")
  foreach ($col in $requiredColumns) {
    Assert-True ($rows[0].PSObject.Properties.Name -contains $col) "CSV missing column: $col"
  }

  Write-Host "[STEP-04] PASS" -ForegroundColor Green
  exit 0
}
catch {
  Write-Host "[STEP-04] FAIL: $($_.Exception.Message)" -ForegroundColor Red
  exit 1
}
