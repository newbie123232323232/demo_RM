# Step 02 Manual Test Guide — Preview Pipeline Realtime

## Preconditions
- API up at `http://localhost:5093`
- FE up at `http://localhost:4200`
- Step 01 data is available.

## API checks
1. POST `/api/bills/preview` with voucher `%` + vipPoints >= 5 -> HTTP 200 and fields:
   - `subtotal`, `voucherThuongAmount`, `baseBeforeVip`, `vipDiscount`, `payable`, `expectedVipPointsEarnedIfCompleted`.
2. POST with `vipPointsUsed = 3` -> HTTP 400.
3. POST with `voucherType=percent` and `voucherValue > 100` -> HTTP 400.
4. POST with empty lines -> HTTP 400.

## UI checks (`/lab/bill`)
1. Chon buyer + them it nhat 1 line draft.
2. Chon voucher `%` hoặc `VND`, nhap gia tri -> panel realtime cap nhat sau debounce.
3. Nhap `vipPointsUsed=3` -> hien loi validation inline.
4. Nhap `vipPointsUsed=0` hoac `>=5` va <= so du buyer -> preview tra ket qua hop le.

## Done criteria
- Preview realtime bám endpoint backend.
- Lỗi validation backend được hiển thị inline trên UI.
