# Dev plan checklist — Module doanh thu & VipPoint (chi tiết)

Checklist triển khai theo **`business_requirements.md`**, mã trong **`module_src/`** (xem **`Lessonlearn.md`**).  
Đánh dấu `[ ]` → `[x]` khi xong.

**Hai trang UI cố định** (`module_src/frontend` — đã có shell & route)

| Đường dẫn | Nhãn menu | Mục đích |
|-----------|-----------|----------|
| **`/lab/bill`** | Lập bill (fake buyer) | Chọn buyer, Draft (client), voucher + VipPoint, preview, xác nhận thanh toán → Pending → Complete. |
| **`/lab/revenue`** | Quản lý doanh thu | Lịch sử bill + lọc + CSV; biểu đồ doanh thu + export ảnh. |

Mặc định mở app: redirect **`/` → `/lab/bill`**. Tiêu đề tab trình duyệt set theo `title` trong `app.routes.ts`.

Mỗi **bước (Step)** dưới đây là một khối công việc lớn và **gắn kết chức năng**; trong mỗi Step, phần **Backend** làm trước hoặc song song phần không phụ thuộc, rồi **Frontend** để có **UI test tay ngay** sau khi Step hoàn tất.

---

## Tiến độ hiện tại (snapshot)

- [x] **Step 0** — Khởi tạo kỹ thuật chung
- [x] **Step 1** — Master data + Page 1 (Draft/Subtotal)
- [x] **Step 2** — Preview pipeline realtime
- [x] **Step 3** — Confirm/Complete + resume Pending
- [x] **Step 4** — Revenue list/filter/CSV
- [x] **Step 5** — Biểu đồ doanh thu + export ảnh
- [x] **Step 5.5** — Báo cáo non-tech XLSX (Summary/TimeSeries/ByBuyer/ByProduct)
- [x] **Step 6** — Hoàn thiện chất lượng & tài liệu chạy

---

## Step 0 — Khởi tạo kỹ thuật chung (`module_src`)

**Mục tiêu:** API và Angular nói chuyện được; Postgres sẵn sàng; quy ước tiền & timezone không tranh cãi sau này.

### Backend

- [x] **0-B1.** Thêm **Npgsql + EF Core** vào `RevenueModule.Api` (hoặc tách `RevenueModule.Infrastructure` nếu muốn gọn — tùy team, vẫn trong `module_src`).
- [x] **0-B2.** `appsettings.Development.json`: connection string PostgreSQL **local** khớp BRD §2.1 (`Host=localhost;Port=5432;Database=moni;Username=moni;Password=***` — điền password trên máy, không commit secret).
- [x] **0-B3.** Đăng ký `DbContext`, chạy migration lần đầu (có thể chưa có bảng nghiệp vụ — chỉ verify DB connect).
- [x] **0-B4.** Bật **CORS** cho origin Angular dev (vd `http://localhost:4200`).
- [x] **0-B5.** Endpoint smoke: `GET /health` hoặc `/api/health` trả `200`.

### Frontend

- [x] **0-F1.** Cấu hình `src/environments/environment.ts`: `apiBaseUrl` trỏ tới API (mặc định **`http://localhost:5093`** theo `RevenueModule.Api/Properties/launchSettings.json` — chỉnh nếu đổi cổng).
- [x] **0-F2.** **`provideHttpClient()`** đã bật trong `app.config.ts`; interceptor (nếu cần) sau này; routing **`/` → `/lab/bill`** + menu 2 link đã có.
- [x] **0-F3.** Shell app: **nav 2 mục** — đã scaffold: `app.component` + `routerLink` tới **`/lab/bill`** và **`/lab/revenue`** (nhãn như bảng trên).

### Kiểm thử tay (Definition of Done Step 0)

- [x] **0-T1.** `dotnet run` (API) + `ng serve` (FE), gọi health từ browser hoặc FE hiển thị “API OK”.
- [x] **0-T2.** Không cần chạy bất kỳ service legacy ngoài `module_src`; chỉ cần API + FE của module này.
- [x] **0-T3.** **Hai luồng test (chốt — xem `Lessonlearn.md` + `module_src/backend/api-tests/README.md`):** chạy `api-tests/step-00-health.ps1` (hoặc `.http`); song song kiểm tra **`GET /health`** trong Postman với `baseUrl` = `http://localhost:5093`.

---

## Step 1 — Master data + Page 1 (chọn buyer, Draft dòng, Subtotal)

**Mục tiêu:** Có buyer/product thật trong DB; Page 1 chọn buyer (hiện VipPoint), thêm sản phẩm + SL vào Draft (state client), **Subtotal** đúng theo giá snapshot trên dòng (BRD §6 — giá tại lúc thêm dòng).

### Backend

- [x] **1-B1.** Entity **Buyer**: `Id`, `Name` (hoặc mã), **`VipPoint`** kiểu **`int` ≥ 0** (BRD §3.2 — chỉ số nguyên).
- [x] **1-B2.** Entity **Product**: `Id`, `Name`, `UnitPriceVnd` (long).
- [x] **1-B3.** Migration + **seed**: ít nhất 3 buyer (một số có VipPoint ≥ 5), ≥ 5 product, giá VND nguyên.
- [x] **1-B4.** API:
  - [x] `GET /api/buyers` (list + VipPoint),
  - [x] `GET /api/buyers/{id}` (chi tiết / balance),
  - [x] `GET /api/products`.
- [x] **1-B5.** (Tùy chọn v1) `POST /api/buyers` tối thiểu để tạo buyer fake nhanh không cần sửa DB — hoặc bắt buộc chỉ seed; nếu có thì Page 1 dùng.

### Frontend — **Page 1 (`/lab/bill` — phần thân)**

- [x] **1-F1.** Trong **`FakeBillPageComponent`**: dropdown **chọn buyer** → hiển thị **VipPoint khả dụng** (gọi API).
- [x] **1-F2.** Danh sách sản phẩm (từ API): chọn product + nhập **số lượng** → **Thêm vào Draft**; **gộp dòng** cùng `productId` (BRD).
- [x] **1-F3.** Bảng Draft: product, đơn giá (copy từ API tại lúc thêm), SL, thành tiền dòng; **Subtotal** = tổng (tính trên client, VND nguyên).
- [x] **1-F4.** **Đổi buyer** → **xóa Draft** + reset form voucher/VipPoint (placeholder cho step sau cũng được, nhưng hành vi cuối phải khớp BRD §5.1).

### Kiểm thử tay

- [x] **1-T1.** Chọn buyer có 7 điểm → hiển thị **7** (số nguyên, BRD §3.2).
- [x] **1-T2.** Thêm cùng product hai lần → một dòng, SL cộng dồn.
- [x] **1-T3.** Subtotal khớp tay tính từ giá seed.

---

## Step 2 — Máy tính tiền server + preview + Page 1 (realtime)

**Mục tiêu:** Một nguồn sự thật cho pipeline tiền (BRD §4.7, §4.3, §11 floor `D`); UI realtime preview khớp API.

### Backend

- [x] **2-B1.** Service **tính pipeline**: `Subtotal` → voucher thường (% Subtotal hoặc VND cố định, cap) → `B` → VipPoint (`p=0` hoặc `p≥5`, `D` floor, cap 20%) → **Payable**; từ chối `p ∈ {1,2,3,4}`.
- [x] **2-B2.** **Unit test**: biên BRD (vd Payable 2.45M → điểm dự kiến **2** — chia nguyên `/1_000_000`); case `p=0`; case cap VipPoint; voucher % + VND.
- [x] **2-B3.** `POST /api/bills/preview` — body: `buyerId`, các dòng (productId, qty, **unitPriceVnd** snapshot), loại voucher + giá trị, `vipPointsUsed`; response: `subtotal`, `voucherThuongAmount`, `baseBeforeVip`, `vipDiscount`, `payable`, `expectedVipPointsEarnedIfCompleted` (chỉ hiển thị — điểm thật khi Complete).
- [x] **2-B4.** Preview **không** ghi DB, **không** trừ điểm.

### Frontend — **Page 1 (`/lab/bill` — tiếp)**

- [x] **2-F1.** Form **voucher thường**: chọn kiểu **%** hoặc **VND** + giá trị (validate 0–100%, n ≥ 0).
- [x] **2-F2.** Input **VipPoint dùng** `p` (0 hoặc ≥ 5); hiển thị lỗi nếu 1–4 hoặc dương nhưng &lt; 5 khi đã nhập dùng điểm (theo BRD).
- [x] **2-F3.** **Debounce** gọi `POST .../preview` khi Draft / voucher / p đổi; panel realtime: Subtotal, giảm voucher, B, %/trần Vip, Payable, **dự kiến điểm nhận**.
- [x] **2-F4.** Nếu API lỗi validation → toast / inline message.

### Kiểm thử tay

- [x] **2-T1.** So sánh một ví dụ tay (voucher 5% + p lớn chạm cap) với số API và số trên UI.
- [x] **2-T2.** `p=0`: Payable = B sau voucher; không báo lỗi điểm.

---

## Step 3 — Ghi bill Pending + Complete + Page 1 (nút thanh toán & Complete)

**Mục tiêu:** Lần đầu persist DB = **Pending**; trừ điểm khi `p>0`; **Complete** cộng điểm, idempotent (BRD §4.5, §5).

### Backend

- [x] **3-B1.** Entity **Bill** + **BillLine**: snapshot `UnitPriceVnd`, `Qty`, các cột tiền (`Subtotal`, `VoucherThuongAmount`, `VipPointUsed`, `VipDiscountVnd`, `Payable`, …), `Status` (Pending | Completed), `PendingAt`, `CompletedAt` (UTC).
- [x] **3-B2.** Migration; ràng buộc: không lưu bill rỗng (≥ 1 line); `Payable ≥ 0`.
- [x] **3-B3.** `POST /api/bills/confirm` — thân giống preview + **idempotency key tùy chọn** hoặc đủ validate; transaction: tạo Bill + lines, **trừ VipPoint buyer nếu p>0**, kiểm tra đủ điểm và `p≥5` khi p>0.
- [x] **3-B4.** `POST /api/bills/{id}/complete` — chỉ từ Pending; cộng điểm **`Payable / 1_000_000`** (int); set `CompletedAt`; lần 2 → **`409 Conflict`** + body rõ ràng (BRD §4.5).
- [x] **3-B5.** `GET /api/bills/{id}` cho FE hiển thị sau confirm.

### Frontend — **Page 1 (`/lab/bill` — tiếp)**

- [x] **3-F1.** Khu vực **trạng thái + nút** (BRD §5.3): trước confirm hiển thị “Draft (chưa lưu)” hoặc tương đương.
- [x] **3-F2.** **Xác nhận thanh toán**: validate §5.5 (≥1 dòng, Subtotal&gt;0, …) → gọi confirm → nhận `billId`, status **Pending**, **bật** nút Complete.
- [x] **3-F3.** **Complete** → gọi API → status **Completed**, **tắt** nút; gọi lại `GET buyer` để thấy VipPoint mới.
- [x] **3-F4.** Hiển thị **BillId** (copy được) để sang Page 2 tra cứu.
- [x] **3-F5.** Hiển thị danh sách bill **Pending** trên `/lab/bill`; chọn một bill để **tiếp tục xử lý** (nạp lại form), cho phép **cập nhật Pending** rồi Complete.

### Kiểm thử tay

- [x] **3-T1.** Một bill Pending: DB có dòng, buyer đã trừ đúng p.
- [x] **3-T2.** Complete: buyer cộng đúng điểm (`Payable / 1_000_000`); Complete lần 2 → **409**.
- [x] **3-T3.** `p=0`: không trừ ở Pending, vẫn Complete và nhận điểm theo Payable.
- [x] **3-T4.** Pending resume: chọn bill Pending từ UI, nạp lại dữ liệu và cập nhật được bill Pending trước khi Complete.

---

## Step 4 — Page 2: Lịch sử bill + lọc + CSV

**Mục tiêu:** Một màn hình duy nhất quản lý doanh thu phần **bảng**; filter khớp BRD §8; export CSV §10.

### Backend

- [x] **4-B1.** `GET /api/bills` query: `buyerId?`, `from`/`to` (theo **PendingAt**, timezone VN), granularity day|week|month (hoặc chỉ gửi range date và server bucket — chốt một), `payableMaxVnd?` (≤ slider), `productIds` (AND), `status?` mặc định cả Pending+Completed.
- [x] **4-B2.** Trả về DTO đủ cột tooltip §10 (Subtotal, voucher, Vip, Payable, điểm dùng/nhận, PendingAt, CompletedAt).
- [x] **4-B3.** `GET /api/bills/export.csv` — cùng query; cột tối thiểu BRD §10.
- [x] **4-B4.** Index DB hỗ trợ filter (BRD checklist B5).

### Frontend — **Page 2 (`/lab/revenue` — phần bảng)**

- [x] **4-F1.** Trang **`/lab/revenue`**: bảng bill với các cột chính + tooltip chi tiết.
- [x] **4-F2.** Filter: buyer, khoảng thời gian + chọn **ngày/tuần/tháng** (mapping query), **slider** Payable ≤ max (0–100M), multi-select product (AND).
- [x] **4-F3.** **Empty state** khi không có dòng.
- [x] **4-F4.** Nút **Tải CSV** (download từ API).

### Kiểm thử tay

- [x] **4-T1.** Sau Step 3, sang Page 2 thấy bill vừa tạo (theo PendingAt).
- [x] **4-T2.** Slider + AND product (tạo thêm vài bill seed hoặc tay) cho kết quả đúng.
- [x] **4-T3.** CSV mở bằng Excel/Sheets, cột đủ.

---

## Step 5 — Page 2: Biểu đồ doanh thu + export ảnh

**Mục tiêu:** Chỉ bill **Completed**; trục thời gian theo **CompletedAt** (BRD §9); 3 chế độ + buyer+product; export PNG.

### Backend

- [x] **5-B1.** `GET /api/reports/revenue-series` — query: `mode` = total | byBuyer | byProduct | byBuyerAndProduct, `buyerId?`, `productId?`, `from`/`to`, bucket day|week|month; timezone Asia/Ho_Chi_Minh, tuần bắt đầu thứ Hai.
- [x] **5-B2.** Tổng doanh thu = tổng **Payable** trong bucket; mode product = tổng **allocated** theo mục 7 (largest remainder — implement helper + test).
- [x] **5-B3.** Đảm bảo list (Step 4) và chart dùng đúng mốc **PendingAt** vs **CompletedAt** (không lẫn).
- [x] **5-B4.** `revenue-series`: khi **cả** `from` và `to` có giá trị — trục bucket **đủ lịch** inclusive (VN, tuần thứ Hai); bucket không có Completed = **0 VND**; thiếu một biên → giữ series **thưa**; `from` > `to` → **400** `invalid_date_range`. Sheet `TimeSeries` của `export-nontech.xlsx` **đồng bộ** semantics này khi có cả hai biên hợp lệ.

### Frontend — **Page 2 (`/lab/revenue` — phần biểu đồ)**

- [x] **5-F1.** Chọn chế độ trục Y (tổng / buyer / product / buyer+product) + buyer/product khi cần.
- [x] **5-F2.** Line chart (thư viện tùy chọn: Chart.js, ng2-charts, …); trục X khi ít điểm vẫn đọc được.
- [x] **5-F3.** Bộ lọc thời gian đồng bộ khái niệm với bảng (cùng khoảng hoặc reuse state).
- [x] **5-F4.** **Export ảnh** biểu đồ theo trạng thái lọc hiện tại (html2canvas hoặc export server — chốt một).

### Kiểm thử tay

- [x] **5-T1.** Hoàn thành ≥2 bill cùng ngày → biểu đồ tổng có điểm.
- [x] **5-T2.** Đổi bucket month → số cộng khớp Payable các bill Completed trong tháng.
- [x] **5-T3.** File ảnh tải về mở được.
- [x] **5-T4.** Đối chiếu liên nguồn: `sum(chart points)` = `sum(list)` = `sum(csv)` trên cùng filter (`status=completed`, `timeField=completed`, `payableMaxVnd`).
- [x] **5-T5.** Với **cả** `from`+`to`: `revenue-series` có đủ số điểm = số bucket lịch trong kỳ; ngày/tuần/tháng “trống” có `revenueVnd=0`; XLSX `TimeSeries` cùng số dòng bucket.

### Checkpoint prove done (Step 5)

- [x] **5-P1. Runtime gate:** `GET /health` trả `status=ok`, `database=connected`.
- [x] **5-P2. Backend test gate:** `dotnet test` tại `RevenueModule.Api.Tests` pass (bao gồm integration dense-range + `invalid_date_range`), đã dọn warning conflict `Microsoft.EntityFrameworkCore.Relational` về cùng `9.0.15`.
- [x] **5-P3. API regression gate:** `module_src/test/step-04/step-04-api.ps1` pass và `module_src/test/step-05/step-05-api.ps1` pass.
- [x] **5-P4. Frontend build gate:** `npx ng build --configuration=development` pass.
- [x] **5-P5. Data coverage gate:** đã seed 20 bill lịch sử theo kịch bản kiểm chứng chart (5 bill hôm qua, 10 bill tuần trước, 5 bill cho 5 tuần trước đó; buyer random và có coverage tất cả buyer hiện có trong DB).

---

## Step 5.5 — Non-tech XLSX export (kế toán/vận hành)

**Mục tiêu:** Bổ sung file xuất `.xlsx` business-friendly cho người không tech, dùng chung filter của `/lab/revenue`, không tạo entity/domain mới.

### Backend

- [x] **5.5-B1.** Thêm package `ClosedXML 0.105.0` vào `RevenueModule.Api`.
- [x] **5.5-B2.** Endpoint `GET /api/reports/export-nontech.xlsx` (params: `buyerId`, `productId`, `from`, `to`, `payableMaxVnd`, `bucket`); semantics revenue = `Status=Completed` + `CompletedAt`.
- [x] **5.5-B3.** 4 sheet: `Summary` (KPI tổng), `TimeSeries` (Bucket/Revenue/Bills/AOV), `ByBuyer` (Buyer/Revenue/Bills/AOV/VipUsed/VipEarned), `ByProduct` (allocated revenue theo largest-remainder, qty, số bill chứa product).
- [x] **5.5-B4.** Reuse helper alloc/timezone/bucket hiện có; không tạo entity mới.

### Frontend

- [x] **5.5-F1.** Nút **Báo cáo non-tech (.xlsx)** trên `/lab/revenue` (khu chart) dùng cùng bộ filter buyer/product/from/to/payableMax/bucket.
- [x] **5.5-F2.** Hint text giải thích báo cáo dùng `CompletedAt`.

### Test

- [x] **5.5-T1.** Postman collection thêm 2 request export-nontech (day, week+buyer).
- [x] **5.5-T2.** `step-05-api.ps1` mở rộng: download xlsx, kiểm tra magic bytes `PK`, đếm ≥4 sheet xml, regex ensure `Summary` chứa số `chartSum` từ `revenue-series`.
- [x] **5.5-T3.** Manual test mục 5: 4 sheet hiện đầy đủ, Summary khớp chart total, ByProduct sum bằng Summary khi không filter product.

### Checkpoint prove done (Step 5.5)

- [x] **5.5-P1.** `dotnet build` + `dotnet test` pass (0 warning, 3/3).
- [x] **5.5-P2.** `step-04-api.ps1` + `step-05-api.ps1` pass.
- [x] **5.5-P3.** `npx ng build --configuration=development` pass.
- [x] **5.5-P4.** Sanity check: `Content-Type=application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, magic bytes `50 4B 03 04`.
- [x] **5.5-P5.** Đồng bộ đầy đủ trạng thái todo/plan mode của Step 5.5 vào checklist (backend endpoint, frontend action, test assets, verification gate).

---

## Step 6 — Hoàn thiện chất lượng & tài liệu chạy

**Mục tiêu:** Ổn định regression, tài liệu cho người mới clone repo.

### Backend

- [x] **6-B1.** Integration test (TestHost hoặc WebApplicationFactory): confirm → complete → điểm buyer; filter list; series chỉ Completed.
- [x] **6-B2.** Chuẩn hóa mã lỗi (400 validation, 409 complete lặp, 404 bill).

### Frontend

- [x] **6-F1.** Xử lý lỗi mạng / timeout tối thiểu trên Page 1 & 2.
- [x] **6-F2.** (Tùy chọn) Loading state cho bảng & chart.

### Chung

- [x] **6-T1.** Cập nhật `module_src/README.md` hoặc `Planning_doc`: lệnh migration, seed, chạy API + FE.
- [x] **6-T2.** Chạy lại toàn bộ **1-T → 5-T** như regression smoke.

### Checkpoint prove done (Step 6)

- [x] **6-P1.** `RevenueModule.Api.Tests`: pass đủ bộ (integration: confirm/complete, list filter, revenue-series completed-only, revenue-series dense calendar, invalid date range).
- [x] **6-P2.** Backend error contract chuẩn hóa `ApiError(code,message)` cho các nhánh chính 400/404/409.
- [x] **6-P3.** Frontend Page 1 + Page 2 thêm timeout guard (`10s`) và thông báo lỗi mạng nhất quán.
- [x] **6-P4.** Regression script `step-01` đến `step-05` đều pass.
- [x] **6-P5.** Build gate: `npx ng build --configuration=development` pass.
- [x] **6-P6.** Lessons/Issues Step 6 đã cập nhật đầy đủ vào `Planning_doc/Lessonlearn.md` và `Planning_doc/issues_history.md` (bao gồm nguyên nhân + cách xử lý + rule phòng ngừa).

---

## Backlog (không thuộc v1)

- [ ] **G1.** Hủy đơn & hoàn điểm (ngoài BRD hiện tại).
- [ ] **G2.** Auth / phân quyền.
- [ ] **G3.** Audit log Payable / điểm.

---

## Tham chiếu

- Nghiệp vụ: **`business_requirements.md`**.
- Quy tắc `module_src` & khởi chạy & **test API hai luồng**: **`Lessonlearn.md`**.
- Script / `.http` theo Step: **`module_src/backend/api-tests/`**.
- **Biến môi trường & hằng cấu hình** (Postgres, URL API/CORS, VipPoint %/trần/tích điểm, debounce preview, slider 100M, timezone, Postman `baseUrl`, mã lỗi 409…): **`module_src/.env.example`** — copy → **`.env`** local, không commit secret.
- Code gốc Marketify: **chỉ tham khảo ý tưởng**, không import.

---

## Step 6.5 — Hardening trước makeup UI/UX

**Mục tiêu:** fix lỗi semantics/runtime còn lại giữa list/csv/chart/xlsx; chốt UX product picker ở mức vận hành ổn định trước phase polish.

### Backend

- [x] **6.5-B1.** `BillsController` chuẩn hóa filter `from`/`to` theo lịch `Asia/Ho_Chi_Minh` (đầu ngày local đến trước đầu ngày kế), dùng UTC hợp lệ cho `timestamptz`.
- [x] **6.5-B2.** `GET /api/bills` + `GET /api/bills/export.csv`: nếu `from > to` theo ngày lịch thì trả **400** `invalid_date_range`.
- [x] **6.5-B3.** Đồng bộ hành vi thời gian với `ReportsController` để tránh lệch list/csv so với chart/xlsx.

### Frontend

- [x] **6.5-F1.** Product picker modal: thêm đóng bằng **Esc**.
- [x] **6.5-F2.** Product picker modal: focus vào dialog khi mở (tối thiểu keyboard-friendly), vẫn chỉ apply filter khi bấm **Xác nhận**.

### Test

- [x] **6.5-T1.** Integration test: `/api/bills?from=YYYY-MM-DD&to=YYYY-MM-DD` không 500, trả dữ liệu hợp lệ.
- [x] **6.5-T2.** Integration test: `/api/bills/export.csv?from=...&to=...` không 500, tải CSV hợp lệ.
- [x] **6.5-T3.** Regression nhanh: `dotnet test` + `ng build --configuration=development`.

### Checkpoint prove done (Step 6.5)

- [x] **6.5-P1.** Không còn lỗi 500 ở list/csv khi lọc ngày kiểu date-only.
- [x] **6.5-P2.** `RevenueModule.Api.Tests` pass đủ bộ (bao gồm case mới của Step 6.5).
- [x] **6.5-P3.** FE build pass, product picker keyboard path dùng được (Esc + focus mở modal).
