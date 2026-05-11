# API tests — ba kênh backend song song (chốt)

Chi tiết lý do và gate đóng step: `Planning_doc_demo/Lessonlearn.md` mục **§19**.

## Ba kênh (chạy song song, không thay thế lẫn nhau)

| Kênh | Tài sản trong repo | Mục đích |
|------|-------------------|----------|
| **1 — Script + automated** | `dotnet test` trên `RevenueModule.Api.Tests` + file `step-*-*.ps1` dưới đây | Hồi quy nhanh, lặp lại được; agent/CI chạy và dán output. |
| **2 — Postman** | `postman/RevenueModule.postman_collection.json` + `postman/RevenueModule.local.postman_environment.json` | Collection Runner: assert status/body; khám phá API. |
| **3 — Manual** | File `step-*-*.http` (REST Client), hoặc Postman từng request, curl | Edge case, quan sát header/body, demo tay. |

**Frontend:** không nằm trong bộ ba này trừ khi step yêu cầu rõ (xem Lessonlearn §19).

## Biến môi trường

- `BASE_URL` — mặc định `http://localhost:5093` (profile `http` của `RevenueModule.Api`). Có thể `http://127.0.0.1:5093`.

## Chạy script (Windows PowerShell)

```powershell
cd d:\BT_intdemo1\marketify-mini-ecommerce\module_src\backend\api-tests
.\step-00-health.ps1
$env:BASE_URL = 'http://127.0.0.1:5093'
.\step-d2-products-smoke.ps1
```

API phải đang chạy (`dotnet run` trong `RevenueModule.Api`).

## `dotnet test` (cùng kênh 1)

```powershell
dotnet test "d:\BT_intdemo1\marketify-mini-ecommerce\module_src\backend\RevenueModule.Api.Tests\RevenueModule.Api.Tests.csproj"
```

## Postman (kênh 2)

1. Import **Collection:** `postman/RevenueModule.postman_collection.json`
2. Import **Environment:** `postman/RevenueModule.local.postman_environment.json`
3. Chọn env **RevenueModule local**, mở folder **D2 — Products**, **Run collection** (theo thứ tự folder).

## File `.http` (kênh 3 — gợi ý manual)

Mở trong VS Code (REST Client) hoặc tương đương.

## Bảng script theo step

| Step | Script `.ps1` | `.http` | Postman folder |
|------|----------------|---------|----------------|
| 0 | `step-00-health.ps1` | `step-00-health.http` | Step 0 — Smoke |
| D2 Products | `step-d2-products-smoke.ps1` | `step-d2-products-smoke.http` | D2 — Products (smoke + contracts) |

Khi thêm step API mới: thêm một dòng bảng + cập nhật Postman + Lessonlearn nếu đổi quy ước.
