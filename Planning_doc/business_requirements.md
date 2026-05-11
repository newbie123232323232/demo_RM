# Business requirements — Module doanh thu & VipPoint (admin)

Tài liệu gom toàn bộ nghiệp vụ đã thống nhất trong phiên làm việc spec. Phiên bản này phục vụ lập kế hoạch triển khai trên codebase **marketify-mini-ecommerce** (.NET + Angular + PostgreSQL).

**Triển khai module thử nghiệm (độc lập):** mã nguồn nằm trong **`module_src/`**; mọi tham chiếu/phụ thuộc vào codebase legacy ngoài `module_src` đều bị cấm (chi tiết tại **`Planning_doc/Lessonlearn.md`**).

---

## 1. Bối cảnh & phạm vi

### 1.1 Mục đích

- Module **độc lập về mặt nghiệp vụ** so với luồng e-commerce đầy đủ: tập trung **doanh thu**, **bill**, **buyer**, **VipPoint**, **báo cáo / biểu đồ**.
- **Người dùng chính:** dev / admin (admin UI), không xây luồng khách cuối.
- **Không ưu tiên:** quản lý tồn kho, thuế, phí ship, các hạng mục giá trị gia tăng ngoài tiền hàng trên bill.

### 1.2 Tech stack (đã chốt — không đổi trong tài liệu này)

- Backend: .NET (repo hiện tại dùng kiến trúc layered/onion).
- Frontend: Angular.
- CSDL: PostgreSQL.

### 1.3 Đối tượng nghiệp vụ

| Đối tượng | Mô tả |
|-----------|--------|
| **Buyer** | Khách mua; có số dư **VipPoint** kiểu **số nguyên** (`0`, `1`, `2`, … — không thập phân). |
| **Product** | Sản phẩm có giá (VND nguyên); dùng để lập dòng bill. |
| **Bill** | Đơn/bill chứa buyer, các dòng (product + số lượng), trạng thái, snapshot tiền và điểm. |

**Lưu ý repo gốc:** có entity `Customer` — có thể map hoặc đổi tên theo “buyer” trong UI/spec; nghiệp vụ dùng thuật ngữ **buyer**.

---

## 2. Tiền tệ & thời gian

- **Đơn vị tiền:** **VND nguyên** (integer); không dùng float cho tiền.
- **Timezone:** `Asia/Ho_Chi_Minh`.
- **Tuần (lọc / trục X):** bắt đầu **thứ Hai** (chuẩn ISO week).

### 2.1 PostgreSQL — môi trường dev local (đã chốt)

- **pgAdmin / server:** tên hiển thị tùy bạn (vd `MONI local`); **Host** `localhost`, **Port** `5432`, **Database** `moni`, **User** `moni`, **Password** khớp cấu hình local (vd biến `DATABASE_URL` / secret trên máy — **không** commit mật khẩu vào git).
- **Phạm vi hiện tại:** chỉ **dev & test trên Postgres local**; có thể song song chuẩn bị Docker sau, **v1 implement không bắt buộc Docker**.
- **Connection string** (Npgsql, ví dụ): `Host=localhost;Port=5432;Database=moni;Username=moni;Password=<your_password>`

---

## 3. VipPoint — tích điểm (sau CompletedOrder)

### 3.1 Cơ sở tính

- Chỉ cộng điểm khi bill ở trạng thái **Completed** (xem mục 5).
- Cơ sở quy đổi: **toàn bộ số tiền buyer phải trả** cho bill đó (**Payable**), tức **sau mọi giảm giá** (voucher thường nếu có, VipPoint, v.v.), **không** gồm thuế / ship / phụ phí ngoài phạm vi module.
- **Không** tính điểm trước khi Complete.

### 3.2 Công thức điểm nhận — **số nguyên** (đã chốt, thay thế mọi phiên bản có thập phân)

- Quy ước: **`1` VipPoint = `1.000.000` VND** quy đổi khi **tích điểm** sau Complete.
- **Chỉ dùng số nguyên không âm** cho số dư và điểm nhận; **không** lưu / hiển thị thập phân.

**Công thức implement (Payable là `long` VND):**

```text
VipPoint_earned = Payable / 1_000_000    // chia nguyên (cùng nghĩa floor với số dương)
```

**Ví dụ:**

| Payable (VND) | Điểm nhận |
|---------------|-----------|
| 2.450.000 | 2 |
| 4.499.000 | 4 |
| 9.999.000 | 9 |
| 1.000.000 | 1 |
| 999.999 | 0 |

**Lý do chốt số nguyên:** đơn giản hoá DB (`int`), UI, test và tránh tranh cãi làm tròn; trade-off là phần Payable dưới 1 triệu (sau Complete) **không** tích thêm điểm trong đơn đó.

---

## 4. VipPoint — tiêu điểm / giảm giá bill

### 4.1 Điều kiện — dùng VipPoint hay không

- **Không bắt buộc** dùng VipPoint trên mỗi bill: `p = 0` → **không** áp giảm VipPoint, **không** trừ điểm tại Pending, `B` sau voucher thường = **Payable** (nếu không có bước giảm khác).
- Khi **có** áp VipPoint (`p > 0`):
  - Số dư buyer **≥ 5** VipPoint tại thời điểm xác nhận thanh toán.
  - **≥ 5** điểm trong lần redeem (`p ≥ 5`).

### 4.2 Nền tảng tính % giảm VipPoint

- `B` (nền để tính VipPoint) = giá trị bill **sau voucher thường** (mục 4.7), **trước** khi trừ phần giảm VipPoint lần này.
- Trong UI/backend cần phân biệt rõ: **Subtotal** → **sau voucher thường** → **`B`** (trước VipPoint) → **Payable** cuối (xem snapshot mục 6).

### 4.3 Quy tắc giảm

- Gọi `p` = số điểm sử dụng (`p = 0` hoặc `p ≥ 5` — không có khoảng `1…4`), `B` = giá trị bill **được áp VipPoint** (bill sau các KM khác, một số tiền VND).
- **Nếu `p = 0`:** không áp VipPoint; `D = 0`; **Payable** = `B` (sau khi làm tròn VND nếu có bước trung gian).
- **Nếu `p ≥ 5`:**
  - Giảm theo %: mỗi điểm = **1%** của `B`: `D_percent = (p / 100) * B`.
  - **Trần giảm tuyệt đối:** `D_cap = 0,2 * p * 1_000_000` (20% × “nominal” `p` triệu).
  - **Số tiền giảm thực tế:** `D = min(D_percent, D_cap)` (**floor** `D` về VND nguyên trước khi trừ — mục 11).
  - **Payable sau VipPoint** = `B - D` (VND nguyên).

### 4.4 Gói voucher

- **Không** có voucher gắn theo từng sản phẩm; voucher giảm từ **VipPoint** gắn với **buyer** / bill.
- Có thể lưu snapshot mã / metadata voucher sau khi áp để đối soát.

### 4.5 Thời điểm trừ / cộng điểm

- **Trừ** VipPoint đã dùng: khi chuyển sang **Pending**, **chỉ khi** `p > 0` (số điểm đã snapshot trên bill).
- **Cộng** điểm tích lũy: chỉ khi **Completed** (sau bước Complete), theo mục 3.
- API **idempotent:** Complete trên bill đã **Completed** **không** được cộng/trừ điểm lần hai.
- **Mã HTTP khi gọi Complete lặp (đã chốt):** **`409 Conflict`**. **Lý do:** theo semantics HTTP, client gửi yêu cầu **hợp lệ** nhưng **không thể áp** vì **trạng thái tài nguyên** (bill) không cho phép (đã hoàn tất) — khác với `400` (sai cú pháp/validation thường) hay `404` (không có bill). Body lỗi nên có `message` (và tùy chọn `code`, vd `bill_already_completed`) để FE/Postman đọc.

### 4.6 Chính sách hủy (đã thống nhất trong spec)

- Sau khi voucher/VipPoint đã áp vào đơn, nếu buyer hủy đơn **sau khi xác nhận đặt hàng** — **không hoàn trả** voucher/điểm theo policy đã bàn (chi tiết luồng hủy có thể mở rộng sau; module admin có thể giới hạn số trạng thái).

### 4.7 Voucher thường (không qua VipPoint) — đã chốt

- Ngoài giảm từ **VipPoint**, bill có thể có **một khoản voucher thường** do admin chọn trên UI, dạng:
  - **Giảm theo %** trên **Subtotal** (sau gộp dòng), hoặc
  - **Giảm số tiền cố định** `n` VND (VND nguyên).
- **Thứ tự áp dụng (bắt buộc):** `Subtotal` → áp **voucher thường** → ra số tiền trung gian → trên đó áp **VipPoint** (mục 4.3) → **Payable** cuối.
- Nếu giảm cố định `n` **lớn hơn** số tiền còn lại sau bước trước, **chốt theo dev:** giảm tối đa bằng số tiền đó (Payable không âm; không vượt Subtotal sau bước trước).
- **Phân bổ doanh thu theo dòng (mục 7)** không phụ thuộc vào voucher là % hay tiền: luôn dùng **Payable** và **Subtotal** thực tế sau khi đã tính xong mọi giảm; `r = (Subtotal - Payable) / Subtotal` vẫn là một công thức thống nhất.

---

## 5. Trạng thái bill & luồng admin

### 5.1 Draft — bill mềm (đã chốt)

- **Draft** bắt đầu **ngay khi admin chọn buyer**: đây là trạng thái **chỉ để hiển thị và thao tác trên UI**, đóng vai trò tương đương **giỏ / cart**, **không** bắt buộc có bản ghi trong DB.
- **Lần đầu lưu vào DB** = khi admin nhấn **xác nhận thanh toán**; bản ghi được tạo (hoặc cập nhật) ở trạng thái **Pending** với snapshot đầy đủ (mục 6).
- **Đổi buyer trong lúc Draft (đã chốt):** **reset toàn bộ** bill mềm — xóa dòng, đặt lại voucher thường và `p` về mặc định (không dùng điểm), để tránh gán sản phẩm/ưu đãi sai buyer.

### 5.2 Pending & Completed (đã chốt)

- **Pending:** đã **xác nhận thanh toán**; **đã trừ** VipPoint dùng (nếu có); có bản ghi DB.
- **Completed:** hoàn tất; **đã cộng** điểm tích lũy; **khóa** nghiệp vụ.
- **Bổ sung vận hành đã chốt:** bill Pending phải có đường **resume/cập nhật** ngay trên UI, không yêu cầu can thiệp DB tay.
  - API hỗ trợ: `PUT /api/bills/{id}/pending`.
  - Chỉ cho cập nhật khi bill còn `Pending`.
  - Không đổi `buyerId` của bill đã xác nhận; nếu cần buyer khác thì tạo bill mới.
  - Khi cập nhật Pending, hệ thống hoàn điểm đã trừ trước đó rồi trừ lại theo cấu hình mới (để số dư nhất quán).

### 5.3 UI: trạng thái và nút (đã chốt)

- Hiển thị **hai thành phần kề nhau:** **trạng thái bill** và **nút hành động**.
- **Sau khi nhấn xác nhận thanh toán:** `status = Pending`; nút **Complete** **enabled**.
- **Sau khi nhấn Complete:** `status = Completed`; nút **Complete** **disabled** ngay trên UI; **backend** từ chối Complete lặp (idempotent / lỗi rõ ràng).

### 5.4 Luồng UI “buyer fake data” / lập bill (không qua cart riêng)

1. Chọn **buyer** → mở **Draft** (bill mềm) → hiển thị **VipPoint khả dụng**.
2. Chọn **product** + **số lượng** → thêm **trực tiếp** vào bill Draft (hành vi như cart nhưng trong cùng màn hình bill).
3. **Gộp dòng:** cùng một product → **một dòng**, cộng dồn số lượng.
4. (Tùy UI) Chọn **voucher thường**: % hoặc giảm `n` VND — áp trước VipPoint theo mục 4.7.
5. Realtime (hoặc tính theo sự kiện): số điểm VipPoint dùng → % giảm → **trần giảm** → **Payable** → **dự kiến VipPoint nhận sau Completed** (mục 3).
6. **Xác nhận thanh toán** → validate mục **5.5** → tạo/ghi DB → **Pending**; nút **Complete** bật.
7. Nếu thoát màn hình khi còn Pending: vào danh sách bill Pending trên `/lab/bill`, bấm **tiếp tục** để nạp lại dữ liệu bill.
8. Có thể **cập nhật Pending** (line/voucher/VipPoint) rồi mới Complete.
9. **Complete** → **Completed**; khóa nút và server.

### 5.5 Điều kiện xác nhận thanh toán (đã chốt)

- Bill có **ít nhất một dòng** sản phẩm (sau gộp dòng).
- **Subtotal > 0**, **Payable ≥ 0** (VND nguyên).
- Nếu có voucher **%**: `0 ≤ % ≤ 100`.
- Nếu có voucher **số tiền** `n`: `n ≥ 0`; áp dụng cap theo mục 4.7.
- Nếu `p > 0`: thỏa mục 4.1 (`p ≥ 5`, đủ số dư). **`p ∈ {1,2,3,4}` không hợp lệ.**

---

## 6. Snapshot bill (khuyến nghị lưu để báo cáo & biểu đồ thống nhất)

Các trường tối thiểu (tên có thể map DB khác):

- Buyer id.
- **Subtotal** = `Σ (đơn_giá × SL)` sau gộp dòng (VND).
- Các khoản giảm (tách hoặc gộp — tùy implement): **voucher thường** (loại % / số tiền + giá trị áp dụng), **VipPoint** (điểm dùng + tiền giảm).
- **Payable** = số tiền buyer phải trả cuối cùng (VND).
- **VipPoint_used**, **VipPoint_earned** (earned chỉ có ý nghĩa sau Completed hoặc lưu 0 rồi cập nhật).
- Trạng thái; **mốc thời gian (lưu UTC, hiển thị +7):**
  - **`PendingAt`:** thời điểm xác nhận thanh toán (ghi DB lần đầu).
  - **`CompletedAt`:** thời điểm chuyển Completed (null nếu chưa Complete).
- Dòng bill: `productId`, `qty`, **`unitPrice`** = **giá snapshot tại thời điểm thêm dòng vào Draft** (hoặc tại xác nhận thanh toán nếu implement chỉ snapshot một lần — **chốt:** giữ nguyên giá đã snapshot trên dòng khi Pending/Completed, không đổi theo bảng giá product sau này).
- Và/hoặc **allocated revenue** sau phân bổ (mục 7), có thể tính lưu tại Complete hoặc truy vấn từ snapshot.

---

## 7. Phân bổ doanh thu theo dòng sản phẩm (cho biểu đồ & báo cáo)

### 7.1 Tỷ lệ hiệu dụng (một công thức cho mọi case, kể cả VipPoint chạm trần 20%)

```text
r = (Subtotal - Payable) / Subtotal   // nếu Subtotal > 0
```

(tương đương `Payable / Subtotal` khi gán `alloc[i] = lineGross[i] * Payable / Subtotal` với chỉnh remainder).

- **Không** cần nhánh logic riêng cho “case đặc biệt” trần VipPoint: `r` luôn phản ánh đúng tổng giảm thực tế.

### 7.2 Gán tiền cho từng dòng

- `lineGross[i] = đơn_giá × SL` (VND nguyên).
- Sau phân bổ: `Σ allocated[i] = Payable`.

### 7.3 Remainder (đã chốt)

- Dùng **phương pháp phần dư lớn nhất (largest remainder)**:
  1. `raw[i] = lineGross[i] * Payable`; `alloc[i] = raw[i] / Subtotal`; `rem[i] = raw[i] % Subtotal`.
  2. `diff = Payable - Σ alloc[i]`.
  3. Sắp xếp `i` theo `rem[i]` giảm dần; tie-break thứ tự cố định (vd thứ tự dòng).
  4. Cộng `1` VND cho `diff` dòng đầu trong danh sách đó.

`Subtotal = 0`: không chia; xử lý lỗi hoặc toàn 0 theo dev.

### 7.4 Biểu đồ theo **một sản phẩm**

- Doanh thu theo thời gian = tổng **allocated** của sản phẩm đó trên các bill trong bucket thời gian (filter như lịch sử).

### 7.5 Biểu đồ **buyer A + sản phẩm abc**

- Cùng công thức allocated; chỉ lọc bill thuộc buyer A và có sản phẩm abc (và có thể yêu cầu có dòng abc — theo định nghĩa lọc bill).

---

## 8. Lịch sử bill — bộ lọc & hành vi

- Lọc theo: **user/buyer**, **ngày / tuần / tháng**, **tổng bill (slider)**.
- **Mốc thời gian cho danh sách (đã chốt):** bucket/lọc theo **`PendingAt`** (thời điểm bill được tạo trên DB). Khoảng **ngày / tuần / tháng** là **biên lịch** trong `Asia/Ho_Chi_Minh`, **bao gồm** cả ngày đầu và cuối kỳ (inclusive) theo quy ước dev (00:00–23:59:59.999 local hoặc half-open UTC — miễn nhất quán list, CSV, export).
- Hiển thị bill **Pending** và **Completed** nếu thỏa filter (trừ khi UI có toggle “chỉ Completed” — mặc định **hiện cả hai**).
- **Slider giá:** `0`–`100` triệu VND; hiển thị bill có **Payable ≤** ngưỡng kéo (ví dụ kéo 50M → `Payable ≤ 50.000.000`).
- **Product (multi-select):** bill phải **chứa đủ** tất cả sản phẩm đã tick (**AND** trên tập SKU có trong bill).
- **Kết hợp filter:** mọi điều kiện áp **đồng thời** (AND logic).
- **Empty state** khi không có bill thỏa filter (đặc biệt AND product).

---

## 9. Biểu đồ doanh thu

- **Phạm vi bill (đã chốt):** chỉ tính bill trạng thái **Completed** (doanh thu đã “khóa”).
- **Mốc thời gian trục X / bucket (đã chốt):** theo **`CompletedAt`** trong `Asia/Ho_Chi_Minh` (cùng quy ước biên kỳ inclusive như mục 8).
- Trục **Y:** luôn là **tiền (doanh thu)** — tổng **Payable** (hoặc allocated theo product/buyer như mục 7).
- Trục **X:** thời gian (day / week / month; cùng timezone & tuần thứ Hai).
- **Trục X đủ lịch trong khoảng lọc (đã chốt):** Khi người dùng chọn **cả** `from` **và** `to`, API/series phải trả **mọi** bucket lịch (ngày / tuần / tháng) từ đầu đến cuối kỳ **inclusive** theo `Asia/Ho_Chi_Minh` và quy ước tuần (thứ Hai). Bucket trong kỳ **không** có bill Completed tương ứng = **0 VND** (“không bán” = 0), **không** được bỏ qua ngày/tuần/tháng giữa hai điểm có dữ liệu (tránh đường nối gây hiểu nhầm xu hướng). Khi **thiếu** một trong hai biên `from`/`to`, series có thể **thưa** (chỉ bucket có dữ liệu) để tương thích gọi API không giới hạn kỳ. Nếu `from` > `to` (theo ngày lịch), API trả lỗi **400** `invalid_date_range`.
- **Lọc thời gian `from`/`to` trên báo cáo (CompletedAt):** tham số kiểu ngày (Y-M-D) được hiểu là **ngày dương lịch tại `Asia/Ho_Chi_Minh`**; backend chuyển sang cặp mốc UTC hợp lệ cho PostgreSQL (`timestamptz`): từ **00:00** ngày `from` đến **trước** 00:00 ngày **kế sau** ngày `to` (tương đương inclusive cả ngày `to` theo địa phương).
- **Khoảng mặc định trên UI (đã chốt):** Khi **cả hai** ô `from`/`to` của biểu đồ để trống, client gửi khoảng **10** kỳ gần nhất theo lịch `Asia/Ho_Chi_Minh` và đúng **bucket** đang chọn: **ngày** (10 ngày kết thúc “hôm nay” VN), **tuần** (10 tuần bắt đầu thứ Hai, kết thúc tại ngày “hôm nay” VN), **tháng** (10 tháng dương lịch từ ngày 01 của tháng đầu kỳ đến **cuối tháng hiện tại** VN). Trong khoảng đó series vẫn **đủ lịch** và 0 VND cho bucket trống như mục trên.
- **Zoom biểu đồ (đã chốt):** Giữ **Ctrl** và lăn chuột — lăn **lên** phóng to (neo theo vị trí con trỏ), lăn **xuống** thu nhỏ. **Không** thu nhỏ rộng hơn phạm vi dữ liệu đã tải cho lần render hiện tại (độ rộng tối đa = toàn bộ điểm trong response). Phóng to có **sàn** hiển thị tối thiểu **3** bucket (không phóng vô hạn).
- **Ba kiểu biểu đồ đường (chọn ở UI):**
  1. **Tổng doanh thu / time** (mặc định có thể: tổng / tuần).
  2. **Doanh thu từ buyer A / time** (chọn buyer).
  3. **Doanh thu từ sản phẩm abc / time** (chọn một product; dùng allocated — mục 7).
- **Kết hợp:** buyer A + product abc → doanh thu (tiền) / time với cùng công thức allocated.
- Bộ lọc thời gian / buyer / product **đồng bộ khái niệm** với lịch sử bill (team có thể dùng chung query param).
- **Xuất biểu đồ:** ảnh theo trạng thái lọc hiện tại.
- **Trục X khi ít điểm dữ liệu:** vẫn đọc được (không co cụm vô nghĩa).

---

## 10. Các chức năng bổ sung đã chốt

- **Export CSV** lịch sử bill theo filter hiện tại (cùng tập bill với danh sách đang xem).
- **Cột CSV tối thiểu (đã chốt):** `BillId`, `BuyerId` (hoặc mã/tên hiển thị), `Status`, `PendingAt`, `CompletedAt` (nullable), `Subtotal`, `VoucherThuong` (tiền giảm hoặc mô tả ngắn), `VipPoint_TienGiam`, `Payable`, `VipPoint_Dung`, `VipPoint_Nhan` (nullable đến khi Complete).
- **Bảng / tooltip:** tiền hàng (Subtotal), **giảm voucher thường** (nếu có), giảm VipPoint, tiền phải trả (Payable), điểm dùng, điểm nhận (khi đã Complete).
- **Hiển thị VipPoint:** **số nguyên** (đồng bộ mục 3.2).

---

## 11. Quy ước kỹ thuật bổ sung (đã chốt)

- **Một Draft đồng thời:** một phiên làm việc admin = **một** bill mềm tại một thời điểm (một buyer + một tập dòng); không yêu cầu multi-draft song song trong v1.
- **Đóng / hủy Draft:** đóng tab hoặc rời trang **không** ảnh hưởng DB; chỉ mất state client.
- **Làm tròn `D` (VipPoint) và Payable:** luôn về **VND nguyên**; **làm tròn xuống (floor)** cho số tiền giảm `D` khi cần chuyển từ tỷ lệ — **chốt** để khớp preview và snapshot (tránh làm tròn lên làm trừ quá số dư).
- **HTTP khi Complete lặp:** **409 Conflict** (chi tiết mục 4.5).

---

## 12. Phiên bản tài liệu

- **Nguồn:** hội thoại spec founder + Draft & voucher thường + pre-build + **chốt bổ sung:** VipPoint **số nguyên** (mục 3.2), Postgres dev local `moni` (mục 2.1), Complete lặp **409** (mục 4.5).
- Cập nhật khi thay đổi nghiệp vụ.

---

## 13. Kế hoạch hardening trước makeup UI/UX (đợt riêng)

### 13.1 Mục tiêu

- Chốt lại tính nhất quán filter thời gian giữa **Bill History / CSV / Chart / XLSX** trước khi làm lớp UI polish.
- Loại bỏ lỗi runtime 500 ở query ngày kiểu `YYYY-MM-DD` và khóa semantics về lịch `Asia/Ho_Chi_Minh`.
- Hoàn thiện UX product picker hiện tại theo mức “dùng ổn định” (không chỉ đẹp).

### 13.2 Phạm vi bắt buộc

- **Backend Bill History (`GET /api/bills`, `GET /api/bills/export.csv`):**
  - Chuẩn hóa `from`/`to` theo **ngày lịch VN** như report: `from` = đầu ngày local, `to` = trước đầu ngày kế.
  - Không dùng so sánh trực tiếp `DateTime` raw `Unspecified` vào cột `timestamptz`.
  - Nếu `from > to` theo ngày lịch: trả **400** `invalid_date_range`.
- **Test tự động:**
  - Thêm integration test cho list/csv với `from`/`to` date-only để chặn hồi quy 500.
  - Giữ invariant: cùng filter thì chart/list/csv/xlsx không lệch semantics thời gian.
- **Frontend UX tối thiểu (không makeup):**
  - Product picker modal hỗ trợ đóng bằng **Esc**, focus vào modal khi mở.
  - Event áp dụng filter vẫn chỉ chạy khi bấm **Xác nhận**.

### 13.3 Tiêu chí hoàn tất

- Không còn lỗi 500 cho list/csv khi truyền `from`/`to` kiểu ngày.
- Bộ test backend pass; build frontend pass.
- Manual smoke: cùng filter ngày cho list/csv/chart/xlsx không có lệch bất thường về tập dữ liệu theo mốc thời gian.
