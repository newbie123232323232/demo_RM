# Demo Dry-Run Checklist (Live)

## A) Trước khi paste prompt (7 checks)

- [ ] **Branch đúng:** đang ở `main`, sau đó tạo `demo/add-product-page-v2` từ `main` HEAD.
- [ ] **Baseline đủ:** có `module_src/` + `Planning_doc/` + `Planning_doc_demo/` trong workspace.
- [ ] **Docs đủ 6 file:** `business_requirements`, `devplan_checklist`, `issues_history`, `Lessonlearn`, `Promt_History`, `demo_prompt`.
- [ ] **Runtime sạch:** API/FE cũ đã dọn cổng nếu cần (5093/4200), sẵn sàng khởi động lại.
- [ ] **Prompt đúng bản distill-final:** copy từ `Planning_doc_demo/demo_prompt.md` (không dùng bản cũ trong chat history).
- [ ] **Shell guard:** nếu dùng PowerShell thì chaining bằng `;` (không dùng `&&`).
- [ ] **Port guard FE:** trước `ng serve`, xác nhận cổng 4200 chưa bị chiếm; nếu đã có server thì tái sử dụng.

## B) Sau mỗi step (5 checks)

- [ ] **Prove-done:** step hiện tại pass verify gate theo `devplan_checklist.md`.
- [ ] **Test đã chạy đúng scope:** tối thiểu build/test/manual smoke theo step (không skip).
- [ ] **Docs đã sync:** cập nhật checklist state + log issue/lesson/prompt-history nếu có phát sinh.
- [ ] **Drift guard:** mọi bằng chứng pass/fail đã được phản ánh ngay vào checklist (không có trạng thái done nhưng chưa tick).
- [ ] **Dependency guard:** task chưa done đã có annotation `(Pending: phụ thuộc Step Dx - <task>)`.

## C) Gate review bắt buộc

- [ ] **Sau D2:** đã chạy council-review backend, xử lý xong critical/major.
- [ ] **Sau D6:** đã chạy council-review frontend, xử lý xong critical/major.
- [ ] **Trước close:** final council-review cho docs/prompt, không còn gap tái lập.
- [ ] **Evidence D2 backend đủ 3 kênh:** script/automated + Postman Runner (`newman` nếu không GUI) + manual.

## D) Chốt buổi demo (Definition of Done)

- [ ] `dotnet test` pass.
- [ ] `npx ng build --configuration=development` pass.
- [ ] Manual smoke UC1-UC5 pass.
- [ ] Smoke `/lab/bill` + `/lab/revenue` không regression.
- [ ] Báo cáo cuối có: tests/build status, files changed, smoke checks, branch + commit hash.
- [ ] Đã tạo artifact `Planning_doc_demo/final_output_d9.md`.
- [ ] Nếu còn pending manual, đã ghi rõ dependency trong checklist (không đánh dấu done mơ hồ).

## E) Fallback nếu demo lệch kỳ vọng

- [ ] Nếu data không lên UI: check nhanh `FE up -> API up -> /health -> 1 endpoint data`.
- [ ] Nếu lọc/search không đúng: verify event binding + request params trước khi debug sâu.
- [ ] Nếu có drift tài liệu/code: ưu tiên `business_requirements` + `issues_history` + `Lessonlearn` làm source of truth, rồi sửa lại `demo_prompt` ngay.
