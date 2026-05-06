# Lesson learned — quy ước repo & module thử nghiệm

## Luật tổ chức: `module_src` (độc lập tuyệt đối về code)

**Ngữ cảnh:** Module doanh thu / VipPoint là **thử nghiệm**, không gắn deploy vào dự án gốc. Cần **độc lập cả khi đọc và điều tra**, không phụ thuộc biên dịch hay tài nguyên của `MarketifyBackend`, `MarketifyClient`, v.v.

### Chốt

1. **Vị trí:** Thư mục gốc **`module_src/`** nằm **ngang hàng** với `MarketifyBackend/`, `MarketifyClient/`, `Planning_doc/`, … trong cùng repository.
2. **Cấu trúc tối thiểu:**
   - `module_src/backend/` — backend **riêng** của module (ví dụ `RevenueModule.Api`).
   - `module_src/frontend/` — frontend **riêng** của module (ứng dụng Angular/UI độc lập).
3. **Cấm tái sử dụng tài nguyên codebase gốc (hard rule):**
   - **Không** thêm `ProjectReference`, `PackageReference` trỏ tới project trong `MarketifyBackend/`.
   - **Không** import/copy file nguồn từ `MarketifyClient/src/...` vào `module_src/frontend` (trừ khi là **tài liệu** hoặc **đoạn mẫu đã rewrite** hoàn toàn — vẫn ưu tiên viết mới).
   - **Không** dùng chung `DbContext`, migration, connection factory, auth middleware, hay đường dẫn build của solution gốc.
   - **Không** symlink thư mục từ repo gốc vào `module_src`.
4. **Tham chiếu chặt chẽ với codebase gốc (được phép, chỉ ở lớp “ý tưởng”):**
   - So sánh **stack** (.NET, Angular, PostgreSQL) và **pattern** ở mức khái niệm.
   - Đối chiếu hành vi với **`Planning_doc/business_requirements.md`** — đây là **hợp đồng nghiệp vụ** chung; code triển khai **chỉ** trong `module_src`.
   - Khi cần “xem gốc làm thế nào”, mở `Marketify*` **chỉ để đọc**, không merge code.
5. **Lợi ích đã học:** Giữ một cây mã nguồn monorepo để tiện **diff tài liệu / BRD**, nhưng **biên điều tra và build** của module thử nghiệm **gói gọn trong `module_src/`** — tránh lạc vào toàn bộ e-commerce legacy.

### Vi phạm

Mọi PR/commit đưa `ProjectReference` tới `MarketifyBackend` hoặc import trực tiếp từ `MarketifyClient` vào `module_src` coi là **vi phạm luật module thử nghiệm**; cần revert hoặc tách sang repo riêng nếu mục tiêu thay đổi.

---

## Khởi chạy module (`module_src`) — local dev

**Không cần** chạy `MarketifyBackend` / `MarketifyClient`. Chỉ cần hai terminal (API + Angular), trừ khi đã tích hợp Postgres và muốn test DB.

### Cổng mặc định (theo scaffold hiện tại)

| Dịch vụ | URL / cổng | Ghi chú |
|---------|------------|---------|
| **API** (`RevenueModule.Api`) | `http://localhost:5093` | Profile `http` trong `Properties/launchSettings.json`; HTTPS thêm `7069`. Kiểm tra nhanh: **`/health`** (JSON), **`/`** (text); template mẫu **`/WeatherForecast`**. **`/`** trước đây không có route → Chrome báo 404 là do không có trang gốc, không phải API tắt. |
| **Angular** (`ng serve`) | `http://localhost:4200` hoặc `http://127.0.0.1:4200` | Phải **bật** `ng serve` — chỉ `ng build` thì không có web server. |
| **FE → API** | `module_src/frontend/src/environments/environment.ts` → `apiBaseUrl` | Giữ **trùng** cổng HTTP của API (mặc định `5093`). |

### Lệnh nhanh — format **mục đích: lệnh** (PowerShell, copy một dòng)

- **Chạy API (HTTP, cổng 5093):** `cd d:\BT_intdemo1\marketify-mini-ecommerce\module_src\backend\RevenueModule.Api; dotnet run --launch-profile http`
- **Chạy Angular (dev server, cổng 4200, không prompt tương tác):** `cd d:\BT_intdemo1\marketify-mini-ecommerce\module_src\frontend; npx ng serve --host 127.0.0.1 --port 4200` *(có thể dùng `npm start` khi chắc cổng trống — nếu 4200 bận, `npm start` có thể hỏi Y/n và treo trong môi trường không tương tác)*

- **Áp dụng thay đổi code API (restart):** trong terminal đang chạy API nhấn **Ctrl+C**, rồi lại lệnh “Chạy API…” ở trên (không stop thì file `.exe` bị khóa, `dotnet build` có thể lỗi).
- **Áp dụng thay đổi code FE:** `ng serve` thường tự rebuild; nếu treo: **Ctrl+C** rồi lại lệnh “Chạy Angular…”.

- **Mở UI module:** trình duyệt → `http://localhost:4200/` (hoặc `http://127.0.0.1:4200/`).
- **Kiểm tra API có phản hồi:** trình duyệt → `http://localhost:5093/health` (JSON `status: ok`) hoặc `http://localhost:5093/` (text); mẫu template → `http://localhost:5093/WeatherForecast`.

- **Build API (không chạy server):** `cd d:\BT_intdemo1\marketify-mini-ecommerce\module_src\backend\RevenueModule.Api; dotnet build` (nên stop `dotnet run` trước nếu báo file locked).
- **Tạo migration (Step 0-B3):** `cd d:\BT_intdemo1\marketify-mini-ecommerce\module_src\backend\RevenueModule.Api; dotnet ef migrations add InitialRevenueSchema --output-dir Data/Migrations`
- **Apply migration vào Postgres local:** `cd d:\BT_intdemo1\marketify-mini-ecommerce\module_src\backend\RevenueModule.Api; dotnet ef database update`
- **Ping health + DB sau migrate:** `Invoke-RestMethod -Uri http://localhost:5093/health`
- **Build FE một lần:** `cd d:\BT_intdemo1\marketify-mini-ecommerce\module_src\frontend; npx ng build --configuration=development`

- **Tìm PID chiếm cổng 4200:** `Get-NetTCPConnection -LocalPort 4200 -ErrorAction SilentlyContinue | Select-Object OwningProcess -Unique`
- **Tìm PID chiếm cổng 5093:** `Get-NetTCPConnection -LocalPort 5093 -ErrorAction SilentlyContinue | Select-Object OwningProcess -Unique`
- **Dừng process theo PID (dọn cổng):** `Stop-Process -Id <PID> -Force` (chỉ khi chắc là `dotnet` / `node` của module)

- **Chạy Angular cổng khác (4200 bận):** `cd d:\BT_intdemo1\marketify-mini-ecommerce\module_src\frontend; npx ng serve --port 4201` → mở `http://localhost:4201/`

- **Dừng hẳn process đang chiếm cổng 5093 và 4200 rồi chạy lại cả hai:** `5093,4200 | ForEach-Object { Get-NetTCPConnection -LocalPort $_ -ErrorAction SilentlyContinue } | Select-Object -ExpandProperty OwningProcess -Unique | ForEach-Object { Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue }` — sau đó chạy lại lệnh **Chạy API** và **Chạy Angular** (dùng khi `dotnet run` báo file locked hoặc `ng serve` hỏi đổi cổng vì 4200 bận).

### Bước khởi chạy (chi tiết)

1. Mở **hai terminal**: một chạy lệnh **Chạy API…**, một chạy **Chạy Angular…**.
2. Đợi log `Now listening on: http://localhost:5093` và `Local: http://localhost:4200/`.
3. Mở UI và/hoặc `/health` như trên.

### Dọn cổng (bổ sung)

Dùng các dòng **Tìm PID** / **Stop-Process** / **Chạy Angular cổng khác** trong mục **Lệnh nhanh** ở trên. Có thể thay `Get-NetTCPConnection` bằng `netstat -ano | findstr :4200` (hoặc `:5093`).

---

## Chốt kỹ thuật bổ sung (đồng bộ BRD)

| Hạng mục | Quyết định |
|----------|------------|
| **PostgreSQL (dev)** | Local: `localhost:5432`, DB `HMModule`, user `hmuser`, password tự cấu hình (pgAdmin server vd `MONI local`). **Chỉ dev local** trong v1; Docker để sau. Connection string mẫu: `appsettings.Development.json` → `ConnectionStrings:RevenueDb` (hoặc env `DATABASE_URL`). |
| **VipPoint** | Chỉ **số nguyên** (`int ≥ 0`). Điểm nhận: `Payable / 1_000_000` (chia nguyên). |
| **Complete lặp** | **`409 Conflict`** + body có `message` (vd `code`: `bill_already_completed`). *Vì sao 409:* request hợp lệ nhưng **trạng thái resource** (bill đã Completed) **mâu thuẫn** với hành động — chuẩn REST hơn `400` (sai input tổng quát) hay `404`. |
| **Postman** | Dùng **một collection chung cho toàn module** tại `module_src/test/RevenueModule.postman_collection.json`; mỗi Step chỉ thêm request/folder vào collection này. |

---

## Phương pháp test API (chốt — hai luồng song song)

- **Luồng A (trong repo):** thư mục **`module_src/backend/api-tests/`** — mỗi Step có script **`step-NN-*.ps1`** (PowerShell, `Invoke-RestMethod`) và tốt nhất file **`step-NN-*.http`** (VS Code REST Client / tương tự). Agent hoặc bạn chạy script sau khi Step xong để hồi quy.
- **Luồng B (bạn, bên ngoài):** **Postman** / Insomnia / curl — cùng `baseUrl` (`http://localhost:5093`), tự tạo Collection theo folder từng Step; dùng để thử tay, biên, header sau này.
- **Quy ước:** khi **đóng một Step** có endpoint mới → **cập nhật** `api-tests/README.md` (bảng script), **thêm** script + **đồng bộ** request Postman (cùng URL/body mẫu).
- **Chi tiết & hướng dẫn chạy:** `module_src/backend/api-tests/README.md`.

## Bổ sung workflow test theo Step (cập nhật 2026-05-05)

- Thư mục test cấp module: **`module_src/test/`** (ngang hàng `backend` và `frontend`).
- Mỗi Step có thư mục riêng: **`module_src/test/step-NN/`**.
- Tối thiểu cho mỗi Step:
  - `manual-test.md` (manual test chi tiết)
  - `step-NN-api.ps1` (script agent tự chạy)
- Postman dùng **một file collection chung cho toàn module API**:
  - **`module_src/test/RevenueModule.postman_collection.json`**
  - Khi xong Step mới thì thêm request/folder mới vào cùng collection này, không tách file collection theo Step.
- Lệnh chạy script Step 1:
  - **Chạy script test API Step 1:** `cd d:\BT_intdemo1\marketify-mini-ecommerce\module_src\test\step-01; .\step-01-api.ps1`
  - **Chạy script test API Step 2:** `cd d:\BT_intdemo1\marketify-mini-ecommerce\module_src\test\step-02; .\step-02-api.ps1`
  - **Chạy script test API Step 3:** `cd d:\BT_intdemo1\marketify-mini-ecommerce\module_src\test\step-03; .\step-03-api.ps1`
  - **Chạy script test API Step 4:** `cd d:\BT_intdemo1\marketify-mini-ecommerce\module_src\test\step-04; .\step-04-api.ps1`

## Issues thực tế đã gặp và tự xử lý (log nhanh)

1. **Lặp nhiều phiên API trên cùng cổng 5093**
   - Hiện tượng: `address already in use`, một số task background lỗi dù service vẫn sống.
   - Xử lý: giữ đúng 1 phiên API chạy nền; trước khi restart thì dọn process theo cổng.
2. **Build/migration fail do file exe bị khóa**
   - Hiện tượng: `MSB3021/MSB3027` khi `dotnet build` hoặc `dotnet ef`.
   - Xử lý: dừng phiên `dotnet run` trước, chạy migration/build, rồi bật lại API.
3. **Thứ tự chạy migration và kiểm thử dễ bị lệch**
   - Hiện tượng: endpoint mới chạy nhưng DB schema chưa theo kịp.
   - Xử lý: bắt buộc chuỗi `migration add` -> `database update` -> `build` -> `health` -> test API.
4. **Tiến hóa quy ước Postman**
   - Hiện tượng: từng tạo collection theo Step gây phân mảnh.
   - Xử lý: gom toàn bộ endpoint vào 1 collection chung `module_src/test/RevenueModule.postman_collection.json`.
5. **Mất kết nối mạng/phiên terminal gián đoạn**
   - Hiện tượng: task cũ fail hoặc mất context.
   - Xử lý: không phụ thuộc trạng thái task cũ, luôn xác nhận lại bằng `/health` và endpoint thật trước khi tiếp tục.
6. **Step 4 còn thiếu index filter ở backend**
   - Hiện tượng: API list/filter chạy được nhưng chưa có index hỗ trợ truy vấn theo PendingAt/Status/Payable/Product.
   - Xử lý: thêm migration index riêng (`Step4BillFilterIndexes`) trước khi đóng Step 4.
7. **CSV cần test “chuẩn import” thay vì chỉ kiểm tra text**
   - Hiện tượng: test cũ chỉ check có header string, chưa chắc parse ổn.
   - Xử lý: nâng script Step 4 để parse `ConvertFrom-Csv`, assert cột bắt buộc và số dòng dữ liệu.
8. **Bill Pending bị “kẹt” nếu UI không hỗ trợ resume**
   - Hiện tượng: bill đã confirm nhưng chưa complete có thể tồn tại lâu, trước đây chỉ xử lý bằng DB tay.
   - Xử lý: thêm danh sách Pending trên `/lab/bill`, hỗ trợ nạp lại bill, cập nhật Pending (`PUT /api/bills/{id}/pending`) và Complete ngay trong UI.
9. **Cập nhật Pending cần giữ bất biến buyer và cân bằng lại điểm**
   - Hiện tượng: nếu cho đổi buyer hoặc không hoàn/trừ lại điểm khi cập nhật sẽ tạo lệch số dư.
   - Xử lý: chốt rule update Pending: không cho đổi buyer; hoàn điểm đã trừ trước đó rồi trừ lại theo payload mới.
10. **Đổi contract API mà quên update consumer gây “mất dữ liệu giả” trên UI**
   - Hiện tượng: `GET /api/bills` đã đổi sang response phân trang (`items/page/...`) nhưng `/lab/bill` vẫn parse như mảng nên không hiện danh sách Pending.
   - Xử lý: sửa `fake-bill.component.ts` đọc `response.items`; thêm rule smoke test các màn dùng lại endpoint chung sau khi đổi DTO.
11. **Connection refused trên FE do API process không chạy**
   - Hiện tượng: UI vẫn mở được (`:4200`) nhưng gọi API lỗi `ERR_CONNECTION_REFUSED` ở F12 (thường với `http://localhost:5093/...`).
   - Xử lý: xác nhận nhanh `GET /health`; nếu fail thì khởi động lại API và verify lại `/health` + 1 endpoint nghiệp vụ (`/api/buyers`).
12. **Integration test dễ fail giả khi dùng InMemory provider thay cho provider runtime**
   - Hiện tượng: test host đăng ký đồng thời `Npgsql` + `InMemory` gây conflict provider EF.
   - Xử lý: chuyển integration test sang chạy provider thật theo cấu hình test (`ConnectionStrings:RevenueDb`), tạo dữ liệu test bằng API để tránh phụ thuộc seed mặc định.
13. **Assertion integration test lệch do dataset shared khi không scope filter đủ chặt**
   - Hiện tượng: test `revenue-series` ban đầu cộng cả dữ liệu Completed sẵn có trong DB test nên expected bị lệch.
   - Xử lý: đổi mode test sang `byBuyer` với buyer test vừa tạo để cô lập tập dữ liệu, assert deterministic.
14. **Chuẩn hóa error contract cần làm đồng thời BE + FE**
   - Hiện tượng: nếu chỉ đổi backend trả `code/message` mà FE không giữ fallback hợp lý sẽ tạo UX lỗi mơ hồ.
   - Xử lý: chốt format lỗi `ApiError(code,message)` cho 400/404/409 ở backend; FE parse `error.message` + timeout message riêng >10s để user biết do mạng/runtime hay validation.

## Rule ngắn cho cả team sau incident contract drift

- Khi đổi shape response của endpoint dùng chung (nhất là list endpoint):
  - cập nhật toàn bộ client consumer trong module (`/lab/bill`, `/lab/revenue`, script test nếu có),
  - chạy lại ít nhất 1 smoke test UI cho từng màn phụ thuộc,
  - chỉ chốt “done” sau khi xác nhận dữ liệu render khớp DB/API.
- Khi FE báo `ERR_CONNECTION_REFUSED`, luôn chẩn đoán theo thứ tự:
  - kiểm tra API có listen đúng cổng bằng `GET /health`,
  - nếu API down thì restart API trước, không debug sâu business/filter,
  - sau restart phải verify thêm 1 endpoint dữ liệu (`/api/buyers` hoặc `/api/bills`) rồi mới kết luận hệ thống hồi phục.
- Với integration test có side effects (confirm/complete/list/report):
  - luôn tạo dữ liệu test riêng qua API (không dựa dữ liệu seed có sẵn),
  - scope filter đủ hẹp (ví dụ `byBuyer` theo buyer test),
  - tránh provider giả khác runtime nếu mục tiêu là test contract hành vi end-to-end.

---

*Cập nhật: `module_src`; khởi chạy local; format mục đích: lệnh; phương pháp test API hai luồng.*
