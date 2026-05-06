# API tests — hai luồng song song (chốt)

## Nguyên tắc

| Luồng | Ai dùng | Mục đích |
|--------|---------|----------|
| **A — Script trong repo** | Agent / CI / bạn chạy lệnh | Hồi quy nhanh, lặp lại được, gắn với từng **Step** trong `devplan_checklist.md`. |
| **B — Công cụ bên ngoài** | Bạn (Postman, Insomnia, curl tay, …) | Khám phá API, edge case, header/auth sau này. |

Mỗi khi **hoàn thành một Step** có API mới:

1. Thêm (hoặc cập nhật) file **`step-NN-*.ps1`** và tốt nhất kèm **`step-NN-*.http`** cùng mô tả trong README này (bảng dưới).
2. Cập nhật **Postman** (folder tương ứng Step, request trùng endpoint + body mẫu).

**Biến môi trường chung**

- `BASE_URL` = `http://localhost:5093` (profile `http` của `RevenueModule.Api`).

## Chạy script (Windows PowerShell)

```powershell
cd d:\BT_intdemo1\marketify-mini-ecommerce\module_src\backend\api-tests
.\step-00-health.ps1
```

Nếu API chạy cổng khác: `$env:BASE_URL = 'http://localhost:XXXX'; .\step-00-health.ps1`

## File `.http`

Mở bằng VS Code (extension REST Client) hoặc JetBrains — click **Send Request** trên từng khối.

## Bảng script theo Step

| Step | Script | Endpoint / ghi chú |
|------|--------|-------------------|
| 0 | `step-00-health.ps1`, `step-00-health.http` | `GET /health`, `GET /` |
| 1+ | *(thêm khi implement)* | buyers, products, … |

## Postman — file import dùng chung toàn module

1. Import **Collection dùng chung:** `../../test/RevenueModule.postman_collection.json`
2. (Tuỳ chọn) Import **Environment cũ:** `postman/RevenueModule.local.postman_environment.json` hoặc tự tạo env `baseUrl=http://localhost:5093`.
3. Mỗi Step done: thêm folder/request vào **cùng file collection dùng chung** để đồng bộ với `step-NN-*.ps1`.
