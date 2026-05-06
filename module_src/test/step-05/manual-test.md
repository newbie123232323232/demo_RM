# Step 05 - Manual test (chart + export + reconcile)

## Pre-condition
- API: `http://localhost:5093`
- FE: `http://localhost:4200/lab/revenue`
- Database has at least 1 bill `Completed`.

## 1) Revenue series endpoint quick check
1. Open: `GET /api/reports/revenue-series?mode=total&bucket=day`
2. Verify response has `mode`, `bucket`, `points[]`.
3. Open: `GET /api/reports/revenue-series?mode=byProduct&bucket=week&productId=1`
4. Verify endpoint returns `200` and `points[]`.
5. With **both** `from` and `to` set (e.g. 3 calendar days where only the first day has Completed revenue), verify `points[]` has **three** buckets and the middle day has `revenueVnd: 0` (full calendar axis, not a sparse line).

## 2) UI chart filter + tooltip
1. Open `/lab/revenue`, scroll to "Biểu đồ doanh thu".
2. Change `mode` and `bucket` values.
3. With `byBuyer` or `byBuyerAndProduct`, leave buyer empty and verify UI shows validation message.
4. Hover mouse over a data point and verify tooltip appears with date + VND value.

## 3) Export image
1. Click `Xuất ảnh PNG`.
2. Verify a PNG file is downloaded and opens correctly.

## 4) Reconcile chart/list/csv
1. Keep chart in `mode=total`.
2. Click `Đối chiếu chart/list/csv`.
3. Verify green success message:
   - `chart = list = csv = <same value>`
4. Change filter (date/buyer/product), click compare again, and verify still consistent.

## 5) Non-tech XLSX export
1. Set chart filter (Buyer, Product, From/To, Bucket) you want to verify.
2. Click `Báo cáo non-tech (.xlsx)`.
3. Open the downloaded `revenue-nontech-<bucket>.xlsx` in Excel/LibreOffice.
4. Verify 4 sheets exist: `Summary`, `TimeSeries`, `ByBuyer`, `ByProduct`.
5. On `Summary`:
   - "Doanh thu (Completed)" must equal the chart total (in `mode=total`) under same filter.
   - "Số bill Completed" must match list/CSV row count under `status=completed&timeField=completed`.
6. On `TimeSeries`: bucket row count must equal `points[]` length of revenue-series for the same filter (including zero-revenue buckets when both `from` and `to` are set).
7. On `ByBuyer`: sum of "Doanh thu" column equals `Summary` total revenue.
8. On `ByProduct`: when no product filter, sum of "Doanh thu phân bổ" equals `Summary` total revenue (largest-remainder allocation preserves total).
