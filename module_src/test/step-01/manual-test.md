# Step 01 Manual Test Guide — Buyer/Product + Draft Subtotal

Muc tieu: xac nhan Step 1 (backend API master data + UI Draft co subtotal) hoat dong dung theo checklist.

## 0) Preconditions

- API dang chay: `http://localhost:5093`
- Frontend dang chay: `http://localhost:4200`
- Database migration da apply den Step1.

Lenh khoi dong nhanh (PowerShell):

- API: `cd d:\BT_intdemo1\marketify-mini-ecommerce\module_src\backend\RevenueModule.Api; dotnet run --launch-profile http`
- FE: `cd d:\BT_intdemo1\marketify-mini-ecommerce\module_src\frontend; npx ng serve --host 127.0.0.1 --port 4200`

## 1) API smoke cho Step 1

1. Mo `http://localhost:5093/health` -> ky vong `status = ok`, `database = connected`.
2. Goi `GET http://localhost:5093/api/buyers`:
   - Ky vong HTTP 200.
   - Ky vong co >= 3 buyer.
   - VipPoint la so nguyen, co buyer >= 5 point.
3. Goi `GET http://localhost:5093/api/products`:
   - Ky vong HTTP 200.
   - Ky vong co >= 5 product.
   - `unitPriceVnd` la so nguyen khong am.
4. Goi `GET http://localhost:5093/api/buyers/2`:
   - Ky vong HTTP 200, dung id = 2.
5. Goi `GET http://localhost:5093/api/buyers/9999`:
   - Ky vong HTTP 404.

## 2) Manual UI test tren /lab/bill

1. Mo `http://localhost:4200/lab/bill`.
2. Chua chon buyer, thu bam "Them vao Draft":
   - Ky vong thong bao yeu cau chon buyer.
3. Chon buyer co `VipPoint = 7` (hoac buyer >= 5):
   - Ky vong hien dung VipPoint kha dung.
4. Chon 1 product + qty = 2, bam "Them vao Draft":
   - Ky vong co 1 dong trong bang Draft.
   - Ky vong thanh tien dong = don gia * 2.
5. Them lai cung product voi qty = 3:
   - Ky vong khong tao dong moi, dong cu tang qty thanh 5.
6. Kiem tra Subtotal:
   - Ky vong Subtotal = tong thanh tien tat ca dong.
7. Doi buyer:
   - Ky vong Draft bi reset rong.

## 3) Definition of done Step 1

- API buyers/products va buyer detail chay on dinh.
- UI /lab/bill load du lieu that, them draft duoc, gop dong duoc, subtotal dung.
- Doi buyer reset draft dung hanh vi da chot.

## 4) Neu fail

- Kiem tra API health truoc.
- Kiem tra `environment.ts` dang tro dung `http://localhost:5093`.
- Kiem tra migration da update DB (`dotnet ef database update`).
- Kiem tra process chiem cong 5093/4200 va restart theo `Planning_doc/Lessonlearn.md`.
