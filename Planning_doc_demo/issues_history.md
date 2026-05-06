# Issues History — Demo "Add Product (Admin)"

> Log các vấn đề thực tế gặp trong quá trình build v1. Mỗi entry theo template:
>
> ```text
> ### [Ngày] [Tên issue]
> - Issue:
> - Tác động:
> - Đã xử lý:
> - Quy ước tiếp theo:
> ```

---

### 2026-05-06 — Council review backend D2: phát hiện FK gap + 4 fix

- **Issue:** Adversarial review sau Step D2 phát hiện `BillLine.ProductId` không có FK constraint ở DB schema → race giữa `Any()` check và `SaveChanges()` có thể tạo orphan row, và DB không bảo vệ. Ngoài ra: 409 message tiếng Anh không khớp BR, thiếu validate `priceMin > priceMax`, và missing tests cho `name=null`, `pageSize > 100`, `priceMin > priceMax`.
- **Tác động:** Race condition tuy hiếm trong demo single-admin nhưng là schema gap thật sự + UX message lệch BR + test coverage có lỗ hổng.
- **Đã xử lý:**
  1. Thêm migration `DemoProductFkOnBillLine` AddForeignKey `BillLines.ProductId → Products.Id ON DELETE RESTRICT`. Áp dụng vào DB local.
  2. Update `RevenueDbContext.OnModelCreating` cho `BillLine` thêm `HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict)`.
  3. Đổi 409 message sang tiếng Việt khớp BR. Bọc `Products.Remove + SaveChangesAsync` trong `try/catch (DbUpdateException)` → map về cùng 409 contract (defense-in-depth).
  4. Thêm validate `priceMin > priceMax → 400 invalid_price_range` trên `GET /api/products` (đối xứng với BillsController `from > to`).
  5. Thêm 3 test mới: `CreateProduct_NullName_Returns400`, `GetProductsList_PriceMinGreaterThanPriceMax_Returns400`, `GetProductsList_PageSizeOver100_ClampsTo100`.
- **Quy ước tiếp theo:**
  - Mọi entity có quan hệ tham chiếu ở dữ liệu khác phải có cả application check + DB-level FK ngay từ đầu, không "để sau".
  - Mọi filter range trên controller mới phải có 400 validation đối xứng với pattern cũ — không silent empty.
  - Council review backend phải verify được cả schema lẫn controller, không chỉ controller surface.

### 2026-05-06 — Council review frontend D6: focus trap incomplete + a11y gaps

- **Issue:** Adversarial review sau Step D6 phát hiện 1 critical + 6 major:
  - Forward Tab từ dialog shell (`tabindex="-1"`) không bị bắt → focus thoát modal.
  - Inputs text/number không disabled trong `formSubmitting` → user có thể edit khi request đang fly.
  - Inline error spans không có `role="alert"`/`aria-live` → screen reader im lặng khi validation/server fail.
  - Row action button "Sửa"/"Xoá" không kèm tên product cho AT.
  - Delete dialog thiếu `aria-describedby` trỏ tới body có tên product.
  - Initial focus trên dialog shell thay vì button Hủy (destructive: nguy cơ Enter vô tình confirm).
  - Nested `setTimeout(0) + setTimeout(30)` racy.
  - Price filter không debounce (DoS API risk khi user gõ nhanh).
- **Tác động:** Demo sống được nhưng không pass a11y review thực tế; race khi network chậm.
- **Đã xử lý:**
  1. `onDialogKeydown` thêm nhánh `!event.shiftKey && active === dialog → preventDefault(); first.focus()`.
  2. Thêm `[disabled]="formSubmitting"` cho 2 input.
  3. Thêm `role="alert"` + `aria-live="polite"` cho field error, `aria-live="assertive"` cho dialog-level error + top errorMessage.
  4. Thêm `[attr.aria-label]="'Sửa sản phẩm ' + product.name"` (tương tự cho Xoá).
  5. Thêm `id="product-delete-body"` cho `<p>` body, `aria-describedby` trên delete dialog.
  6. Thêm `#deleteCancelBtn` ref, `openDeleteDialog` focus Hủy đầu tiên.
  7. Đơn giản hoá initial focus còn 1 setTimeout: target `nameInput ?? formDialog` (form), `deleteCancelBtn ?? deleteDialog` (delete).
  8. Tạo `priceFilterDebounce$` Subject debounceTime(300), `onPriceFilterChanged` chỉ `next()`.
- **Quy ước tiếp theo:**
  - Focus trap pattern phải xử lý cả 3 nhánh: Shift+Tab từ first/shell → last; Tab từ last → first; Tab từ shell → first.
  - Errors trong modal/page bắt buộc có aria-live; field error `polite`, dialog-level error `assertive`.
  - Row action button trong table luôn có `aria-label` chứa context entity.
  - Destructive confirm dialog initial focus trên Hủy, không phải nút Xoá.

### 2026-05-06 — Council review final: 4 landmine trong tài liệu demo_prompt

- **Issue:** Adversarial review final phát hiện inconsistency giữa các tài liệu demo:
  1. `devplan_checklist.md` D5-F5 vẫn ghi "> 200 ký tự" trong khi BR + v1 code dùng 160.
  2. `devplan_checklist.md` D6-F4 ghi "đóng dialog" khi 404, trong khi v1 cố ý giữ dialog mở (tránh race với user thao tác mới).
  3. `ProductsController.ValidateProductPayload` trả message English `"Request body is required."` cho null body — vi phạm rule "tất cả error message tiếng Việt".
  4. `demo_prompt.md` thiếu khẳng định baseline `main` phải chứa module gốc — nếu `main` chỉ có docs thì v2 không có gì để build trên.
- **Tác động:** Nếu v2 implementer follow tài liệu literally, sẽ drift > 5% so với v1 trên validation UX và delete error flow. Nếu main không có baseline, demo v2 chết ngay từ đầu.
- **Đã xử lý:**
  1. Sửa D5-F5 sang "> 160 ký tự (khớp DB constraint)".
  2. Sửa D6-F4: 404 giữ dialog mở, copy "Bấm 'Hủy' để đóng và làm mới danh sách", cấm setTimeout auto-close.
  3. Localize null-body message sang "Thiếu nội dung request.".
  4. Thêm "Baseline assumption" block vào header `demo_prompt.md` ghi rõ main phải chứa toàn bộ `module_src/` baseline (chỉ thiếu Add Product feature).
- **Quy ước tiếp theo:**
  - Council final review bắt buộc đọc chéo BR ↔ devplan ↔ Lessonlearn ↔ v1 code. Inconsistency nội bộ là loại bug khó phát hiện nhất nhưng phá demo nặng nhất.
  - Mọi prompt distill cho replay phải có "Baseline assumption" block khẳng định state khởi đầu.

### 2026-05-06 — `dotnet build` lock vì API process đang chạy nền

- **Issue:** Lần đầu build sau khi viết ProductsController, `dotnet build` báo `MSB3027/MSB3021` retry 10 lần fail vì `RevenueModule.Api.exe` đang bị PID 21924 lock.
- **Tác động:** Ngừng vòng iter; suýt nhầm là code lỗi.
- **Đã xử lý:** `Stop-Process -Id 21924 -Force` rồi `dotnet build` lại — pass ngay.
- **Quy ước tiếp theo:** Trước mọi `dotnet build` / `dotnet ef migrations add` / `dotnet ef database update` trong phiên có API chạy nền: kill PID listening port 5093 trước (khớp lesson cũ trong `Planning_doc/Lessonlearn.md` mục dọn cổng).

### 2026-05-06 — UX bug: filter tên không apply ngay khi gõ

- **Issue:** User manual test phát hiện tìm tên product có lúc không apply ngay (ví dụ gõ `"demo"` không lọc tức thì), chỉ thấy kết quả đúng sau hành động khác gây refresh list (như bấm trang sau/trang trước). Trong khi filter giá đã live.
- **Tác động:** Trải nghiệm lọc không nhất quán, dễ bị hiểu nhầm là API filter theo `q` không hoạt động.
- **Đã xử lý:**
  1. Trước tiên thử chuyển event text filter sang `(input)` nhưng vẫn có case timing lệch giữa event và state khi debounce.
  2. Chốt fix ổn định: dùng `(ngModelChange)="onSearchChanged($event)"` và trong component set trực tiếp `searchTerm = value` trước khi `.next()` vào debounce stream.
  3. Giữ debounce 300ms như spec, không cần blur/click-out để apply.
  4. Rebuild FE pass sau fix.
- **Quy ước tiếp theo:** Với text filter có debounce trong Angular forms, ưu tiên nhận giá trị mới trực tiếp từ `ngModelChange` payload rồi ghi vào state trước khi trigger stream; không phụ thuộc ngầm vào timing đồng bộ của two-way binding.

### 2026-05-06 — Scope test drift: thêm unit test rồi rollback theo yêu cầu demo-step

- **Issue:** Trong lúc fix UX filter tên, AI có thêm unit test frontend (spec cho `ProductsPageComponent`) để khóa hành vi debounce + live search, đồng thời chỉnh `app.component.spec.ts` cho nav link mới. Tuy nhiên user xác nhận không cần unit test, chỉ cần test theo style các step trước (build + manual smoke).
- **Tác động:** Tạo thêm thay đổi ngoài scope mong muốn của user, tăng độ ồn trong nhánh demo.
- **Đã xử lý:** Xóa file `products.component.spec.ts`, revert `app.component.spec.ts` về trạng thái cũ, giữ lại duy nhất fix logic/runtime cần thiết cho UX.
- **Quy ước tiếp theo:** Với flow demo-step hiện tại, mặc định không thêm frontend unit test mới nếu user không yêu cầu rõ; ưu tiên đúng contract test đã chốt trước đó.

---

## Mirror policy với `Planning_doc/issues_history.md` (bắt buộc từ 2026-05-06)

- `Planning_doc_demo/issues_history.md` phải **kế thừa và đồng bộ liên tục** các issue workflow hữu dụng từ `Planning_doc/issues_history.md`.
- Từ mốc này, mỗi khi:
  - có issue mới trong luồng demo (`Planning_doc_demo`) **hoặc**
  - có issue mới trong luồng module chung (`Planning_doc`),
  thì phải cân nhắc mirror sang phía còn lại nếu có giá trị tái sử dụng.
- Ưu tiên giữ lại cả issue "thừa" hoặc chuyên hóa nếu có khả năng giúp lần implement sau tránh lặp sai.
- Khi mirror, thêm ghi chú nguồn ở entry:
  - `Source: Planning_doc/issues_history.md`
  - hoặc `Source: Planning_doc_demo/issues_history.md`
- Với training người mới, mục tiêu là "memory càng đầy đủ càng tốt", không tối ưu theo tiêu chí gọn.

### Mirror batch #1 (seed cho v2 Add Product) — Source: `Planning_doc/issues_history.md`

> Các mục dưới đây được chọn vì ảnh hưởng trực tiếp tốc độ/độ ổn định khi build feature mới ở v2.

1. **Contract drift giữa backend DTO và frontend consumer**
   - Nếu đổi shape response endpoint dùng chung mà không update toàn bộ consumer, UI dễ “mất dữ liệu giả”.
   - Áp dụng cho v2: mọi thay đổi ở `GET /api/products` phải verify cả `/lab/products`, `/lab/bill`, `/lab/revenue` (ít nhất smoke nhanh).

2. **Runtime gate trước khi debug business**
   - FE lỗi data có thể chỉ là API down (`ERR_CONNECTION_REFUSED`).
   - Áp dụng cho v2: luôn kiểm tra thứ tự `FE up -> API up -> /health -> 1 endpoint data` trước khi debug filter/logic.

3. **Test phải deterministic, scope dữ liệu đủ hẹp**
   - Integration test fail giả khi dùng dataset shared/seed chung.
   - Áp dụng cho v2: test CRUD/filter Product phải tạo dữ liệu có prefix riêng (GUID tag), assert trên dataset cô lập.

4. **Provider test phải gần runtime thực**
   - Trộn InMemory và provider runtime gây nhiễu/false fail.
   - Áp dụng cho v2: giữ pattern test Postgres runtime như Step6IntegrationTests.

5. **Mọi đổi error contract phải rollout đồng bộ BE/FE**
   - Backend đổi code/message mà FE không parse đúng sẽ tạo UX mơ hồ.
   - Áp dụng cho v2: khi đổi code/message lỗi product, update luôn map lỗi frontend và smoke nhánh lỗi chính.
