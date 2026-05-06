param([string]$BaseUrl = "http://localhost:5093")
$ErrorActionPreference = "Stop"

function Assert-True {
  param([bool]$Condition,[string]$Message)
  if(-not $Condition){ throw "Assertion failed: $Message" }
}

function Get-BillsSummary {
  param([string]$Url)
  $page = 1
  $totalPages = 1
  $sum = 0
  $count = 0
  while($page -le $totalPages) {
    $resp = Invoke-RestMethod -Uri "$Url&page=$page&pageSize=20" -Method GET
    $totalPages = [Math]::Max(1, [int]$resp.totalPages)
    $count = [int]$resp.totalCount
    foreach($item in $resp.items) {
      $sum += [int64]$item.payable
    }
    $page++
  }
  return @{ sum = $sum; count = $count }
}

try {
  $series = Invoke-RestMethod -Uri "$BaseUrl/api/reports/revenue-series?mode=total&bucket=day&payableMaxVnd=100000000" -Method GET
  Assert-True ($series.mode -eq "total") "mode should be total"
  Assert-True ($series.bucket -eq "day") "bucket should be day"
  Assert-True ($null -ne $series.points) "points should not be null"

  $seriesByProduct = Invoke-RestMethod -Uri "$BaseUrl/api/reports/revenue-series?mode=byProduct&bucket=week&productId=1" -Method GET
  Assert-True ($seriesByProduct.mode -eq "byproduct") "mode should be normalized byproduct"
  Assert-True ($seriesByProduct.bucket -eq "week") "bucket should be week"

  $sharedFilter = "$BaseUrl/api/bills?status=completed&timeField=completed&payableMaxVnd=100000000"
  $billSummary = Get-BillsSummary -Url $sharedFilter

  $csv = Invoke-WebRequest -Uri "$BaseUrl/api/bills/export.csv?status=completed&timeField=completed&payableMaxVnd=100000000" -UseBasicParsing
  $rows = $csv.Content | ConvertFrom-Csv
  $csvSum = 0
  foreach($row in $rows) {
    $csvSum += [int64]$row.Payable
  }

  $chart = Invoke-RestMethod -Uri "$BaseUrl/api/reports/revenue-series?mode=total&bucket=day&payableMaxVnd=100000000" -Method GET
  $chartSum = 0
  foreach($point in $chart.points) {
    $chartSum += [int64]$point.revenueVnd
  }

  Assert-True ($billSummary.sum -eq $csvSum) "sum(list) must equal sum(csv)"
  Assert-True ($chartSum -eq $billSummary.sum) "sum(chart points) must equal sum(list)"

  $xlsxPath = Join-Path $env:TEMP "revenue-nontech.xlsx"
  if(Test-Path $xlsxPath) { Remove-Item $xlsxPath -Force }
  Invoke-WebRequest -Uri "$BaseUrl/api/reports/export-nontech.xlsx?bucket=day&payableMaxVnd=100000000" -OutFile $xlsxPath -UseBasicParsing | Out-Null
  Assert-True (Test-Path $xlsxPath) "xlsx file should be downloaded"
  $xlsxBytes = [System.IO.File]::ReadAllBytes($xlsxPath)
  Assert-True ($xlsxBytes.Length -gt 200) "xlsx file should be > 200 bytes"
  Assert-True ($xlsxBytes[0] -eq 0x50 -and $xlsxBytes[1] -eq 0x4B) "xlsx file must have PK zip signature"

  Add-Type -AssemblyName System.IO.Compression.FileSystem
  $zip = [System.IO.Compression.ZipFile]::OpenRead($xlsxPath)
  try {
    $sheetEntries = $zip.Entries | Where-Object { $_.FullName -like "xl/worksheets/sheet*.xml" }
    Assert-True ($sheetEntries.Count -ge 4) "xlsx must contain at least 4 worksheet xml files (Summary, TimeSeries, ByBuyer, ByProduct)"

    $summaryEntry = $zip.Entries | Where-Object { $_.FullName -eq "xl/worksheets/sheet1.xml" } | Select-Object -First 1
    Assert-True ($null -ne $summaryEntry) "Summary sheet (sheet1.xml) must exist"

    $reader = New-Object System.IO.StreamReader($summaryEntry.Open())
    $summaryXml = $reader.ReadToEnd()
    $reader.Close()
    if($chartSum -gt 0) {
      Assert-True ($summaryXml -match ">$chartSum<") "Summary sheet must contain total revenue $chartSum equal to chart sum"
    }
  }
  finally {
    $zip.Dispose()
  }

  Write-Host "[STEP-05] PASS" -ForegroundColor Green
  exit 0
}
catch {
  Write-Host "[STEP-05] FAIL: $($_.Exception.Message)" -ForegroundColor Red
  exit 1
}
