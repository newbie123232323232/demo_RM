# 05 - Company Machine Runbook (Safe + Clean)

## Preflight

1. Ensure correct branch (usually `src-demo-test-3` for official run).
2. Confirm DB access and connection string.
3. Check ports:
   - API: 5093
   - FE: 4200

## Startup sequence

### Backend

```powershell
dotnet restore "module_src/backend/RevenueModule.Api/RevenueModule.Api.csproj"
dotnet build "module_src/backend/RevenueModule.Api/RevenueModule.Api.csproj"
dotnet run --project "module_src/backend/RevenueModule.Api/RevenueModule.Api.csproj" --launch-profile http --no-build
```

### Frontend

```powershell
cd module_src/frontend
npx ng build --configuration=development
npx ng serve --host 127.0.0.1 --port 4200
```

## Safety notes

- In PowerShell, chain with `;` (not `&&`).
- If build fails with `MSB3027` / `MSB3021`:
  - stop process locking `RevenueModule.Api.exe`,
  - then rebuild.
- Do not start duplicate `ng serve` if port 4200 is already occupied.

## Smoke checklist

- `GET http://127.0.0.1:5093/health` -> `ok`
- `http://127.0.0.1:4200/lab/products` -> 200
- `http://127.0.0.1:4200/lab/bill` -> 200
- `http://127.0.0.1:4200/lab/revenue` -> 200

## Backend testing channels

1. Script/automated:
   - `dotnet test ...RevenueModule.Api.Tests.csproj`
   - `module_src/backend/api-tests/step-*.ps1`
2. Postman runner:
   - collection + environment (or `newman`)
3. Manual:
   - `.http` or GUI walkthrough.

