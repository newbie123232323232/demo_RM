# 03 - Frontend Map

## App shell and routing

- Shell/nav: `module_src/frontend/src/app/app.component.html`
- Routes: `module_src/frontend/src/app/app.routes.ts`
  - `/lab/bill` -> fake bill page
  - `/lab/products` -> products page
  - `/lab/revenue` -> revenue page

## Products page files

- `module_src/frontend/src/app/pages/products/products.component.ts`
- `module_src/frontend/src/app/pages/products/products.component.html`
- `module_src/frontend/src/app/pages/products/products.component.css`

## Products page behavior summary

- List + filter + pagination.
- Add/Edit modal with validation and server error mapping.
- Delete confirm modal with 409/404 handling.
- A11y patterns:
  - dialog roles/labels,
  - focus trap,
  - return focus to opener,
  - aria-live on errors.

## Two runtime pitfalls already encountered (must remember)

1. Search not truly realtime when binding logic drifts:
   - current intended behavior is input-driven realtime search with debounce.

2. Min/max price transient typing causing generic list-load error:
   - current intended behavior is client-side `min <= max` guard with explicit filter error.

## Why old pages matter

`/lab/bill` and `/lab/revenue` still consume products list.
Any API shape drift in products can break them even if `/lab/products` looks fine.

