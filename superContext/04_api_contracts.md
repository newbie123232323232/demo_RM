# 04 - API Contracts (Demo-Critical)

## Health

- `GET /health`
  - expected status: `ok` when DB is reachable.

## Products contract

### `GET /api/products`

- No query params -> array of product items.
- With any query param (`q`, `priceMin`, `priceMax`, `page`, `pageSize`) -> paged object:
  - `items`, `page`, `pageSize`, `totalCount`, `totalPages`.

### `GET /api/products/{id}`

- 200 with product item.
- 404 with `product_not_found`.

### `POST /api/products`

- 201 with body and `Location` header.
- 400 `invalid_payload` for validation failures.

### `PUT /api/products/{id}`

- 200 with updated item.
- 404 `product_not_found`.
- 400 `invalid_payload`.

### `DELETE /api/products/{id}`

- 204 if deleted.
- 404 `product_not_found`.
- 409 `conflict_product_in_use` when referenced by bill lines.

## Validation guardrails

- Product name max length: 160.
- UnitPriceVnd must be positive integer business-wise.
- `priceMin > priceMax` must return 400 at API level.

## Error shape

- `ApiError(code, message)` for user-facing errors.

## Compatibility note

Do not collapse dual-shape list contract unless all dependent consumers are migrated.

