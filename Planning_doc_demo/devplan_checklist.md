# Dev Plan Checklist — Demo "Add Product (Admin)"

> **Quy ước:** Mỗi step kết thúc đều có checkpoint "prove done" — chỉ tick `[x]` khi pass đủ check. Step có verify-gate phải chạy lệnh và verify thực tế trước khi mark done.

## Step D1 — Backend: mở rộng `ProductsController` thành full CRUD + filter

### Backend
- [ ] **D1-B1.** `GET /api/products`:
  - Khi không có query → giữ response **mảng phẳng** `ProductListItemDto[]` (backward-compat với `/lab/bill`, `/lab/revenue`).
  - Khi có ≥ 1 query (`q`, `priceMin`, `priceMax`, `page`, `pageSize`) → trả object phân trang `{ items, page, pageSize, totalCount, totalPages }`.
  - `q`: case-insensitive contains theo `Name` (`EF.Functions.ILike` cho Postgres).
  - `priceMin`/`priceMax`: `>=` / `<=` trên `UnitPriceVnd`.
  - `page` mặc định 1, `pageSize` mặc định 20, clamp `pageSize` ≤ 100.
- [ ] **D1-B2.** `GET /api/products/{id}`:
  - 200 + `ProductListItemDto` nếu có.
  - 404 `ApiError(code="product_not_found", message)` nếu không.
- [ ] **D1-B3.** `POST /api/products`:
  - Body `CreateProductRequest { Name, UnitPriceVnd }`.
  - Validation tập trung trong helper `ValidateProductPayload`: trim Name, không rỗng, ≤ 200 ký tự; `UnitPriceVnd > 0`.
  - 201 + body `ProductListItemDto`, header `Location: /api/products/{id}`.
  - 400 `invalid_payload` với message field cụ thể.
- [ ] **D1-B4.** `PUT /api/products/{id}`:
  - Validation y hệt POST.
  - 200 + body cập nhật.
  - 404 nếu id không tồn tại.
- [ ] **D1-B5.** `DELETE /api/products/{id}`:
  - Check `BillLines.Any(x => x.ProductId == id)` trước khi xoá.
  - Nếu có tham chiếu → 409 `conflict_product_in_use`.
  - Nếu không → xoá → 204.
  - 404 nếu id không tồn tại.
- [ ] **D1-B6.** Pattern code: dùng `async`/`CancellationToken`/`AsNoTracking()` nhất quán với các controller khác. Helper validation đặt private trong cùng file.

### Checkpoint prove done D1
- [ ] **D1-P1.** `dotnet build` pass.
- [ ] **D1-P2.** Manual smoke với REST client / curl từng endpoint mới (happy + 1 case lỗi mỗi loại).
- [ ] **D1-P3.** Verify backward-compat: `GET /api/products` không param → vẫn array (vì `/lab/bill`, `/lab/revenue` consume kiểu này).

---

## Step D2 — Backend: integration tests

### Test
- [ ] **D2-T1.** File mới `module_src/backend/RevenueModule.Api.Tests/DemoProductCrudIntegrationTests.cs` (theo pattern `Step6IntegrationTests.cs`).
- [ ] **D2-T2.** Test `CreateProduct_HappyPath_Returns201WithBody`.
- [ ] **D2-T3.** Test `CreateProduct_EmptyName_Returns400`.
- [ ] **D2-T4.** Test `CreateProduct_PriceZeroOrNegative_Returns400`.
- [ ] **D2-T5.** Test `CreateProduct_NameOver200Chars_Returns400`.
- [ ] **D2-T6.** Test `UpdateProduct_HappyPath_Returns200`.
- [ ] **D2-T7.** Test `UpdateProduct_NotFound_Returns404`.
- [ ] **D2-T8.** Test `DeleteProduct_HappyPath_Returns204`.
- [ ] **D2-T9.** Test `DeleteProduct_WhenReferencedByBill_Returns409`.
- [ ] **D2-T10.** Test `GetProductsList_WithFilter_PagesAndSearches`.
- [ ] **D2-T11.** Test `GetProductsList_NoQuery_StillReturnsArrayShape` (backward-compat guard).
- [ ] **D2-T12.** Cleanup tất cả product/bill test sau khi chạy (theo lesson hiện có).

### Checkpoint prove done D2
- [ ] **D2-P1.** `dotnet test` pass đủ bộ (cả test cũ Step 1-6.5 vẫn pass).
- [ ] **D2-P2.** Council review backend đầu tiên — không còn critical.

---

## Step D3 — Frontend: route + nav + skeleton component

### Frontend
- [ ] **D3-F1.** Tạo component standalone `ProductsPageComponent` ở `module_src/frontend/src/app/pages/products/products.component.{ts,html,css}` (skeleton: page header + placeholder).
- [ ] **D3-F2.** Đăng ký route `/lab/products` trong app routing (lazy-load hoặc trực tiếp tuỳ pattern hiện có).
- [ ] **D3-F3.** Thêm nav link "Sản phẩm" trong `app.component.html` đặt giữa "Lập bill" và "Quản lý doanh thu", giữ pattern `routerLinkActive` + `aria-current` đã có Phase 1.

### Checkpoint prove done D3
- [ ] **D3-P1.** `npx ng build --configuration=development` pass.
- [ ] **D3-P2.** Mở `/lab/products` thấy page skeleton, nav active đúng.
- [ ] **D3-P3.** 2 page cũ vẫn truy cập bình thường, không layout broken.

---

## Step D4 — Frontend: list + filter + pagination + states

### Frontend
- [ ] **D4-F1.** Service hoặc inline `HttpClient` (theo pattern revenue page) gọi `GET /api/products` với HttpParams: `q`, `priceMin`, `priceMax`, `page`, `pageSize`.
- [ ] **D4-F2.** Khi có filter ≠ rỗng → gọi với param và parse object phân trang. Khi filter rỗng + chỉ tải lần đầu → vẫn nên gọi có `page=1&pageSize=20` để dùng response phân trang (đơn giản hoá UI). Lưu ý: chỉ vậy thôi cũng đủ vì page này là page admin riêng, không phải consumer cũ.
- [ ] **D4-F3.** Card filter: input search (debounce 300ms hoặc đơn giản `(ngModelChange)`), input priceMin / priceMax, nút "Reset bộ lọc".
- [ ] **D4-F4.** Bảng product: `Id`, `Name`, `UnitPriceVnd` (`col-num`), action col (Sửa / Xoá).
- [ ] **D4-F5.** Pagination: "Trang trước", "Trang sau", "SP/trang" (5/10/15/20).
- [ ] **D4-F6.** States: loading hint, error message, empty state card dashed, sẵn dùng tokens Phase 1.
- [ ] **D4-F7.** CTA "Thêm sản phẩm" (primary button) ở section-head của card list — chưa wire modal, chỉ stub click trong step này.

### Checkpoint prove done D4
- [ ] **D4-P1.** Build pass.
- [ ] **D4-P2.** Manual smoke: load page thấy danh sách, filter theo tên + giá hoạt động, paginate hoạt động.
- [ ] **D4-P3.** Empty state hiển thị khi filter không khớp; loading/error state hiển thị khi tắt API.

---

## Step D5 — Frontend: modal Add/Edit (a11y full)

### Frontend
- [ ] **D5-F1.** Modal component inline trong template `products.component.html` (dùng pattern modal product picker đã hardened).
- [ ] **D5-F2.** State trong component: `isFormOpen`, `formMode: 'create' | 'edit'`, `formProduct: { id?, name, unitPriceVnd }`, `formError`, `isSubmitting`.
- [ ] **D5-F3.** Mở từ "Thêm sản phẩm" → mode create, form rỗng. Mở từ "Sửa" trên row → mode edit, form prefill.
- [ ] **D5-F4.** A11y: `role="dialog"`, `aria-modal="true"`, `aria-labelledby` trỏ tới heading (id duy nhất), `tabindex="-1"`, focus dialog khi mở, Esc đóng, click backdrop đóng, click trong dialog stopPropagation, body scroll lock, return focus tới opener khi đóng, Tab/Shift+Tab focus trap.
- [ ] **D5-F5.** Validation client-side: trim name, error nếu rỗng hoặc > 200 ký tự, error nếu unitPriceVnd ≤ 0.
- [ ] **D5-F6.** Submit: disable nút trong khi loading; map lỗi `400 invalid_payload` từ backend hiển thị inline trong modal; happy path → đóng modal + reload list giữ nguyên page filter.

### Checkpoint prove done D5
- [ ] **D5-P1.** Build pass.
- [ ] **D5-P2.** Manual smoke: tạo mới, sửa tên, sửa giá; Esc/click backdrop/Tab cycle/return focus đều OK; lỗi validation client + server đều show rõ.

---

## Step D6 — Frontend: confirm dialog xoá + handle 409

### Frontend
- [ ] **D6-F1.** State `isDeleteConfirmOpen`, `deleteTarget: ProductListItem | null`, `deleteError`, `isDeletingNow`.
- [ ] **D6-F2.** Modal confirm dùng cùng pattern a11y. Body show tên product. Nút "Xoá" có class `btn-danger`.
- [ ] **D6-F3.** Submit DELETE: nếu happy → đóng dialog + reload list. Nếu 409 → hiển thị lỗi inline trong dialog "Sản phẩm đang được dùng trong bill, không thể xoá", giữ dialog mở để user thấy.
- [ ] **D6-F4.** Nếu 404 → hiển thị "Sản phẩm không tồn tại hoặc đã bị xoá", reload list, đóng dialog.

### Checkpoint prove done D6
- [ ] **D6-P1.** Build pass.
- [ ] **D6-P2.** Manual smoke: xoá happy + xoá product có bill → thấy lỗi 409 rõ ràng.
- [ ] **D6-P3.** Council review FE adversarial — không còn critical.

---

## Step D7 — Verification toàn diện

- [ ] **D7-V1.** `dotnet test` pass.
- [ ] **D7-V2.** `npx ng build --configuration=development` pass.
- [ ] **D7-V3.** Manual smoke đầy đủ 5 UC (UC1-UC5).
- [ ] **D7-V4.** Manual smoke 2 page cũ: `/lab/bill` add line/preview/confirm/complete; `/lab/revenue` filter list / product picker / chart load / export / reconcile — tất cả không regression.
- [ ] **D7-V5.** Lint sạch trên các file mới/sửa.
- [ ] **D7-V6.** A11y nhanh: keyboard tab order trong page và 2 modal mới, focus-visible thấy rõ, Esc trong modal hoạt động, return focus về nút opener đúng.

---

## Step D8 — Tài liệu & distill prompt

- [ ] **D8-DOC1.** `Lessonlearn.md` (Planning_doc_demo) điền đủ luật/quy ước rút ra.
- [ ] **D8-DOC2.** `issues_history.md` (Planning_doc_demo) log issue gặp + cách giải quyết.
- [ ] **D8-DOC3.** `Promt_History.md` (Planning_doc_demo) đầy đủ các exchange quyết định.
- [ ] **D8-DOC4.** `demo_prompt.md` distill xong (song ngữ VI-EN, đủ tái lập v2).
- [ ] **D8-DOC5.** Council review final — verify `demo_prompt.md` đủ tái lập v2.

---

## Step D9 — Cherry-pick docs về main

- [ ] **D9-G1.** Trên branch `demo/add-product-page-v1`: commit folder `Planning_doc_demo/` thành commit riêng (tách khỏi commit code).
- [ ] **D9-G2.** Checkout `main`, cherry-pick chỉ commit docs.
- [ ] **D9-G3.** Push `main`. Branch `demo/add-product-page-v1` giữ nguyên trên local + remote để tham chiếu nếu cần.
