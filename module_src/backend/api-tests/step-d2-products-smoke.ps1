# D2 — smoke Products API (chạy khi API đã `dotnet run`, cổng mặc định 5093)
# Agent/CI: kiểm tra hợp đồng HTTP cơ bản bổ sung cho `dotnet test` (integration).
# Chạy: .\step-d2-products-smoke.ps1
# Hoặc: $env:BASE_URL='http://127.0.0.1:5093'; .\step-d2-products-smoke.ps1

$ErrorActionPreference = 'Stop'
$base = if ($env:BASE_URL) { $env:BASE_URL.TrimEnd('/') } else { 'http://localhost:5093' }

Write-Host "=== GET $base/health ==="
$h = Invoke-RestMethod -Uri "$base/health" -Method Get
if ($h.status -ne 'ok') { throw "Expected status=ok" }
Write-Host "OK:" ($h | ConvertTo-Json -Compress)

Write-Host "=== GET /api/products (no query -> JSON array) ==="
$flat = Invoke-RestMethod -Uri "$base/api/products"
if ($flat -isnot [System.Array]) { throw "Expected JSON array when no list query params" }
Write-Host "OK: array length" $flat.Length

Write-Host "=== GET /api/products?page=1&pageSize=2 (paged shape) ==="
$paged = Invoke-RestMethod -Uri "$base/api/products?page=1&pageSize=2"
if (-not $paged.PSObject.Properties['items']) { throw "Expected paged body with 'items'" }
Write-Host "OK: totalCount=$($paged.totalCount) page=$($paged.page)"

Write-Host "=== GET /api/products?priceMin=100&priceMax=50 -> 400 ==="
try {
    Invoke-WebRequest -Uri "$base/api/products?priceMin=100&priceMax=50" -UseBasicParsing | Out-Null
    throw "Expected 400 invalid_price_range"
}
catch {
    if ($_.Exception.Response) {
        $code = [int]$_.Exception.Response.StatusCode
        if ($code -ne 400) { throw "Expected 400, got $code" }
        Write-Host "OK: 400 as expected"
    }
    else { throw $_ }
}

$uniq = "SmokePS1-$(Get-Date -Format 'yyyyMMddHHmmss')"
Write-Host "=== POST /api/products ($uniq) ==="
$body = @{ name = $uniq; unitPriceVnd = 12000 } | ConvertTo-Json
$post = Invoke-WebRequest -Uri "$base/api/products" -Method POST -Body $body -ContentType 'application/json; charset=utf-8' -UseBasicParsing
if ($post.StatusCode -ne 201) { throw "Expected 201, got $($post.StatusCode)" }
$loc = $post.Headers['Location']
if (-not $loc) { throw "Expected Location header on 201" }
$created = $post.Content | ConvertFrom-Json
$id = [int]$created.id
Write-Host "OK: 201 id=$id Location=$loc"

Write-Host "=== DELETE /api/products/$id ==="
$del = Invoke-WebRequest -Uri "$base/api/products/$id" -Method DELETE -UseBasicParsing
if ($del.StatusCode -ne 204) { throw "Expected 204, got $($del.StatusCode)" }
Write-Host "OK: 204"

Write-Host "=== GET /api/products/$id -> 404 ==="
try {
    Invoke-WebRequest -Uri "$base/api/products/$id" -UseBasicParsing | Out-Null
    throw "Expected 404 after delete"
}
catch {
    if ($_.Exception.Response) {
        $code = [int]$_.Exception.Response.StatusCode
        if ($code -ne 404) { throw "Expected 404, got $code" }
        Write-Host "OK: 404"
    }
    else { throw $_ }
}

Write-Host "All D2 Products smoke checks passed."
