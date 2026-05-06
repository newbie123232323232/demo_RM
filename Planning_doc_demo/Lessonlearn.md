# Lesson Learned — Demo "Add Product (Admin)"

> File này chỉ chứa luật/quy ước rút ra **trong v1 demo**. Các luật chung của module vẫn ở `Planning_doc/Lessonlearn.md`.

## 1. Quy ước branch cho demo (Option A + docs-on-main)

- Mọi implementation feature demo sống trên branch `demo/add-product-page-v1` (hoặc các v2/v3… kế tiếp).
- Tag `demo-baseline` đánh dấu HEAD của `main` ngay trước khi tách branch — dùng làm điểm khôi phục an toàn.
- Tài liệu `Planning_doc_demo/` được commit trong cùng branch v1 trong khi build, đến cuối được **cherry-pick riêng** sang `main` để các lần demo sau (v2, v3…) đều có nguồn tham chiếu ổn định mà không cần lôi code v1 về.
- `main` **không bao giờ** chứa code work-in-progress của demo, chỉ chứa code production-grade + tài liệu định hướng.

## 2. Quy ước backward-compat khi mở rộng API dùng chung

- Khi mở rộng endpoint `GET /api/products` đã được nhiều consumer dùng (trong module này: `/lab/bill`, `/lab/revenue`):
  - **Mặc định không param → giữ nguyên shape cũ** (`ProductListItemDto[]`).
  - Chỉ trả response shape mới (object phân trang) khi caller chủ động gửi param.
- Ưu tiên này phù hợp module nhỏ: tránh phải đụng các consumer cũ khi không cần thiết, vẫn cho phép page admin mới dùng phân trang.
- Trade-off: response polymorphism hơi xấu về API design. Chấp nhận trong scope demo; nếu mở rộng thêm consumer, cân nhắc tách `GET /api/products/search` riêng hoặc bump version.

## 3. Quy ước modal a11y (kế thừa Phase 1, mở rộng cho demo)

- Mọi modal mới (Add/Edit, Confirm Delete) bắt buộc:
  - `role="dialog"`, `aria-modal="true"`, `aria-labelledby` trỏ tới id duy nhất.
  - `tabindex="-1"` trên dialog để focus được; auto-focus khi mở.
  - Esc đóng, click backdrop đóng, click trong dialog `stopPropagation`.
  - Body scroll lock khi mở (lưu `document.body.style.overflow` trước, restore khi đóng — không clobber `''`).
  - Return focus tới opener khi đóng (lưu opener qua `document.activeElement` lúc mở).
  - Tab/Shift+Tab focus trap trong dialog.
- Modal nào đã có sẵn helper (`releaseProductPickerSideEffects`, `onPickerKeydown` ở revenue page) thì khi tạo modal mới có thể tạo helper riêng cùng pattern, không share state cross-component.
- **Focus trap đầy đủ cần 3 nhánh** (revenue picker chỉ có 2 — phát hiện trong council FE D6, đã fix ở products page):
  1. Shift+Tab khi `active === first || active === dialog` → wrap về `last`.
  2. Tab khi `active === last` → wrap về `first`.
  3. Tab khi `active === dialog` (focus đang trên `tabindex="-1"` shell) → forward sang `first`. Nếu thiếu nhánh này, focus thoát ra ngoài modal.
- **Initial focus cho destructive confirm dialog** đặt trên nút Hủy (không phải nút Xoá) để tránh user vô tình Enter/Space → confirm.
- **Error elements trong dialog/page** bắt buộc có `role="alert"` + `aria-live`:
  - `aria-live="polite"` cho field-level validation (không gián đoạn ngữ cảnh).
  - `aria-live="assertive"` cho dialog-level/page-level error (nội dung quan trọng cần nghe ngay).
- **Action button trong table row** luôn có `[attr.aria-label]="'<verb> ' + entity.<displayName>"` để screen reader có context entity, không chỉ "Sửa" / "Xoá" trống nghĩa.
- **Disable input + button trong submit lifetime** (không chỉ disable nút) để chặn state drift khi network chậm.

## 4. Quy ước FK conflict semantics

- Xoá entity có FK ràng buộc ở chỗ khác → **không** silent fail, **không** soft-delete ngầm:
  - Backend: explicit check trước khi xoá (deterministic 409 với code `conflict_product_in_use`).
  - **Defense-in-depth:** thêm DB-level FK với `ON DELETE RESTRICT` để DB chặn race giữa application check và `SaveChanges`. Controller bọc `try/catch (DbUpdateException)` và map về cùng 409 — không lộ exception ra client.
  - Migration cụ thể: `DemoProductFkOnBillLine` (`AddForeignKey BillLines.ProductId → Products.Id`).
- Frontend: hiển thị lỗi inline trong dialog (giữ dialog mở để user thấy ngay tại điểm thao tác), không hiển thị lỗi global rồi đóng dialog.

## 5. Quy ước tài liệu auto-log Promt_History.md

- Sau **mỗi exchange có quyết định nghiệp vụ/kỹ thuật** trong v1 demo, AI tự cập nhật `Planning_doc_demo/Promt_History.md` ngay:
  - Số thứ tự đánh liên tục từ 1.
  - Prompt user: rút gọn nhưng giữ nguyên ý.
  - Quyết định/output AI: tóm tắt 1-3 câu, kèm tên file bị ảnh hưởng nếu có.
- **Không log lặt vặt** (xác nhận, "ok", reply ngắn không có quyết định). Mục tiêu là sản xuất ra "ghi chép có giá trị tái lập".
- Cuối v1, distill thành `demo_prompt.md` — file này là deliverable chính cho v2 demo.

## 6. Quy ước council-review trong v1 demo

- Bắt buộc 3 lần council review:
  - Sau Step D2 (backend xong): focus vào API contract, FK semantics, validation edge case.
  - Sau Step D6 (frontend xong): focus vào a11y modal, behavior preservation page cũ.
  - Trước khi mark plan completed: focus vào `demo_prompt.md` — kiểm tra prompt có đủ thông tin để v2 ra ~95% giống v1 không.
- Tất cả review chạy adversarial (subagent đóng vai hostile reviewer).
- Critical findings phải fix ngay; major findings fix nếu effort thấp; minor defer.

## 7. Quy ước input validation đối xứng giữa các controller

- Khi mở rộng filter có range (priceMin/priceMax, dateMin/dateMax v.v.):
  - Bắt buộc validate `min ≤ max` ngay trên controller, trả `400 invalid_*_range`.
  - Lý do: silent empty result làm UX rối (user không biết là filter sai vs data thật sự rỗng).
  - Đã có pattern cũ: `BillsController.GetList` validate `from ≤ to`. ProductsController phải đối xứng.

## 8. Known limitations giữ nguyên trong scope demo

- `EF.Functions.ILike(name, "%{q}%")` không escape `%`, `_`, `\` — search literal chứa các ký tự này sẽ ra kết quả bất ngờ. Acceptable cho demo (admin tìm tên sản phẩm thực, không phải user-facing). Nếu mở rộng ra ngoài demo → escape metachar trước khi truyền vào ILike.
- Không có optimistic concurrency (rowversion/etag) trên `PUT /api/products/{id}` — last writer wins. Acceptable cho admin demo (1 admin user).
- `UnitPriceVnd` chỉ check `> 0`, không có hard upper bound. Acceptable cho demo; nếu prod cần cap để chặn overflow ở phép nhân `qty × price`.

## 9. Quy ước test pollution

- Test 409 (`DeleteProduct_WhenReferencedByBill_Returns409`) tạo `Buyer + Product + Bill Pending` không thể cleanup qua API hiện có (không có DELETE bill endpoint).
- Trade-off chấp nhận: mỗi test run pile thêm vài rows test data với GUID prefix duy nhất, không ảnh hưởng test khác (filter scope theo prefix).
- Nếu muốn cleanup hoàn toàn: hoặc thêm dev-only DELETE bill endpoint, hoặc inject `RevenueDbContext` vào test fixture để xoá trực tiếp. Cả hai đều ngoài scope demo.

## 10. Quy ước commit cho demo

- Plain commit, không trailer, không HEREDOC fancy.
- Tách commit **code** và commit **docs** riêng biệt để bước cherry-pick về main không lẫn code feature.
- Suggested commit messages:
  - `demo(product): scaffold planning_doc_demo skeleton + business spec + dev checklist`
  - `demo(product): backend CRUD + filter + 409 FK conflict`
  - `demo(product): backend integration tests for CRUD + filter`
  - `demo(product): frontend page list/filter/pagination`
  - `demo(product): frontend modal add/edit + a11y`
  - `demo(product): frontend confirm delete + 409 handling`
  - `demo(product): planning_doc_demo finalize + distill demo_prompt.md`
