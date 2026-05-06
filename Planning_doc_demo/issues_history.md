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

### 2026-05-06 — `dotnet build` lock vì API process đang chạy nền

- **Issue:** Lần đầu build sau khi viết ProductsController, `dotnet build` báo `MSB3027/MSB3021` retry 10 lần fail vì `RevenueModule.Api.exe` đang bị PID 21924 lock.
- **Tác động:** Ngừng vòng iter; suýt nhầm là code lỗi.
- **Đã xử lý:** `Stop-Process -Id 21924 -Force` rồi `dotnet build` lại — pass ngay.
- **Quy ước tiếp theo:** Trước mọi `dotnet build` / `dotnet ef migrations add` / `dotnet ef database update` trong phiên có API chạy nền: kill PID listening port 5093 trước (khớp lesson cũ trong `Planning_doc/Lessonlearn.md` mục dọn cổng).
