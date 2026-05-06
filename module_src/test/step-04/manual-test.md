# Step 04 Manual Test Guide — Revenue List, Filter, CSV

## Preconditions
- API: http://localhost:5093
- UI: http://localhost:4200/lab/revenue
- Da co bill tu Step 3.

## UI test
1. Mo /lab/revenue, thay danh sach bill.
2. Loc theo status Pending/Completed.
3. Chon buyer, doi payable slider, kiem tra so dong thay doi.
4. Chon 2 products (AND), ket qua chi gom bill co du ca 2 product.
5. Bam `Tai CSV`, mo file va kiem tra co header + du lieu.

## API checks
- GET /api/bills
- GET /api/bills?status=completed&payableMaxVnd=100000000&productIds=1
- GET /api/bills/export.csv
