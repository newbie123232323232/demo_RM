# Revenue Module (`module_src`) - Runbook

This folder is an independent mini-module (backend + frontend + tests) for revenue/VipPoint flow.

## 1) Start local runtime

- API:
  - `cd d:\BT_intdemo1\marketify-mini-ecommerce\module_src\backend\RevenueModule.Api`
  - `dotnet run --launch-profile http`
- Frontend:
  - `cd d:\BT_intdemo1\marketify-mini-ecommerce\module_src\frontend`
  - `npx ng serve --host 127.0.0.1 --port 4200`

Health checks:
- `http://localhost:5093/health`
- `http://localhost:4200/lab/bill`
- `http://localhost:4200/lab/revenue`

## 2) Database migration (when schema changes)

- `cd d:\BT_intdemo1\marketify-mini-ecommerce\module_src\backend\RevenueModule.Api`
- `dotnet ef migrations add <MigrationName> --output-dir Data/Migrations`
- `dotnet ef database update`

## 3) Build and test gates

Backend:
- `cd d:\BT_intdemo1\marketify-mini-ecommerce\module_src\backend\RevenueModule.Api.Tests`
- `dotnet test --nologo`

Frontend:
- `cd d:\BT_intdemo1\marketify-mini-ecommerce\module_src\frontend`
- `npx ng build --configuration=development`

Regression scripts:
- `cd d:\BT_intdemo1\marketify-mini-ecommerce\module_src\test`
- `powershell -ExecutionPolicy Bypass -File .\step-01\step-01-api.ps1`
- `powershell -ExecutionPolicy Bypass -File .\step-02\step-02-api.ps1`
- `powershell -ExecutionPolicy Bypass -File .\step-03\step-03-api.ps1`
- `powershell -ExecutionPolicy Bypass -File .\step-04\step-04-api.ps1`
- `powershell -ExecutionPolicy Bypass -File .\step-05\step-05-api.ps1`

## 4) Common incident: FE loads but no data

If browser shows `ERR_CONNECTION_REFUSED` for `localhost:5093`:
1) Verify API process is running.
2) Check `GET /health`.
3) Restart API if needed.
4) Verify one business endpoint (`/api/buyers`) returns `200` before debugging business logic.
# module_src — Revenue / VipPoint (thử nghiệm)

Thư mục này chứa **toàn bộ** mã nguồn backend và frontend của module thử nghiệm, **tách biệt** với `MarketifyBackend` và `MarketifyClient`.

## Cấu trúc

| Thư mục | Mô tả |
|---------|--------|
| `backend/RevenueModule.Api` | ASP.NET Core Web API (scaffold độc lập, .NET 9). |
| `frontend/` | Angular riêng: **`/` → `/lab/bill`**, **`/lab/revenue`**; menu “Lập bill (fake buyer)” / “Quản lý doanh thu”. `environment.ts`: API mặc định `http://localhost:5093`. |
| `.env.example` | Mẫu biến môi trường (Postgres, URL API, CORS, quy tắc VipPoint, debounce preview, v.v.) — copy thành **`.env`**; không commit `.env`. |

**Cấu hình:** xem **`module_src/.env.example`** (gom từ `Planning_doc/devplan_checklist.md` + BRD). Backend/FE hiện dùng `appsettings` / `environment.ts`; đồng bộ tay hoặc User Secrets cho mật khẩu DB.

**Production-lite backend config (không đọc `.env` trực tiếp):**

- Ưu tiên `ConnectionStrings:RevenueDb`.
- Nếu chưa có, API fallback sang env var `DATABASE_URL` và tự convert về format Npgsql.
- Chấp nhận cả:
  - `postgresql://user:pass@host:5432/db`
  - `postgresql+asyncpg://user:pass@host:5432/db` (phù hợp URL bạn đang dùng cho FastAPI).

## Test API (hai luồng)

- Script + `.http`: **`backend/api-tests/`** (chi tiết `README.md` trong thư mục đó).
- Postman / tay: cùng `baseUrl` như `Lessonlearn.md`.

## Quy tắc (bắt buộc)

Xem **`Planning_doc/Lessonlearn.md`**. Tóm tắt:


- **Không** reference, import, hay chia sẻ build với project trong `MarketifyBackend` / `MarketifyClient`.
- **Được** tham chiếu nghiệp vụ từ `Planning_doc/business_requirements.md` và checklist tương ứng.

## Chạy nhanh (local)

**API**

```bash
cd backend/RevenueModule.Api
dotnet run
```

**UI** (sau `npm install` trong `frontend` nếu cần)

```bash
cd frontend
npm install
npm start
```

Cổng mặc định tùy template; cấu hình CORS và URL API khi nối hai phần.

## Liên hệ với repo gốc

Cùng repository chỉ để tiện **versioning tài liệu** (`Planning_doc/`). Module này **không** là một phần deploy của solution Marketify gốc trừ khi quyết định khác sau này.
