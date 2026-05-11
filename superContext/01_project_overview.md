# 01 - Project Overview

## What this repository is (for this demo scope)

- Main demo module lives under `module_src/`.
- Stack:
  - Backend: ASP.NET Core + EF Core + PostgreSQL.
  - Frontend: Angular standalone components.
- Demo domain: Revenue/VipPoint lab with 3 pages:
  - `/lab/bill`
  - `/lab/products`
  - `/lab/revenue`

## Primary objective of current demo track

Reproduce and present "Add Product (Admin)" flow with stable contracts and replay-friendly docs.

## Core architecture shape

- Backend app: `module_src/backend/RevenueModule.Api`
- Backend tests: `module_src/backend/RevenueModule.Api.Tests`
- Frontend app: `module_src/frontend`
- Demo docs: `Planning_doc_demo/*`

## Environments and ports

- API default: `http://localhost:5093` (profile `http`)
- Frontend default: `http://127.0.0.1:4200` (or localhost:4200)
- DB (default local): PostgreSQL `HMModule`

## Single most important contract to preserve

`GET /api/products` dual response shape:

- No query params -> returns flat array for backward compatibility.
- With query params -> returns paged object.

Breaking this is the fastest way to regress old consumers.

