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

## 4. Quy ước FK conflict semantics

- Xoá entity có FK ràng buộc ở chỗ khác → **không** silent fail, **không** soft-delete ngầm:
  - Backend: explicit check trước khi xoá, trả `409` với code rõ ràng (`conflict_product_in_use`).
  - Không dựa vào `DbUpdateException` rồi parse — kém deterministic.
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

## 7. Quy ước commit cho demo

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
