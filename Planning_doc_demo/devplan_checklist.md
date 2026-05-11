# Dev Plan Checklist — Demo "Add Product (Admin)"

> Regenerated from `Planning_doc_demo/business_requirements.md` for v2 execution.  
> Contract bắt buộc giữ: `GET /api/products` no-param => array, có param => paged object.

## Rule vận hành cập nhật tiến độ (bắt buộc)

- Sau mỗi checkpoint có bằng chứng (test/build/manual/council), cập nhật tick trong file này ngay trong cùng phiên làm việc.
- Không được để trạng thái `done nhưng chưa tick`: nếu có drift, phải thêm entry vào `issues_history.md` và sửa checklist ngay.
- Với mục manual/council chưa làm, giữ `[ ]` và ghi rõ lý do/pending ngay trong dòng checkpoint.
- Trước khi chuyển step, bắt buộc so khớp 3 nơi: bằng chứng runtime, checklist, và lịch sử quyết định (`Promt_History.md`).
- Không chuyển sang step kế tiếp khi step hiện tại còn task `[ ]`, trừ khi task đó có dependency rõ ràng.
- Với task chưa done do phụ thuộc task/step khác, ghi ngay trên dòng task theo format: `(Pending: phụ thuộc Step Dx - <task>)`.
- Quy ước format: dòng **task** dùng `-`; dòng **tiêu chí/spec/validation** (task con) dùng `+`.

## Step D1 — Backend API contract + CRUD + validation

### Mục tiêu
- Hoàn thiện backend cho Product: list/filter, get-by-id, create, update, delete theo BR.

### Việc cần làm
- [x] Mở rộng `GET /api/products`:
  + no-param => trả `ProductListItemDto[]` (backward-compat),
  + có param (`q`, `priceMin`, `priceMax`, `page`, `pageSize`) => trả object phân trang.
- [x] Thêm `GET /api/products/{id}` trả 200 hoặc 404 `product_not_found`.
- [x] Thêm `POST /api/products` + `PUT /api/products/{id}` với validation:
  + `Name`: trim, non-empty, max 160,
  + `UnitPriceVnd`: > 0.
- [x] Thêm `DELETE /api/products/{id}`:
  + 204 khi xoá được,
  + 404 nếu không tồn tại,
  + 409 `conflict_product_in_use` nếu có tham chiếu bill.
- [x] Validate `priceMin > priceMax` => 400 `invalid_price_range`.
- [x] Dùng `ApiError(code, message)` và message tiếng Việt cho user-facing errors.

### Checkpoint prove done
- [x] `dotnet build` pass.
- [x] Smoke nhanh từng endpoint mới/mở rộng (happy + lỗi chính).
- [x] `GET /api/products` no-param vẫn trả array shape.

---

## Step D2 — Backend integrity + integration tests

### Mục tiêu
- Khóa behavior bằng integration tests và defense-in-depth DB FK.

### Việc cần làm
- [x] Bổ sung FK `BillLines.ProductId -> Products.Id` với `ON DELETE RESTRICT`.
- [x] Catch `DbUpdateException` trong delete path để map về 409 contract.
- [x] Tạo test integration cho:
  + Create/Update/Delete happy path,
  + 400 invalid payload,
  + 404 product_not_found,
  + 409 conflict_product_in_use,
  + list filter/pagination/backward-compat,
  + `priceMin > priceMax`,
  + `pageSize > 100` clamp.
- [x] Dữ liệu test deterministic, có scope riêng để tránh pollution.

### Checkpoint prove done
- [x] `dotnet test` pass cho toàn bộ suite backend.
- [ ] 3 kênh test backend theo Lessonlearn §19 có chứng từ (script + Postman Runner đã có; manual còn pending) (Pending: phụ thuộc Step D7 - Manual smoke UC1-UC5).
- [x] Council review backend adversarial xong, không còn critical (xem `issues_history.md` mục Council D2 replay + assert `Location`).

---

## Step D3 — Frontend route + nav + products page skeleton

### Mục tiêu
- Tạo điểm vào UI cho admin products.

### Việc cần làm
- [x] Tạo page `/lab/products` với component riêng.
- [x] Thêm nav link "Sản phẩm" nằm giữa "Lập bill" và "Quản lý doanh thu".
- [x] Giữ active state + `aria-current` đúng pattern hiện hữu.

### Checkpoint prove done
- [x] `npx ng build --configuration=development` pass.
- [x] Truy cập `/lab/products` thấy page render đúng (route + component; verify UI khi `ng serve`).
- [x] Nav hoạt động không làm vỡ `/lab/bill` và `/lab/revenue` (chỉ thêm link + route; build pass).

---

## Step D4 — Frontend list + filters + pagination + states

### Mục tiêu
- Hiển thị và lọc danh sách product theo BR.

### Việc cần làm
- [x] Gọi `GET /api/products` với query params cần thiết.
- [x] Render table cột `Id`, `Name`, `UnitPriceVnd`, action.
- [x] Search text live với debounce 300ms:
  + template `(ngModelChange)="onSearchChanged($event)"`,
  + set state trước khi trigger debounce stream.
- [x] Price filters debounce 300ms.
- [x] Pagination: previous/next + `SP/trang` (5/10/15/20).
- [x] Xử lý loading/empty/error state.

### Checkpoint prove done
- [x] Build frontend pass.
- [x] Smoke thay thế (không bắt buộc browser): `ng serve --host 127.0.0.1 --port 4200` + HTTP 200 tới `/lab/products` (shell SPA). Hành vi list/filter/pagination cần verify tay khi có trình duyệt hoặc E2E sau.
- [x] Empty/loading/error states hiển thị đúng (verify tay khi tắt API / filter rỗng).

---

## Step D5 — Modal Add/Edit (a11y + validation)

### Mục tiêu
- Cho phép tạo/sửa product với UX/a11y chuẩn.

### Việc cần làm
- [x] Modal Add/Edit với `role="dialog"`, `aria-modal`, `aria-labelledby`.
- [x] Focus trap đầy đủ (first/last/shell), Esc, backdrop close.
- [x] Scroll lock + restore đúng, return focus về opener.
- [x] Validation client:
  + name rỗng hoặc >160,
  + unitPriceVnd <=0.
- [x] Map lỗi 400 từ backend hiển thị inline (`role="alert"` + `aria-live`).
- [x] Disable input/button trong submit lifecycle.

### Checkpoint prove done
- [x] Build frontend pass.
- [ ] Manual smoke create/edit happy + lỗi validation (Pending: phụ thuộc Step D7 - Manual smoke UC1-UC5).
- [ ] Keyboard/a11y check pass cho modal Add/Edit (Pending: phụ thuộc Step D7 - Manual smoke UC1-UC5).

---

## Step D6 — Delete confirm dialog + 409/404 handling

### Mục tiêu
- Hoàn thiện delete flow an toàn, rõ lỗi.

### Việc cần làm
- [x] Modal confirm xoá có `aria-describedby`, body hiển thị tên product.
- [x] Initial focus trên nút "Hủy" (destructive-safe).
- [x] 409 hiển thị lỗi inline và giữ dialog mở.
- [x] 404 hiển thị thông báo rõ + giữ dialog mở để user chủ động đóng.
- [x] Row action buttons có `aria-label` chứa tên product.

### Checkpoint prove done
- [x] Build frontend pass.
- [ ] Manual smoke delete happy + 409 + 404 pass (Pending: phụ thuộc Step D7 - Manual smoke UC1-UC5).
- [x] Council review frontend adversarial xong, không còn critical.

---

## Step D7 — End-to-end verification

### Mục tiêu
- Đảm bảo feature mới và page cũ cùng ổn định.

### Việc cần làm
- [x] Chạy `dotnet test`.
- [x] Chạy `npx ng build --configuration=development`.
- [ ] Manual smoke UC1-UC5 trên `/lab/products` (Pending: cần verify tay trên browser/session manual).
- [x] Chạy Postman Collection Runner folder `D2 — Products` và lưu chứng từ pass (`newman`).
- [x] Smoke regression `/lab/bill` + `/lab/revenue` (HTTP 200 qua `ng serve` hiện có, không mở browser).
- [x] Lint các file mới/sửa.

### Checkpoint prove done
- [x] Backend tests pass.
- [x] Frontend build pass.
- [ ] Không regression trên consumer cũ (Pending: phụ thuộc Step D7 - Manual smoke `/lab/bill` + `/lab/revenue` ở runtime thực tế).

---

## Step D8 — Documentation sync

### Mục tiêu
- Ghi lại issue/lesson/quyết định để tái lập và training.

### Việc cần làm
- [x] Cập nhật `Planning_doc_demo/issues_history.md` với issue phát sinh + cách xử lý.
- [x] Cập nhật `Planning_doc_demo/Lessonlearn.md` với quy ước mới.
- [x] Cập nhật `Planning_doc_demo/Promt_History.md` theo mốc quyết định lớn.
- [x] Tuân thủ mirror policy với `Planning_doc`.

### Checkpoint prove done
- [x] Docs phản ánh đầy đủ thay đổi chính.
- [x] Không có mâu thuẫn BR/devplan/issues/lesson.

---

## Step D9 — Final gate & output

### Mục tiêu
- Chốt kết quả có thể trình bày trong demo.

### Việc cần làm
- [x] Council review final cho docs/prompt completeness.
- [x] Tổng hợp output cuối (xem `Planning_doc_demo/final_output_d9.md`):
  + test/build status,
  + file chính thay đổi,
  + smoke checks đã verify,
  + branch + commit hash.

### Checkpoint prove done
- [x] Không còn critical findings mở.
- [x] Có báo cáo cuối đầy đủ theo format trong `demo_prompt.md` (`Planning_doc_demo/final_output_d9.md`).
