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
