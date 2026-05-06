param(
  [string]$BaseUrl = "http://localhost:5093",
  [int]$Seed = 20260505
)

$ErrorActionPreference = "Stop"

function Assert-True {
  param([bool]$Condition, [string]$Message)
  if (-not $Condition) { throw "Assertion failed: $Message" }
}

function Get-WeekStartMonday {
  param([datetime]$Date)
  $offset = (([int]$Date.DayOfWeek + 6) % 7)
  return $Date.Date.AddDays(-$offset)
}

function New-RandomLocalDateInWeek {
  param(
    [datetime]$WeekStart,
    [System.Random]$Random
  )
  $dayOffset = $Random.Next(0, 7)
  return $WeekStart.AddDays($dayOffset)
}

function New-ConfirmPayload {
  param(
    [int]$BuyerId,
    [array]$Products,
    [System.Random]$Random
  )

  $lineCount = if ($Random.NextDouble() -lt 0.35) { 2 } else { 1 }
  $picked = @()
  while ($picked.Count -lt $lineCount) {
    $p = $Products[$Random.Next(0, $Products.Count)]
    if (-not ($picked | Where-Object { $_.id -eq $p.id })) {
      $picked += $p
    }
  }

  $lines = @()
  foreach ($product in $picked) {
    $qty = $Random.Next(1, 4)
    $lines += @{
      productId = [int]$product.id
      qty = $qty
      unitPriceVnd = [int64]$product.unitPriceVnd
    }
  }

  $voucherRoll = $Random.Next(0, 100)
  $voucherType = "none"
  $voucherValue = 0
  if ($voucherRoll -lt 20) {
    $voucherType = "percent"
    $voucherValue = @(3,5,7,10)[$Random.Next(0,4)]
  } elseif ($voucherRoll -lt 35) {
    $voucherType = "vnd"
    $voucherValue = @(20000,50000,100000)[$Random.Next(0,3)]
  }

  return @{
    buyerId = $BuyerId
    lines = $lines
    voucherType = $voucherType
    voucherValue = $voucherValue
    vipPointsUsed = 0
  }
}

try {
  $health = Invoke-RestMethod -Uri "$BaseUrl/health" -Method GET
  Assert-True ($health.status -eq "ok") "API health must be ok before seeding"

  $buyers = Invoke-RestMethod -Uri "$BaseUrl/api/buyers" -Method GET
  $products = Invoke-RestMethod -Uri "$BaseUrl/api/products" -Method GET
  Assert-True ($buyers.Count -ge 3) "Need at least 3 buyers"
  Assert-True ($products.Count -ge 2) "Need at least 2 products"

  $rand = [System.Random]::new($Seed)
  $todayLocal = [datetime]::Now.Date
  $currentWeekStart = Get-WeekStartMonday -Date $todayLocal
  $yesterday = $todayLocal.AddDays(-1)
  $previousWeekStart = $currentWeekStart.AddDays(-7)

  $targetDates = New-Object System.Collections.Generic.List[datetime]

  # 5 bills yesterday
  1..5 | ForEach-Object { [void]$targetDates.Add($yesterday) }

  # 10 bills random days in previous week
  1..10 | ForEach-Object {
    [void]$targetDates.Add((New-RandomLocalDateInWeek -WeekStart $previousWeekStart -Random $rand))
  }

  # 5 bills, one per week in the 5 weeks before previous week
  2..6 | ForEach-Object {
    $weekStart = $currentWeekStart.AddDays(-7 * $_)
    [void]$targetDates.Add((New-RandomLocalDateInWeek -WeekStart $weekStart -Random $rand))
  }

  Assert-True ($targetDates.Count -eq 20) "Must produce exactly 20 target dates"

  # Ensure all buyers are used at least once, then random
  $buyerQueue = New-Object System.Collections.Generic.List[int]
  foreach ($b in $buyers) { [void]$buyerQueue.Add([int]$b.id) }
  while ($buyerQueue.Count -lt 20) {
    [void]$buyerQueue.Add([int]$buyers[$rand.Next(0, $buyers.Count)].id)
  }

  $seeded = @()
  for ($i = 0; $i -lt $targetDates.Count; $i++) {
    $buyerId = $buyerQueue[$i]
    $targetLocal = $targetDates[$i]
    $pendingLocal = $targetLocal.AddHours(9 + $rand.Next(0, 9)).AddMinutes($rand.Next(0, 60))
    $completedLocal = $pendingLocal.AddMinutes(5 + $rand.Next(0, 90))
    $pendingUtc = $pendingLocal.ToUniversalTime().ToString("o")
    $completedUtc = $completedLocal.ToUniversalTime().ToString("o")

    $payload = New-ConfirmPayload -BuyerId $buyerId -Products $products -Random $rand
    $confirm = Invoke-RestMethod -Uri "$BaseUrl/api/bills/confirm" -Method POST -ContentType "application/json" -Body ($payload | ConvertTo-Json -Depth 6)
    $billId = [int64]$confirm.billId
    Invoke-RestMethod -Uri "$BaseUrl/api/bills/$billId/complete" -Method POST -ContentType "application/json" -Body "{}" | Out-Null

    $backdateBody = @{
      pendingAtUtc = $pendingUtc
      completedAtUtc = $completedUtc
    } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/api/bills/$billId/test-backdate" -Method POST -ContentType "application/json" -Body $backdateBody | Out-Null

    $seeded += [pscustomobject]@{
      BillId = $billId
      BuyerId = $buyerId
      PendingLocal = $pendingLocal.ToString("yyyy-MM-dd HH:mm")
      CompletedLocal = $completedLocal.ToString("yyyy-MM-dd HH:mm")
    }
  }

  $usedBuyerIds = $seeded | Select-Object -ExpandProperty BuyerId -Unique
  foreach ($buyer in $buyers) {
    Assert-True ($usedBuyerIds -contains [int]$buyer.id) "Buyer $($buyer.id) must be used at least once"
  }

  Write-Host "[SEED-STEP05] PASS - Created 20 completed bills with historical dates." -ForegroundColor Green
  $seeded | Sort-Object PendingLocal | Format-Table -AutoSize
  exit 0
}
catch {
  Write-Host "[SEED-STEP05] FAIL: $($_.Exception.Message)" -ForegroundColor Red
  exit 1
}
