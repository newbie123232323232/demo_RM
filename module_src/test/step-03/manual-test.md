# Step 03 Manual Test Guide — Confirm / Complete Flow

## Preconditions
- API up at `http://localhost:5093`
- UI up at `http://localhost:4200/lab/bill`
- Step 01 + Step 02 done.

## API checks
1. POST `/api/bills/confirm` with valid draft -> HTTP 200, `status=Pending`, co `billId`.
2. GET `/api/bills/{billId}` -> tra chi tiet bill + lines.
3. PUT `/api/bills/{billId}/pending` -> HTTP 200, van `status=Pending`, duoc cap nhat line/voucher/vip.
4. POST `/api/bills/{billId}/complete` -> HTTP 200, `status=Completed`.
5. POST complete lan 2 -> HTTP 409.

## UI checks
1. Tao draft + pricing preview.
2. Bam `Xac nhan thanh toan` -> status Pending + hien BillId.
3. O khu `Bill Pending chua Complete`, bam `Tiep tuc` de nap lai bill vao form.
4. Sua line/voucher/vip va bam `Cap nhat Pending` -> bill duoc cap nhat, van Pending.
5. Bam `Complete` -> status Completed, nut Complete bi disable.
6. Xac nhan VipPoint buyer duoc cap nhat sau complete.

## Done criteria
- Flow Draft -> Pending -> Completed chay end-to-end.
- Co the resume va cap nhat bill Pending ngay tren UI.
- Repeat complete bi chan 409.
