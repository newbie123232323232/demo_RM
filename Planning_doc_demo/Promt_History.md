# Promt History — Demo "Add Product (Admin)"

> Bản này đã được **sanitize** trước khi đưa vào nhánh nguyên liệu demo.
> Chỉ giữ mốc quyết định cấp cao và nhãn loại lỗi tổng quát. Toàn bộ prompt chi tiết, file path, tên hàm, lệnh chạy và mô tả fix cụ thể đã bị loại bỏ có chủ đích để tránh "cheat" cho phiên bản demo v2.
> Khi chạy v2, nội dung mới sẽ được ghi tăng dần theo timeline thực tế của buổi demo.

---

## Mốc cấp cao (high-level milestones, đã được redact)

1. **Khởi tạo demo & chốt phạm vi.** Thống nhất chủ đề "làm việc với trợ lý AI trong coding"; chọn module hiện tại; thêm 1 page admin Add Product để dùng làm bài demo. Chốt sẽ làm v1 trước (private), undo, rồi diễn lại v2 theo prompt chuẩn hoá.

2. **Chiến lược nhánh & tài liệu.** Quyết định tách nhánh implementation khỏi nhánh tài liệu; chỉ folder context được đưa lên môi trường demo; baseline phải là code chưa có Add Product.

3. **Lập kế hoạch v1.** Sinh `business_requirements.md` + `devplan_checklist.md` chi tiết từ phạm vi đã chốt; dựng sườn `Lessonlearn.md`, `issues_history.md`, `Promt_History.md`, `demo_prompt.md`.

4. **Triển khai v1 & các vòng review.** Thực hiện theo step plan; trải qua các vòng review nội bộ với phát hiện được phân loại tổng quát (không liệt kê fix cụ thể):
   - Backend: 1 nhóm rủi ro toàn vẹn dữ liệu, 1 nhóm thông điệp lỗi/contract drift, 1 nhóm thiếu test ranh giới.
   - Frontend: 1 nhóm a11y modal, 1 nhóm trạng thái khi submit, 1 nhóm UX filter.
   - Tổng quát: 1 nhóm scope drift về test, 1 nhóm đồng bộ tài liệu.

5. **Verification cuối v1.** Đầy đủ test backend, build frontend, smoke runtime trên các use case của `business_requirements.md`, smoke cross-consumer trên các page hiện hữu để loại trừ regression.

6. **Distill prompt cho v2.** Rút gọn `demo_prompt.md` thành phiên bản duy nhất sẵn dùng, kèm `demo_dry_run_checklist.md` để vận hành buổi demo. Áp các luật: Plan Mode bắt đầu, context-check bắt buộc, regenerate checklist từ business requirements, no-cheat (cấm copy checklist v1), hard-stop nếu step chưa pass verify.

7. **Hardening & failure-mode.** Bổ sung guardrail: file-lock recovery, runtime-first triage khi UI lỗi data, cross-consumer smoke khi đổi contract dùng chung, deterministic test discipline, mirror policy cho tài liệu kế thừa.

8. **Sanitize cho demo.** Đưa `Planning_doc_demo/` lên nhánh nguyên liệu; redact `devplan_checklist.md` về scaffold trống và `Promt_History.md` về bản tóm tắt cấp cao này để không leak đáp án v1.

---

## Quy ước log mới khi chạy v2

- Mỗi exchange có quyết định lớn → thêm 1 entry mới phía dưới mốc 8.
- Format đề xuất: `N. <ngày-giờ> — <tóm tắt 1 câu>` + 2-4 bullet nội dung. Không dán prompt nguyên văn dài; ưu tiên ý.
- Nếu phát sinh fix kỹ thuật cụ thể → log vào `issues_history.md`, không log path/file ở đây.
- Nếu phát sinh quy ước có thể tái dùng → log vào `Lessonlearn.md`.

---

(Phần dưới đây để trống cho timeline v2.)

9. **2026-05-06 17:25 — Chốt cách vận hành prompt trong buổi chạy v2.**
   - Quyết định dùng một prompt chuẩn duy nhất từ `demo_prompt.md`, sau đó dùng `demo_dry_run_checklist.md` làm gate sau mỗi step.
   - Không dùng cách dán lại từng prompt cũ theo timeline vì dễ tạo drift và tăng nguy cơ lộ đáp án theo lối replay.

10. **2026-05-06 17:28 — Regenerate checklist thực thi từ BR (no-cheat).**
   - `devplan_checklist.md` được tái tạo từ `business_requirements.md` theo step D1..D9.
   - Checklist mới giữ đúng contract bắt buộc (`GET /api/products` no-param => array; có param => paged object).

11. **2026-05-06 17:33 — Bổ sung chế độ vận hành theo trigger chat.**
   - Thêm quy ước `"bắt đầu test"` => agent làm việc theo chế độ mù context trong phạm vi workspace hiện tại.
   - Thêm quy ước `"kết thúc test"` => quay lại chế độ làm việc bình thường.

12. **2026-05-06 — Gate D2: council backend replay + D4 kiểm tra khi không mở browser.**
   - Council backend: không còn critical; bổ sung chứng cứ contract 201 qua assert header `Location` trong test happy-path.
   - D4: xác nhận dev server phản hồi HTTP cho route `/lab/products` và ghi smoke thay thế vào checklist/issues khi không smoke tay được.

13. **2026-05-07 — Chốt ba kênh test backend song song; FE unit test không bắt buộc trong luật này.**
   - Lessonlearn §19: kênh script/`dotnet test`, kênh Postman collection + Runner, kênh manual; đồng bộ `api-tests/README` và mirror ngắn trong `Planning_doc/Lessonlearn.md`.
   - Bổ sung smoke script + `.http` D2 và folder Postman có Tests cho Products.

14. **2026-05-07 — Hoàn thiện D5/D6 trên `/lab/products` (implementation).**
   - Modal thêm/sửa: POST/PUT, validation blur + lỗi server assertive, focus trap đủ ba nhánh, scroll lock + return focus.
   - Modal xoá: focus nút Hủy, 409/404 giữ dialog + `aria-label` hàng; manual smoke còn pending ở D7, council FE giao D7.

15. **2026-05-07 — Siết rule cập nhật tiến độ checklist + tiếp tục D7 verify.**
   - Bổ sung rule bắt buộc update tick ngay khi có bằng chứng runtime/test/council; log issue riêng cho drift checklist.
   - Tiếp tục D7 bằng chứng tự động: lint sạch, backend test pass, smoke HTTP 200 cho `/lab/products`, `/lab/bill`, `/lab/revenue` qua dev server hiện có.

16. **2026-05-07 — Council frontend D6 replay pass sau vòng fix major.**
   - Vòng 1 nêu major về stale response và truncation số thập phân; đã fix ngay trong products component.
   - Vòng re-check theo BR/devplan không còn critical/major; checkpoint council D6 được tick.

17. **2026-05-07 — Bổ sung chứng từ kênh Postman bằng Newman.**
   - Chạy collection folder `D2 — Products (smoke + contracts)` bằng `npx newman run ... --folder ...`; 6 requests, 10 assertions, 0 failed.
   - D7/Postman checkpoint được tick; manual smoke vẫn giữ pending theo rule checklist.

18. **2026-05-07 — Chuẩn hoá dependency cho task pending trong checklist.**
   - Bổ sung rule: không chuyển step khi còn task `[ ]` nếu chưa ghi dependency rõ (`Pending: phụ thuộc Step Dx - ...`).
   - Annotate lại các task pending ở D2/D5/D6/D7 và tick các mục D9 đã có bằng chứng (council final + no critical).

19. **2026-05-07 — Chuẩn hoá format checklist và chốt output D9.**
   - Rule mới: task dùng `-`, tiêu chí/spec/validation dùng `+` để tránh nhầm “task con chưa tick”.
   - Tạo `Planning_doc_demo/final_output_d9.md` theo đúng format `demo_prompt.md` và tick complete cho D9 output/report.
