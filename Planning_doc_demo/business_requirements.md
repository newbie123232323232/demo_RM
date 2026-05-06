# Business Requirements — Demo "Add Product (Admin)"

## 0. Bối cảnh & mục tiêu

- **Bối cảnh:** Module doanh thu/VipPoint hiện đã có 2 page chính: `/lab/bill` (lập bill fake buyer) và `/lab/revenue` (quản lý doanh thu). Sản phẩm (`Product`) đang chỉ có endpoint `GET /api/products` để liệt kê toàn bộ; không có UI để admin tạo/sửa/xoá sản phẩm.
- **Mục tiêu demo:** Bổ sung 1 page admin `/lab/products` cho phép thao tác **CRUD đầy đủ** trên `Product` + filter cơ bản, dùng để minh hoạ cách làm việc với trợ lý AI trong coding (build từ tài liệu định hướng có sẵn).
- **Persona:** Admin nội bộ — không có auth thật trong scope demo; bất kỳ ai vào page đều có quyền CRUD.
- **Phạm vi nghiêm ngặt:** Không thay đổi business logic / API contract của các chức năng cũ (bill, revenue, chart, export, reconcile). Không thêm thuộc tính mới cho `Product`.

## 1. Phạm vi chức năng

### 1.1 Đối tượng nghiệp vụ
- `Product` giữ nguyên schema: `Id` (int, auto), `Name` (string), `UnitPriceVnd` (long, đơn vị VND).
- Không tạo entity mới, không thêm cột mới cho `Product`.

### 1.2 Use case
- **UC1 — Xem danh sách product:** admin vào `/lab/products` thấy bảng tất cả sản phẩm có phân trang.
- **UC2 — Lọc/tìm product:** admin nhập từ khoá tên + khoảng giá min/max để thu hẹp danh sách.
- **UC3 — Tạo product mới:** admin bấm "Thêm sản phẩm" → mở modal nhập tên + đơn giá → xác nhận → product được tạo và xuất hiện trong danh sách.
- **UC4 — Sửa product:** admin bấm "Sửa" trên một dòng → mở modal có dữ liệu hiện tại → chỉnh → xác nhận → product được cập nhật.
- **UC5 — Xoá product:** admin bấm "Xoá" → confirm dialog → nếu product chưa từng dùng trong bill nào thì xoá thành công; nếu đã dùng trong bill thì server từ chối với mã `409 conflict_product_in_use`, FE hiển thị lỗi rõ ràng.

### 1.3 Out of scope
- Auth/authorization thật.
- Soft-delete, audit log, lịch sử thay đổi product.
- Bulk import/export product.
- Liên kết qua lại với danh sách product trong page `/lab/bill` hay `/lab/revenue` (các page này vẫn dùng `GET /api/products` cũ, tự động thấy product mới sau lần fetch tiếp theo).

## 2. API contract

### 2.1 `GET /api/products` (mở rộng, backward-compatible)
- **Query params (đều optional):**
  - `q`: chuỗi tìm theo `Name` (case-insensitive, contains).
  - `priceMin` / `priceMax`: long, lọc theo `UnitPriceVnd` (inclusive cả 2 biên).
  - `page` (mặc định `1`), `pageSize` (mặc định `20`, max `100`).
- **Backward-compat:** khi gọi không kèm bất kỳ param nào, response vẫn là **mảng phẳng** `ProductListItemDto[]` để các consumer hiện hữu (`/lab/bill`, `/lab/revenue`) không bị vỡ.
- **Khi có ít nhất 1 param:** response là object phân trang:
  ```json
  {
    "items": [{ "id": 1, "name": "...", "unitPriceVnd": 1000000 }],
    "page": 1,
    "pageSize": 20,
    "totalCount": 42,
    "totalPages": 3
  }
  ```
- **Sort:** mặc định `OrderBy(Id)`.

### 2.2 `GET /api/products/{id}` (mới)
- 200 + `ProductListItemDto` nếu tồn tại.
- 404 `product_not_found` nếu không.

### 2.3 `POST /api/products` (mới)
- Body: `{ "name": string, "unitPriceVnd": long }`.
- Validation:
  - `name`: trim, sau trim không rỗng, độ dài ≤ 200 ký tự.
  - `unitPriceVnd`: số nguyên dương `> 0`.
- 201 + body `ProductListItemDto` (kèm `Location: /api/products/{id}` header).
- 400 `invalid_payload` nếu fail validation, message chỉ rõ field nào sai.

### 2.4 `PUT /api/products/{id}` (mới)
- Body giống POST.
- Validation tương tự.
- 200 + body `ProductListItemDto`.
- 404 `product_not_found` nếu id không tồn tại.
- 400 `invalid_payload` nếu fail validation.

### 2.5 `DELETE /api/products/{id}` (mới)
- 204 nếu xoá thành công.
- 404 `product_not_found` nếu id không tồn tại.
- 409 `conflict_product_in_use` nếu product đang được tham chiếu trong bất kỳ `BillLine` nào (kể cả bill Pending lẫn Completed). Message gợi ý: "Sản phẩm đang được dùng trong bill, không thể xoá."

### 2.6 Error contract chung
- Format thống nhất `ApiError(code, message)` (giống các controller hiện hữu).
- Mã HTTP theo chuẩn REST: 400 input, 404 not found, 409 conflict, 200/201/204 happy.

## 3. UI/UX requirements

### 3.1 Vị trí & navigation
- Route mới `/lab/products`.
- Nav link "Sản phẩm" trong app shell, đặt **giữa** "Lập bill" và "Quản lý doanh thu" (thứ tự gợi ý: Lập bill → Sản phẩm → Quản lý doanh thu).
- Active state + `aria-current="page"` đúng pattern Phase 1 makeup.

### 3.2 Layout page
- Page header: tiêu đề + subtitle ngắn.
- Card filter (giống pattern revenue page): search by name, priceMin, priceMax, nút Reset.
- Card list: bảng product với cột `Id`, `Name`, `UnitPriceVnd` (căn phải, `tabular-nums`), cột Action (Sửa / Xoá).
- Pagination ở dưới list (giống pattern revenue page): `Trang trước`, `Trang sau`, `Bill/trang` đổi thành `SP/trang` với 5/10/15/20.
- Action bar trên cùng card list có nút "Thêm sản phẩm" (primary CTA).
- Empty state: card dashed "Không có sản phẩm phù hợp với bộ lọc."
- Loading state: hint "Đang tải sản phẩm..."
- Error state: dùng class `.error` với message từ backend.

### 3.3 Modal Add/Edit
- Dùng đúng pattern modal product picker đã hardened ở revenue page:
  - `role="dialog"`, `aria-modal="true"`, `aria-labelledby` trỏ tới heading.
  - `tabindex="-1"` trên dialog, focus dialog khi open.
  - Esc đóng modal, click backdrop đóng modal, click trong dialog stopPropagation.
  - Tab/Shift+Tab focus trap nội bộ.
  - Body scroll lock khi mở, restore overflow cũ khi đóng.
  - Return focus tới opener khi đóng.
- Title:
  - "Thêm sản phẩm mới" cho create.
  - "Sửa sản phẩm #{id}" cho edit.
- Form fields: `Name` (text required), `UnitPriceVnd` (number min=1).
- Validation client-side hiện ngay khi blur: thiếu name / price ≤ 0.
- Footer: "Huỷ" (secondary) + "Xác nhận" (primary).
- Khi submit: disable cả 2 nút trong khi gọi API; nếu lỗi thì hiển thị lỗi server inline trong modal, không đóng modal.

### 3.4 Confirm dialog xoá
- Dùng modal nhỏ (cùng pattern a11y) hoặc native `confirm()` (chấp nhận được cho demo nhưng modal nội bộ là chuẩn hơn).
- **Quyết định:** dùng modal nội bộ để bảo đảm a11y/UX nhất quán.
- Title: "Xác nhận xoá".
- Body: "Bạn có chắc muốn xoá sản phẩm \"{name}\"? Hành động này không thể hoàn tác."
- Footer: "Huỷ" (secondary) + "Xoá" (danger).
- Nếu BE trả 409: hiển thị lỗi inline trong dialog kèm gợi ý "Sản phẩm đang được dùng trong bill, không thể xoá."

### 3.5 Style
- Áp dụng nguyên token modern-soft Phase 1 (`--color-*`, `--space-*`, `--radius-*`, `--shadow-*`).
- Buttons: primary cho CTA chính, danger cho Xoá, ghost cho close icon `×`.

## 4. Validation & error handling chi tiết

| Tình huống | HTTP | Code | UI hiển thị |
|---|---|---|---|
| Tạo/sửa với name rỗng (sau trim) | 400 | `invalid_payload` | "Tên sản phẩm không được để trống." |
| Tạo/sửa với name > 200 ký tự | 400 | `invalid_payload` | "Tên sản phẩm tối đa 200 ký tự." |
| Tạo/sửa với unitPriceVnd ≤ 0 | 400 | `invalid_payload` | "Đơn giá phải > 0 VND." |
| Sửa/xoá id không tồn tại | 404 | `product_not_found` | "Sản phẩm không tồn tại hoặc đã bị xoá." |
| Xoá product đang dùng trong bill | 409 | `conflict_product_in_use` | "Sản phẩm đang được dùng trong bill, không thể xoá." |
| Timeout > 10s | n/a | TimeoutError client | "Request timeout (>10s). Kiểm tra API/DB hoặc kết nối mạng." |

## 5. Definition of Done

- Backend:
  - 5 endpoint mới/mở rộng pass test integration.
  - `dotnet test` toàn module pass.
  - Không regression: các test Step 1-6.5 cũ vẫn pass.
- Frontend:
  - Page `/lab/products` build pass (`npx ng build --configuration=development`).
  - Manual smoke 5 use case (UC1-UC5) đều OK.
  - 2 page hiện hữu (`/lab/bill`, `/lab/revenue`) không thay đổi hành vi (smoke test nhanh).
- A11y:
  - Tất cả modal mới (Add/Edit, Confirm Delete) có role/aria-modal/focus-trap/scroll-lock/return-focus đầy đủ.
  - Nav có aria-current.
  - Tất cả interactive element có focus-visible ring.
- Tài liệu:
  - 6 file trong `Planning_doc_demo/` được điền đầy đủ.
  - `Promt_History.md` log đủ các quyết định v1.
  - `demo_prompt.md` distill xong, đủ ngắn để paste 1 lần khi demo, đủ chi tiết để v2 ra ~95% giống v1.

## 6. Ràng buộc tham chiếu

- Không sửa các file thuộc page `/lab/bill` và `/lab/revenue` ngoại trừ:
  - `app.component.html` để thêm nav link "Sản phẩm".
  - File routing để thêm route `/lab/products`.
- Không động vào `Planning_doc/` cũ.
- Không động vào file plan của Cursor (`.cursor/plans/`).
