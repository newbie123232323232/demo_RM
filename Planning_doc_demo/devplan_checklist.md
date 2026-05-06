# Dev Plan Checklist — Demo "Add Product (Admin)"

> File này là **scaffold trung tính**. Trước khi demo, AI BẮT BUỘC regenerate toàn bộ nội dung step từ `business_requirements.md` (không copy lại từ phiên bản v1 — xem `demo_prompt.md` mục "No-cheat").

## Quy ước chung

- Mỗi step kết thúc bằng checkpoint **prove done** — chỉ tick `[x]` khi pass đủ điều kiện thực tế.
- Step có verify-gate phải chạy lệnh tương ứng và xác nhận kết quả runtime, không tick dựa trên phán đoán.
- Nếu một step có rủi ro hồi quy hoặc đụng vào dữ liệu/contract dùng chung → bắt buộc thêm cross-consumer smoke trước khi mark done.
- Khi mở rộng số step, giữ thứ tự backend trước, frontend sau, verification cuối, finalize docs sau cùng.

## Template prove-done (áp cho mọi step)

- [ ] Mã/cấu hình đã build/compile pass.
- [ ] Test (đơn vị/tích hợp) liên quan pass.
- [ ] Smoke runtime đúng acceptance trong `business_requirements.md`.
- [ ] Không regression trên consumer hiện hữu liên quan.
- [ ] Docs (`Lessonlearn.md`, `issues_history.md`) cập nhật nếu phát sinh quy ước/bài học mới.

---

## Step D1 — _(điền sau khi regenerate từ business_requirements.md)_

### Mục tiêu
- _(điền)_

### Việc cần làm
- [ ] _(điền)_

### Checkpoint prove done
- [ ] _(điền)_

---

## Step D2 — _(điền)_

### Mục tiêu
- _(điền)_

### Việc cần làm
- [ ] _(điền)_

### Checkpoint prove done
- [ ] _(điền)_

---

## Step D3 — _(điền)_

### Mục tiêu
- _(điền)_

### Việc cần làm
- [ ] _(điền)_

### Checkpoint prove done
- [ ] _(điền)_

---

## Step D4 — _(điền)_

### Mục tiêu
- _(điền)_

### Việc cần làm
- [ ] _(điền)_

### Checkpoint prove done
- [ ] _(điền)_

---

## Step D5 — _(điền)_

### Mục tiêu
- _(điền)_

### Việc cần làm
- [ ] _(điền)_

### Checkpoint prove done
- [ ] _(điền)_

---

## Step Dn — _(thêm/bớt theo phạm vi thực tế)_

### Mục tiêu
- _(điền)_

### Việc cần làm
- [ ] _(điền)_

### Checkpoint prove done
- [ ] _(điền)_

---

## Verification cuối

- [ ] Toàn bộ test backend pass.
- [ ] Toàn bộ build frontend pass.
- [ ] Manual smoke đầy đủ các use case trong `business_requirements.md`.
- [ ] Cross-consumer smoke nếu có thay đổi contract dùng chung.
- [ ] Lint sạch trên file mới/sửa.
- [ ] A11y nhanh: tab order, focus-visible, Esc, return focus.

---

## Finalize docs (làm sau verification)

- [ ] `Lessonlearn.md` (Planning_doc_demo) cập nhật quy ước rút ra.
- [ ] `issues_history.md` (Planning_doc_demo) log issue gặp + cách giải quyết.
- [ ] `Promt_History.md` (Planning_doc_demo) ghi mốc quyết định lớn.
- [ ] Council review final trước khi đóng.

---

> _Ghi chú cho người dẫn demo_: nếu thấy file này còn đầy nội dung chi tiết bước nào — đó là dấu hiệu chưa sanitize đúng. Phải reset về template trên trước khi bắt đầu live.
