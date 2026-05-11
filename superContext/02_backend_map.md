# 02 - Backend Map

## Entry point and composition root

- `module_src/backend/RevenueModule.Api/Program.cs`
  - Adds controllers and OpenAPI.
  - Configures CORS from `CORS_ALLOWED_ORIGINS`.
  - Resolves DB via `ConnectionStrings:RevenueDb` or `DATABASE_URL`.
  - Registers `BillPreviewCalculator`.
  - Exposes `/` and `/health`.

## Data layer

- `module_src/backend/RevenueModule.Api/Data/RevenueDbContext.cs`
  - Entities: `Buyer`, `Product`, `Bill`, `BillLine`.
  - Seed data for buyers/products.
  - Critical relation:
    - `BillLine.ProductId` -> `Products.Id` with `DeleteBehavior.Restrict`.
  - Indexes/check constraints for bills and lines.

## Controllers and route map

- `BuyersController` (`/api/buyers`)
  - `GET /api/buyers`
  - `GET /api/buyers/{id}`
  - `POST /api/buyers`

- `ProductsController` (`/api/products`)
  - `GET /api/products`
  - `GET /api/products/{id}`
  - `POST /api/products`
  - `PUT /api/products/{id}`
  - `DELETE /api/products/{id}`

- `BillsController` (`/api/bills`)
  - list/export/preview/confirm/pending/complete/backdate/get-by-id endpoints.

- `ReportsController` (`/api/reports`)
  - revenue series + non-tech export endpoints.

## Product-specific backend behavior (important)

- Validation:
  - name trim + non-empty + max 160.
  - unitPriceVnd > 0.
  - priceMin > priceMax -> 400 `invalid_price_range`.
- Delete semantics:
  - explicit check bill-line references -> 409 conflict.
  - catch `DbUpdateException` -> map to same 409 for defense in depth.
- Error envelope:
  - `ApiError(code, message)` with user-facing Vietnamese messages.

## Backend test entry points

- Full test project:
  - `module_src/backend/RevenueModule.Api.Tests/RevenueModule.Api.Tests.csproj`
- Product CRUD integration:
  - `DemoProductCrudIntegrationTests.cs`

