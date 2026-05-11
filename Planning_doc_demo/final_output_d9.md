# Final Output D9 — Demo Add Product (Admin)

## 1) Tests/Build status

- Backend tests: `dotnet test module_src/backend/RevenueModule.Api.Tests/RevenueModule.Api.Tests.csproj` => **Passed 24/24**.
- Frontend build: `npx ng build --configuration=development` => **Pass**.
- Lints (products page files) => **No linter errors**.
- Postman runner (kênh 2, qua `newman`) => **6 requests, 10 assertions, 0 failed**.

## 2) File chính đã sửa/tạo

- Backend API:
  - `module_src/backend/RevenueModule.Api/Controllers/ProductsController.cs`
  - `module_src/backend/RevenueModule.Api/Data/RevenueDbContext.cs`
  - `module_src/backend/RevenueModule.Api/Data/Migrations/20260506075311_DemoProductFkOnBillLine.cs`
  - `module_src/backend/RevenueModule.Api/Data/Migrations/20260506075311_DemoProductFkOnBillLine.Designer.cs`
  - `module_src/backend/RevenueModule.Api/Data/Migrations/RevenueDbContextModelSnapshot.cs`
- Backend tests:
  - `module_src/backend/RevenueModule.Api.Tests/DemoProductCrudIntegrationTests.cs`
- Backend smoke/tooling:
  - `module_src/backend/api-tests/README.md`
  - `module_src/backend/api-tests/step-d2-products-smoke.ps1`
  - `module_src/backend/api-tests/step-d2-products-smoke.http`
  - `module_src/backend/api-tests/postman/RevenueModule.postman_collection.json`
- Frontend products page:
  - `module_src/frontend/src/app/pages/products/products.component.ts`
  - `module_src/frontend/src/app/pages/products/products.component.html`
  - `module_src/frontend/src/app/pages/products/products.component.css`
  - `module_src/frontend/src/app/app.routes.ts`
  - `module_src/frontend/src/app/app.component.html`
  - `module_src/frontend/src/app/app.component.spec.ts`
- Docs/runbook:
  - `Planning_doc_demo/devplan_checklist.md`
  - `Planning_doc_demo/business_requirements.md`
  - `Planning_doc_demo/issues_history.md`
  - `Planning_doc_demo/Lessonlearn.md`
  - `Planning_doc_demo/Promt_History.md`
  - `Planning_doc/Lessonlearn.md`

## 3) Smoke checklist đã verify

- API `/health` trả `status=ok`.
- `GET /api/products`:
  - no query => mảng phẳng.
  - có query => object phân trang.
- `GET /api/products?priceMin=100&priceMax=50` => `400 invalid_price_range`.
- `POST /api/products` => `201` + `Location`.
- `DELETE /api/products/{id}` => `204`; GET lại id đó => `404`.
- Cross-consumer HTTP smoke route:
  - `/lab/products` => `200`
  - `/lab/bill` => `200`
  - `/lab/revenue` => `200`

## 4) Branch + commit hash

- Branch hiện tại: `src-demo-test-2`
- HEAD hiện tại: `da3cc80`

## 5) Pending gate (để minh bạch trạng thái)

- Manual smoke UC1-UC5 trên browser/session manual: **chưa có chứng từ trong phiên này**.
- Manual regression thao tác thực tế `/lab/bill` + `/lab/revenue`: **chưa có chứng từ tay**.
- Do đó D7 manual checkpoints vẫn giữ `[ ]` theo dependency rule.
