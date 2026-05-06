# Promt History — Demo "Add Product (Admin)" v1

> Log chronological các exchange có quyết định nghiệp vụ/kỹ thuật trong v1 demo.
> Format: số thứ tự, prompt user (rút gọn, giữ ý), quyết định/output AI, file ảnh hưởng.

---

1. **User:** sắp tới có yêu cầu demo về chủ đề "hướng dẫn cách làm việc với trợ lý AI trong coding"; chọn module hiện tại để demo. Quyết định thêm 1 chức năng mới: page admin Add Product (không phải buyer fake). Sẽ làm việc với AI trước để tạo chức năng đó, sau đó undo về trạng thái cũ để demo lần 2 trước khán giả.
   **AI:** đề xuất 2 hướng (A: branch riêng + cherry-pick docs về main; B: làm chung main rồi undo theo version), kèm phân tích rủi ro. Đề xuất prompt contract 5 phần (Goal/Constraints/Definition of Done/Non-goals/Execution protocol).

2. **User:** đã push toàn bộ src code hiện tại lên GitHub `newbie123232323232/demo_RM` nhánh main. Yêu cầu lập một bộ Planning_doc riêng cho chức năng demo, gồm 4 file tương tự `Planning_doc/` cũ + 1 file `Promt_History.md` để AI auto-fill trong quá trình làm v1, từ đó distill ra prompt hoàn chỉnh cho v2. Hỏi nên làm trên branch riêng (Option A) hay làm trên main rồi undo theo version (Option B).
   **AI:** mạnh mẽ khuyến nghị **Option A + docs-on-main**: implementation v1 sống trên branch `demo/add-product-page-v1`, chỉ folder `Planning_doc_demo/` được cherry-pick về main để v2 luôn có nguồn tham chiếu ổn định. Đề xuất 6 file: 4 mirror + `Promt_History.md` + `demo_prompt.md` (deliverable cuối cùng, distill từ Promt_History).

3. **User:** chốt Option A + docs-on-main. Chốt scope: 1 page admin `/lab/products` ngang hàng `lab/bill` và `lab/revenue`, full CRUD (thêm/xoá/sửa/view + filter), dùng đúng các thuộc tính hiện có của Product (`Id`, `Name`, `UnitPriceVnd`), không cần truy xuất/liên kết từ product trong các danh sách khác.
   **AI:** xác nhận `Product` chỉ có 3 trường (đọc `Models/Product.cs`); `ProductsController` hiện chỉ có `GET all`. Lập plan chính thức với:
   - Branch `demo/add-product-page-v1` từ main HEAD + tag `demo-baseline`.
   - Folder `Planning_doc_demo/` ở root, 6 file.
   - Implementation chia 9 step (D1 backend CRUD + filter, D2 backend tests, D3 FE route+nav+skeleton, D4 FE list+filter+pagination, D5 FE modal Add/Edit, D6 FE confirm delete + 409, D7 verify smoke, D8 finalize docs + distill demo_prompt, D9 cherry-pick docs về main).
   - Council review 3 lần (sau D2, sau D6, trước close).
   - Backward-compat `GET /api/products`: không param → array (cũ), có param → paged object (mới).
   - FK semantics khi xoá: explicit check `BillLines`, trả 409 `conflict_product_in_use`, không silent.

4. **User:** confirm plan, yêu cầu execute từ đầu đến hết todos.
   **AI:** tạo branch `demo/add-product-page-v1` + tag `demo-baseline` từ main HEAD (commit `5ab99cb`). Scaffold folder `Planning_doc_demo/` với 6 file:
   - `business_requirements.md` (đầy đủ spec UC1-UC5, API contract chi tiết, validation table, DoD).
   - `devplan_checklist.md` (D1-D9 với checkpoint prove-done từng step).
   - `Lessonlearn.md` (7 luật rút sẵn cho v1 demo).
   - `issues_history.md` (template trống, fill dần).
   - `Promt_History.md` (file này).
   - `demo_prompt.md` (stub, distill cuối v1).

---

(Tiếp tục cập nhật theo từng exchange quyết định lớn.)
