# 10 - src-demo-test-3 File Manifest (authoritative)

Purpose: exact tracked file structure for branch `src-demo-test-3` to support manual copy on a restricted machine.

Source of truth:
- Generated from `git ls-files` on branch `src-demo-test-3`.
- This includes tracked files only (what GitHub branch contains).

Verification rules:
1. Recreate folders/files exactly as listed.
2. Total tracked files must be exactly `111`.
3. No missing path, no extra path.

## Tracked file list (111)

```text
.gitignore
LICENSE
Planning_doc/Lessonlearn.md
Planning_doc/business_requirements.md
Planning_doc/devplan_checklist.md
Planning_doc/issues_history.md
Planning_doc_demo/Lessonlearn.md
Planning_doc_demo/Promt_History.md
Planning_doc_demo/business_requirements.md
Planning_doc_demo/demo_dry_run_checklist.md
Planning_doc_demo/demo_prompt.md
Planning_doc_demo/devplan_checklist.md
Planning_doc_demo/issues_history.md
Promt_History.md
README.md
module_src/.env.example
module_src/.gitignore
module_src/README.md
module_src/backend/README.md
module_src/backend/RevenueModule.Api.Tests/RevenueModule.Api.Tests.csproj
module_src/backend/RevenueModule.Api.Tests/Step6IntegrationTests.cs
module_src/backend/RevenueModule.Api.Tests/UnitTest1.cs
module_src/backend/RevenueModule.Api/Controllers/BillsController.cs
module_src/backend/RevenueModule.Api/Controllers/BuyersController.cs
module_src/backend/RevenueModule.Api/Controllers/ProductsController.cs
module_src/backend/RevenueModule.Api/Controllers/ReportsController.cs
module_src/backend/RevenueModule.Api/Controllers/WeatherForecastController.cs
module_src/backend/RevenueModule.Api/Data/Migrations/20260505051651_InitialRevenueSchema.Designer.cs
module_src/backend/RevenueModule.Api/Data/Migrations/20260505051651_InitialRevenueSchema.cs
module_src/backend/RevenueModule.Api/Data/Migrations/20260505053225_Step1MasterData.Designer.cs
module_src/backend/RevenueModule.Api/Data/Migrations/20260505053225_Step1MasterData.cs
module_src/backend/RevenueModule.Api/Data/Migrations/20260505053504_Step1SeedAsciiNames.Designer.cs
module_src/backend/RevenueModule.Api/Data/Migrations/20260505053504_Step1SeedAsciiNames.cs
module_src/backend/RevenueModule.Api/Data/Migrations/20260505062115_Step3BillEntities.Designer.cs
module_src/backend/RevenueModule.Api/Data/Migrations/20260505062115_Step3BillEntities.cs
module_src/backend/RevenueModule.Api/Data/Migrations/20260505064204_Step4BillFilterIndexes.Designer.cs
module_src/backend/RevenueModule.Api/Data/Migrations/20260505064204_Step4BillFilterIndexes.cs
module_src/backend/RevenueModule.Api/Data/Migrations/20260505065154_Step3PendingResumeFields.Designer.cs
module_src/backend/RevenueModule.Api/Data/Migrations/20260505065154_Step3PendingResumeFields.cs
module_src/backend/RevenueModule.Api/Data/Migrations/RevenueDbContextModelSnapshot.cs
module_src/backend/RevenueModule.Api/Data/RevenueDbContext.cs
module_src/backend/RevenueModule.Api/Models/ApiError.cs
module_src/backend/RevenueModule.Api/Models/Bill.cs
module_src/backend/RevenueModule.Api/Models/BillLine.cs
module_src/backend/RevenueModule.Api/Models/Buyer.cs
module_src/backend/RevenueModule.Api/Models/Product.cs
module_src/backend/RevenueModule.Api/Program.cs
module_src/backend/RevenueModule.Api/Properties/launchSettings.json
module_src/backend/RevenueModule.Api/RevenueModule.Api.csproj
module_src/backend/RevenueModule.Api/RevenueModule.Api.http
module_src/backend/RevenueModule.Api/Services/BillPreviewCalculator.cs
module_src/backend/RevenueModule.Api/WeatherForecast.cs
module_src/backend/RevenueModule.Api/appsettings.Development.json
module_src/backend/RevenueModule.Api/appsettings.json
module_src/backend/api-tests/README.md
module_src/backend/api-tests/postman/RevenueModule.local.postman_environment.json
module_src/backend/api-tests/postman/RevenueModule.postman_collection.json
module_src/backend/api-tests/step-00-health.http
module_src/backend/api-tests/step-00-health.ps1
module_src/frontend/.editorconfig
module_src/frontend/.gitignore
module_src/frontend/.vscode/extensions.json
module_src/frontend/.vscode/launch.json
module_src/frontend/.vscode/tasks.json
module_src/frontend/README.md
module_src/frontend/angular.json
module_src/frontend/package-lock.json
module_src/frontend/package.json
module_src/frontend/src/app/app.component.css
module_src/frontend/src/app/app.component.html
module_src/frontend/src/app/app.component.spec.ts
module_src/frontend/src/app/app.component.ts
module_src/frontend/src/app/app.config.ts
module_src/frontend/src/app/app.routes.ts
module_src/frontend/src/app/pages/fake-bill/fake-bill.component.css
module_src/frontend/src/app/pages/fake-bill/fake-bill.component.html
module_src/frontend/src/app/pages/fake-bill/fake-bill.component.ts
module_src/frontend/src/app/pages/revenue/revenue.component.css
module_src/frontend/src/app/pages/revenue/revenue.component.html
module_src/frontend/src/app/pages/revenue/revenue.component.ts
module_src/frontend/src/assets/.gitkeep
module_src/frontend/src/environments/environment.ts
module_src/frontend/src/favicon.ico
module_src/frontend/src/index.html
module_src/frontend/src/main.ts
module_src/frontend/src/styles.css
module_src/frontend/tsconfig.app.json
module_src/frontend/tsconfig.json
module_src/frontend/tsconfig.spec.json
module_src/test/RevenueModule.postman_collection.json
module_src/test/step-01/manual-test.md
module_src/test/step-01/step-01-api.ps1
module_src/test/step-02/manual-test.md
module_src/test/step-02/step-02-api.ps1
module_src/test/step-03/manual-test.md
module_src/test/step-03/step-03-api.ps1
module_src/test/step-04/manual-test.md
module_src/test/step-04/step-04-api.ps1
module_src/test/step-05/manual-test.md
module_src/test/step-05/seed-history-data.ps1
module_src/test/step-05/step-05-api.ps1
superContext/01_project_overview.md
superContext/02_backend_map.md
superContext/03_frontend_map.md
superContext/04_api_contracts.md
superContext/05_runbook_company_machine.md
superContext/06_branch_and_docs_strategy.md
superContext/07_risks_and_known_pitfalls.md
superContext/08_quick_bootstrap_prompt.md
superContext/09_raw_chat_export_note.md
superContext/README.md
```

## Optional validation command (on any machine with Git)

```powershell
git ls-files | Measure-Object | Select-Object -ExpandProperty Count
```

Expected output: `111`
