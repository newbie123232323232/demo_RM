# Issues History — Workflow & Process

Mục tiêu file này: ghi lại **vấn đề về quy trình làm việc** (không phải lỗi kỹ thuật thuần), quyết định đã chốt, và cách vận hành để tránh lặp lại.

---

## 2026-05-05 — Giai đoạn khởi tạo module doanh thu/VipPoint

### 1) Nguồn cấu hình chưa thống nhất (`.env` vs config mặc định .NET/Angular)
- **Issue (workflow):** Kỳ vọng ban đầu là mọi thứ đọc trực tiếp từ `.env`, trong khi runtime mặc định của .NET/Angular không làm vậy.
- **Tác động:** Dễ hiểu nhầm là phải hard-code hoặc mỗi người tự map một kiểu.
- **Đã xử lý:** Chốt mô hình production-lite:
  - Backend ưu tiên `ConnectionStrings:RevenueDb`, fallback `DATABASE_URL`.
  - Không đọc `.env` trực tiếp trong runtime mặc định.
  - `.env`/`.env.example` giữ vai trò nguồn điền thông tin thống nhất cho team.
- **Quy ước tiếp theo:** Mọi thay đổi biến môi trường phải cập nhật đồng thời:
  - `module_src/.env.example`
  - `module_src/README.md`
  - `Planning_doc/Lessonlearn.md` (mục lệnh vận hành nếu liên quan)

### 2) Chuẩn URL DB giữa stack khác nhau chưa rõ ngay từ đầu
- **Issue (workflow):** URL Postgres theo thói quen FastAPI (`postgresql+asyncpg://...`) chưa được chốt sớm cách dùng trong .NET.
- **Tác động:** Tốn vòng trao đổi để thống nhất công thức dùng chung.
- **Đã xử lý:** Chốt cả 2 lớp biểu diễn:
  - Input workflow cho team: `DATABASE_URL=postgresql+asyncpg://hmuser:123456@localhost:5432/HMModule`
  - Runtime .NET: convert sang Npgsql connection string.
- **Quy ước tiếp theo:** Khi có URL chuẩn theo stack A, luôn ghi thêm mapping tương đương stack B trong tài liệu.

### 3) Thiếu “runbook lệnh” theo format cố định
- **Issue (workflow):** Trước đó lệnh vận hành xuất hiện rải rác trong chat, chưa theo format chuẩn để copy dùng ngay.
- **Tác động:** Dễ sai thao tác khi chuyển ngữ cảnh (run/restart/migrate/health check).
- **Đã xử lý:** Chuẩn hóa trong `Planning_doc/Lessonlearn.md` theo format:
  - **mục đích: lệnh**
  - bổ sung nhóm lệnh migration + database update + health ping.
- **Quy ước tiếp theo:** Mỗi khi chốt bước lớn (Step), cập nhật ngay runbook tương ứng theo cùng format.

### 4) Điều phối phiên chạy chưa rõ ownership (ai dừng/ai chạy lại)
- **Issue (workflow):** Có nhiều terminal/session chạy đồng thời, đôi lúc build/migration bị chặn do phiên trước chưa dừng.
- **Tác động:** Gián đoạn xác minh tiến độ, dễ hiểu nhầm lỗi do code.
- **Đã xử lý:** Chốt thao tác vận hành:
  - Dừng phiên chiếm cổng trước khi build/migrate.
  - Chạy lại API sau migration và verify qua `/health`.
- **Quy ước tiếp theo:** Trước các thao tác thay đổi schema/version:
  1) kiểm tra phiên đang chạy,
  2) dừng phiên cũ,
  3) chạy migration/update,
  4) run lại service,
  5) ping health để xác nhận.

### 5) Trạng thái chốt spec cần “single source of truth”
- **Issue (workflow):** Các quyết định quan trọng (DB local, VipPoint int, 409 conflict, Postman strategy) từng xuất hiện ở nhiều điểm chat.
- **Tác động:** Nguy cơ lệch hiểu giữa tài liệu và thực thi.
- **Đã xử lý:** Gom chốt vào bộ tài liệu `Planning_doc/*` + `module_src/.env.example`.
- **Quy ước tiếp theo:** Khi có quyết định mới, cập nhật theo thứ tự:
  1) `business_requirements.md` (hợp đồng nghiệp vụ),
  2) `devplan_checklist.md` (hành động triển khai),
  3) `Lessonlearn.md` (vận hành),
  4) `issues_history.md` (bài học workflow).

### 6) Tài nguyên test bị tản mạn trước khi vào Step 1
- **Issue (workflow):** Script test, hướng dẫn manual và Postman bị để ở nhiều nơi, khó truy vết theo từng Step.
- **Tác động:** Mất thời gian handoff, dễ bỏ sót khi đóng Step.
- **Đã xử lý:** Chốt cấu trúc test mới tại `module_src/test/` theo quy tắc step-based.
- **Quy ước tiếp theo:** Mỗi Step phải có:
  - `manual-test.md`
  - `step-NN-api.ps1`
  - và cập nhật request vào **collection Postman dùng chung toàn module** (`module_src/test/RevenueModule.postman_collection.json`), không tạo collection riêng theo Step.

### 7) Lệch quy ước Postman (theo Step vs dùng chung module)
- **Issue (workflow):** Đã từng tạo collection Postman riêng cho Step 1, trái với mục tiêu user là một file import dùng chung toàn API module.
- **Tác động:** Tăng phân mảnh test assets, dễ mất đồng bộ khi mở rộng endpoint.
- **Đã xử lý:** Chuyển sang collection Postman duy nhất cho module và xoá file collection theo Step.
- **Quy ước tiếp theo:** Mọi endpoint mới phải thêm folder/request vào collection chung, không tạo file postman mới theo Step trừ khi có yêu cầu đặc biệt.

### 8) Nhiễu trạng thái do task background fail nhưng service thực tế vẫn chạy
- **Issue (workflow):** Nhiều thông báo task shell fail (`exit_code` lớn, bind cổng fail) xuất hiện trong khi API vẫn hoạt động.
- **Tác động:** Dễ mất thời gian restart thừa hoặc chẩn đoán sai tình trạng runtime.
- **Đã xử lý:** Chuẩn hóa follow-up: luôn xác nhận bằng `GET /health` và endpoint nghiệp vụ trước khi hành động tiếp.
- **Quy ước tiếp theo:** Notification lỗi task chỉ là tín hiệu; quyết định tiếp theo dựa trên trạng thái service thực tế.

### 9) Thiếu gate checklist sau mỗi milestone
- **Issue (workflow):** Có lúc code/test đã xong nhưng checkbox checklist chưa cập nhật đồng bộ.
- **Tác động:** Trạng thái dự án mơ hồ, handoff khó.
- **Đã xử lý:** Cập nhật lại checklist ngay sau khi pass test của từng Step.
- **Quy ước tiếp theo:** Kết thúc mỗi milestone phải làm đủ bộ 3: cập nhật test assets, chạy test, tick checklist.

### 10) Đóng Step 4 khi chưa chốt hiệu năng truy vấn
- **Issue (workflow):** Có xu hướng đánh dấu Step 4 done khi API/filter hoạt động, nhưng chưa bổ sung DB index cần thiết.
- **Tác động:** Dễ phát sinh nợ hiệu năng và phải sửa muộn khi dữ liệu tăng.
- **Đã xử lý:** Bổ sung migration `Step4BillFilterIndexes` trước khi chốt Step.
- **Quy ước tiếp theo:** Step có filter/query phải có mục “index/perf gate” pass rồi mới được tick done.

### 11) Kiểm thử CSV chưa đủ độ tin cậy nếu chỉ nhìn text
- **Issue (workflow):** Chỉ assert chuỗi header không đảm bảo file CSV dùng được với công cụ bảng tính.
- **Tác động:** Có thể pass giả, fail khi user import thực tế.
- **Đã xử lý:** Nâng script Step 4 parse CSV bằng `ConvertFrom-Csv` và assert cột bắt buộc + row count.
- **Quy ước tiếp theo:** Test export phải có validation parser-level, không chỉ string-level.

### 12) Luồng Pending chưa complete không có đường xử lý trên UI
- **Issue (workflow):** Trạng thái Pending có thể bị bỏ quên; nếu UI không cho resume thì team phải can thiệp DB.
- **Tác động:** Vận hành thủ công, rủi ro sai dữ liệu và mất audit trail.
- **Đã xử lý:** Thêm luồng resume Pending tại `/lab/bill` + endpoint cập nhật Pending (`PUT /api/bills/{id}/pending`).
- **Quy ước tiếp theo:** Mọi trạng thái trung gian (không phải terminal) phải có đường thao tác rõ ràng trong UI hoặc API vận hành chính thức.

### 13) Update Pending làm lệch dữ liệu nếu không khóa boundary
- **Issue (workflow):** Chỉnh bill Pending có thể vô tình đổi buyer hoặc không cân bằng lại VipPoint đã trừ từ lần confirm trước.
- **Tác động:** Sai số dư VipPoint, khó đối soát lịch sử bill.
- **Đã xử lý:** Rule cập nhật Pending: buyer immutable; trước khi áp payload mới phải hoàn điểm cũ rồi trừ lại điểm mới.
- **Quy ước tiếp theo:** Mọi API update trạng thái trung gian phải định nghĩa rõ boundary bất biến và cơ chế rollback/re-apply side effects.

### 14) Đối chiếu chart/list/csv lệch do mốc thời gian lọc không đồng nhất
- **Issue (workflow):** List/CSV mặc định lọc theo `PendingAt`, trong khi chart doanh thu lọc theo `CompletedAt`.
- **Tác động:** Team dễ kết luận sai là chart sai số, dù thực tế khác "time field".
- **Đã xử lý:** Bổ sung `timeField` cho list/CSV (`pending|completed`) và chốt rule đối chiếu doanh thu dùng `status=completed&timeField=completed`.
- **Quy ước tiếp theo:** Mọi tính năng reconciliation đa nguồn phải chốt cùng semantics filter trước khi viết test.

### 15) CSV numeric format phụ thuộc locale gây parse sai
- **Issue (workflow):** Export CSV từng dùng định dạng số theo locale host, dẫn tới giá trị thập phân tách bằng dấu phẩy và làm lệch cột khi parse.
- **Tác động:** Test parser-level có thể fail giả; import Excel/Sheets không ổn định giữa máy.
- **Đã xử lý:** Chuẩn hóa CSV bằng `InvariantCulture` cho toàn bộ field số.
- **Quy ước tiếp theo:** Tất cả export machine-readable (CSV/JSON snapshot) phải pin culture/format tường minh, không dựa locale runtime.

### 16) Step 5 thiếu gate "đối chiếu liên nguồn" nếu chỉ test endpoint đơn lẻ
- **Issue (workflow):** Chỉ test `revenue-series` độc lập chưa đủ để bảo đảm đúng số với list/CSV thực tế.
- **Tác động:** Rủi ro sai lệch dữ liệu doanh thu khi UI hiển thị khác báo cáo tải xuống.
- **Đã xử lý:** Thêm `step-05-api.ps1` assert `sum(chart points) == sum(list payable) == sum(csv payable)` cùng bộ lọc.
- **Quy ước tiếp theo:** Step có nhiều nguồn hiển thị cùng chỉ số phải có test đối chiếu chéo bắt buộc trước khi tick done.

### 17) Drift contract API list (array -> paged object) làm UI Pending “mất dữ liệu”
- **Issue (workflow):** Sau khi đổi `GET /api/bills` sang response phân trang (`items/page/...`), màn `/lab/bill` vẫn parse kiểu mảng trực tiếp.
- **Tác động:** UI không hiển thị bill Pending dù DB có dữ liệu thật, dễ bị hiểu sai là lỗi nghiệp vụ/DB.
- **Đã xử lý:** Sửa FE `/lab/bill` đọc `BillListResponse.items` cho luồng pending.
- **Quy ước tiếp theo:** Mọi thay đổi contract DTO/API phải có checklist “đổi toàn bộ consumer” + smoke test UI cho các màn phụ thuộc trước khi merge.

### 18) FE báo `Connection refused` do API down nhưng dễ bị nhầm thành lỗi data/filter
- **Issue (workflow):** Người dùng thấy UI không load dữ liệu, F12 báo `ERR_CONNECTION_REFUSED` khi gọi `http://localhost:5093/...` trong lúc FE vẫn chạy bình thường.
- **Tác động:** Team dễ chẩn đoán sai hướng (nghĩ do DB/filter/logic), mất thời gian debug vào business layer.
- **Đã xử lý:** Chuẩn hóa runbook chẩn đoán nhanh: kiểm tra `GET /health`; nếu fail thì restart API; sau đó verify thêm endpoint dữ liệu (`/api/buyers`) trước khi quay lại test UI.
- **Quy ước tiếp theo:** Mọi incident “UI không load data” phải đi qua gate runtime trước:
  1) FE up?,
  2) API up?,
  3) `/health` ok?,
  4) 1 endpoint dữ liệu trả 200?;
  chỉ khi pass 4 bước mới điều tra logic nghiệp vụ.

### 19) Step 6 integration test fail do trộn DB provider (Npgsql + InMemory)
- **Issue (workflow):** Thiết lập test host ban đầu remove DbContext chưa triệt để, dẫn tới service provider giữ đồng thời 2 provider EF và test fail giả.
- **Tác động:** Làm chậm vòng xác nhận Step 6, dễ hiểu nhầm là regression business logic.
- **Đã xử lý:** Bỏ hướng InMemory cho integration scope; dùng provider runtime qua `ConnectionStrings:RevenueDb` và tạo dữ liệu test riêng bằng API.
- **Quy ước tiếp theo:** Integration test cấp contract phải ưu tiên provider/runtime gần production, tránh fake provider nếu đang verify hành vi end-to-end.

### 20) Step 6 revenue-series assertion không deterministic khi dùng dữ liệu dùng chung
- **Issue (workflow):** Test `mode=total` cộng cả dữ liệu completed sẵn trong DB test nên expected không ổn định.
- **Tác động:** Test đỏ không phản ánh bug thật, gây nhiễu chất lượng tín hiệu CI.
- **Đã xử lý:** Scope test theo buyer test mới tạo (`mode=byBuyer&buyerId=...`) để cô lập dataset.
- **Quy ước tiếp theo:** Mọi assert tổng hợp số liệu phải có data boundary rõ ràng (tenant/buyer/time-window) để deterministic.

### 21) Chuẩn hóa error contract cần rollout đồng bộ BE/FE
- **Issue (workflow):** Đổi format lỗi ở backend mà không nâng FE fallback dễ tạo trải nghiệm “lỗi chung chung”.
- **Tác động:** Người dùng khó phân biệt lỗi validation với lỗi timeout/network.
- **Đã xử lý:** Backend trả thống nhất `ApiError(code,message)` cho 400/404/409; FE bổ sung timeout guard 10s và message rõ ràng theo loại lỗi.
- **Quy ước tiếp theo:** Mọi thay đổi contract lỗi API phải có checklist rollout đồng bộ: controller + client parser + smoke test các nhánh lỗi chính.

---

## Template cập nhật (dùng cho các lần sau)

```text
### [Ngày] [Tên issue workflow]
- Issue (workflow):
- Tác động:
- Đã xử lý:
- Quy ước tiếp theo:
```

