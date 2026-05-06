# Demo Prompt — "Add Product (Admin)" v2 replay (Distilled Final)

## Prompting protocol for demo training

### Step 1 — Context check (mandatory before planning/coding)
- Ask and answer 2 questions first:
  - 1 technical question (API/contract/coding boundary),
  - 1 business question (goal/non-goal boundary).
- Only proceed when both are explicitly answered.

### Step 2 — Define F.A and build execution checklist
- Describe functional area (F.A) precisely.
- Build checklist by requirement files.
- Sync that checklist into `Planning_doc_demo/devplan_checklist.md`.

### Step 3 — Explain intent per step
- Each step must state:
  - what is implemented,
  - why it is needed,
  - what output proves completion.

### Step 4 — Implement step-by-step with gates
- Execute sequentially (no skipping).
- After each step:
  - verify step with concrete checks,
  - test the step (within agreed test scope),
  - mark step done only after prove-done.

---

## How to run v2
- Create branch from `main`: `git checkout -b demo/add-product-page-v2`.
- Paste the prompt block below to the AI assistant.
- Let AI execute D1 → D9.
- Validate with manual smoke + `dotnet test` + `npx ng build --configuration=development`.

**Baseline assumption:** `main` contains baseline module (`module_src/backend`, `module_src/frontend`, `Planning_doc`, `Planning_doc_demo`) and does **not** contain Add Product implementation code.

---

## PROMPT TO PASTE — Vietnamese

Bạn là AI coding assistant cho demo training người mới. Mục tiêu: triển khai lại Add Product v2 nhanh, chuẩn, ít drift nhất từ tài liệu `Planning_doc_demo`.

### -1) Startup rule (bắt buộc)
- Ngay khi bắt đầu Add Product v2, phải khởi động bằng **Plan Mode** (không code ngay).
- Trong Plan Mode, bắt buộc làm đủ:
  1. trả lời 2 câu context-check (1 technical + 1 business),
  2. **tạo mới hoặc cập nhật `Planning_doc_demo/devplan_checklist.md` trực tiếp từ `business_requirements.md`** (không chỉ đọc/dùng checklist cũ),
  3. trình bày mục tiêu từng step + prove-done tương ứng.
- Chỉ chuyển sang implementation sau khi plan được chốt.
- **No-cheat:** không copy nguyên checklist cũ để giống cho nhanh; chỉ dùng checklist cũ như reference, còn plan v2 phải tái tạo từ context hiện tại và nêu lý do cho mọi sai khác đáng kể.

### 0) Bắt buộc đọc context theo thứ tự
1. `Planning_doc_demo/business_requirements.md`
2. `Planning_doc_demo/devplan_checklist.md`
3. `Planning_doc_demo/issues_history.md`
4. `Planning_doc_demo/Lessonlearn.md`
5. `Planning_doc_demo/Promt_History.md`
6. Code tham chiếu:
   - `module_src/backend/RevenueModule.Api/Controllers/BillsController.cs`
   - `module_src/backend/RevenueModule.Api.Tests/Step6IntegrationTests.cs`
   - `module_src/frontend/src/app/pages/revenue/revenue.component.{ts,html,css}`
   - `module_src/frontend/src/styles.css`

### 1) Scope bắt buộc
- 1 page admin `/lab/products`.
- Full CRUD + filter cho `Product` (`Id`, `Name`, `UnitPriceVnd`) không thêm field.
- Không làm auth, soft delete, audit log, optimistic concurrency.
- Không sửa logic nghiệp vụ bill/revenue.

### 2) Contract kỹ thuật bắt buộc giữ
- `GET /api/products`:
  - không param → array (backward-compat cho bill/revenue),
  - có param (`q`, `priceMin`, `priceMax`, `page`, `pageSize`) → paged object.
- Validation:
  - `Name`: trim, non-empty, max 160,
  - `UnitPriceVnd`: > 0,
  - `priceMin > priceMax` → `400 invalid_price_range`.
- Error contract: `ApiError(code,message)` + message tiếng Việt cho user-facing.
- Delete semantics:
  - app check `BillLines.Any`,
  - DB FK `BillLines.ProductId -> Products.Id` (`ON DELETE RESTRICT`),
  - catch `DbUpdateException` map về `409 conflict_product_in_use`.

### 3) UI/UX + a11y bắt buộc giữ
- Route `/lab/products`, nav link “Sản phẩm” giữa bill/revenue, giữ `aria-current`.
- List + filter + pagination + loading/empty/error states.
- Search text phải apply live theo debounce 300ms bằng pattern:
  - template `(ngModelChange)="onSearchChanged($event)"`,
  - component set state trước khi trigger debounce.
- Price filters debounce 300ms.
- Modal Add/Edit + Delete confirm:
  - `role="dialog"`, `aria-modal`, `aria-labelledby`,
  - delete dialog có `aria-describedby`,
  - focus trap 3 nhánh (first/last/shell),
  - scroll lock + restore,
  - return focus to opener,
  - destructive confirm focus mặc định ở “Hủy”.
- Error text có `role="alert"` + `aria-live` (`polite` field, `assertive` dialog/page).
- Action button theo row có `aria-label` chứa tên product.

### 4) Execution protocol
Làm tuần tự theo D1 → D9 trong `devplan_checklist.md`.
- Ngay đầu phiên implementation, phải có bước **Plan Sync**:
  - đối chiếu `business_requirements.md`,
  - tạo/cập nhật lại `devplan_checklist.md`,
  - chỉ bắt đầu D1 khi checklist đã phản ánh đúng scope hiện tại.
- Sau mỗi step:
  - update checklist/task state,
  - run verify tương ứng,
  - chỉ mark done khi prove-done pass.
- Nếu phát sinh task khi test/debate/fix: thêm vào checklist/docs ngay.
- Hard stop: nếu step hiện tại chưa pass verify/test/docs-sync thì không được chuyển step tiếp theo.

### 5) Testing protocol (theo demo-step scope)
- Ưu tiên test như step trước:
  - backend: `dotnet test`,
  - frontend: `npx ng build --configuration=development`,
  - manual smoke theo UC1-UC5 + regression bill/revenue.
- Chỉ thêm unit test frontend nếu user yêu cầu rõ.

### 6) Documentation protocol
- Mọi lỗi/sai lầm (user phát hiện hoặc tự phát hiện) phải ghi ngay `Planning_doc_demo/issues_history.md`.
- Mọi lesson mới phải distill vào `Planning_doc_demo/Lessonlearn.md`.
- Mọi quyết định lớn phải log vào `Planning_doc_demo/Promt_History.md`.
- Tuân thủ mirror policy với `Planning_doc` (issues + lesson), chấp nhận cả mục thừa/chuyên hóa nếu hữu ích cho training.

### 7) Review gates bắt buộc
- Sau D2: council review backend adversarial.
- Sau D6: council review frontend adversarial.
- Trước close: council review final cho prompt/docs.

### 8) Output cuối cùng
Khi xong, báo ngắn:
- tests/build pass status,
- file chính đã sửa/tạo,
- smoke checklist đã verify,
- branch + commit hash cuối.

### 9) Failure modes & recovery rules (siêu chuẩn)
- Nếu `dotnet build`/`ef` fail kiểu file-lock (`MSB3021/MSB3027`), phải dừng process đang giữ exe/cổng trước khi build/migrate lại.
- Khi UI lỗi data, debug theo thứ tự cố định:
  1. FE up?,
  2. API up?,
  3. `/health` ok?,
  4. 1 endpoint data trả 200?;
  chỉ khi pass runtime gate mới debug business/filter.
- Mọi thay đổi endpoint dùng chung (đặc biệt list/filter contract) phải smoke cross-consumer:
  - `/lab/products`,
  - `/lab/bill`,
  - `/lab/revenue`.
- Integration test phải deterministic:
  - dữ liệu test có scope/prefix riêng (GUID tag),
  - assert trên dataset cô lập, không phụ thuộc seed dùng chung.
- Khi mirror bài học/issue giữa `Planning_doc` và `Planning_doc_demo`, ưu tiên giữ lại cả mục chuyên hóa nếu có ích cho training; thêm source tag để trace.

---

## PROMPT TO PASTE — English

You are the coding assistant for a beginner training demo. Goal: rebuild Add Product v2 fast and accurately from `Planning_doc_demo`, with minimal drift.

### -1) Startup rule (mandatory)
- Start Add Product v2 in **Plan Mode** first (do not jump into coding).
- In Plan Mode, you must complete:
  1. 2 context-check questions (1 technical + 1 business),
  2. **create or update `Planning_doc_demo/devplan_checklist.md` directly from `business_requirements.md`** (not just reusing the old checklist),
  3. present per-step intents + prove-done gates.
- Move to implementation mode only after plan confirmation.
- **No-cheat:** do not blindly copy the old checklist; use it only as reference, regenerate v2 plan from current context, and justify any meaningful differences.

### 0) Mandatory reading order
1. `Planning_doc_demo/business_requirements.md`
2. `Planning_doc_demo/devplan_checklist.md`
3. `Planning_doc_demo/issues_history.md`
4. `Planning_doc_demo/Lessonlearn.md`
5. `Planning_doc_demo/Promt_History.md`
6. Reference code:
   - `module_src/backend/RevenueModule.Api/Controllers/BillsController.cs`
   - `module_src/backend/RevenueModule.Api.Tests/Step6IntegrationTests.cs`
   - `module_src/frontend/src/app/pages/revenue/revenue.component.{ts,html,css}`
   - `module_src/frontend/src/styles.css`

### 1) Mandatory scope
- One admin page `/lab/products`.
- Full CRUD + filter for `Product` (`Id`, `Name`, `UnitPriceVnd`) with no new fields.
- No auth, soft delete, audit log, or optimistic concurrency.
- Do not modify bill/revenue business logic.

### 2) Non-negotiable technical contract
- `GET /api/products`:
  - no query params => flat array (backward-compatible for bill/revenue pages),
  - with query params => paged object.
- Validation:
  - `Name`: trim, non-empty, max 160,
  - `UnitPriceVnd`: > 0,
  - `priceMin > priceMax` => `400 invalid_price_range`.
- Error contract: `ApiError(code,message)` with Vietnamese user-facing messages.
- Delete semantics:
  - app-level `BillLines.Any` check,
  - DB FK `BillLines.ProductId -> Products.Id` (`ON DELETE RESTRICT`),
  - map `DbUpdateException` to `409 conflict_product_in_use`.

### 3) Mandatory UI/UX + accessibility
- Route `/lab/products`; nav link between bill/revenue with proper `aria-current`.
- List + filters + pagination + loading/empty/error states.
- Text search must apply live using debounce 300ms with:
  - `(ngModelChange)="onSearchChanged($event)"`,
  - update state before triggering debounce stream.
- Price filters also debounce 300ms.
- Add/Edit modal + Delete confirm modal:
  - dialog roles/labels,
  - delete dialog `aria-describedby`,
  - 3-branch focus trap,
  - scroll lock + restore,
  - return focus to opener,
  - destructive confirm initially focuses Cancel.
- Errors use `role="alert"` + `aria-live` (polite for field, assertive for dialog/page).
- Row action buttons include product name in `aria-label`.

### 4) Execution protocol
- Execute D1 -> D9 sequentially from `devplan_checklist.md`.
- At implementation start, run a mandatory **Plan Sync**:
  - reconcile with `business_requirements.md`,
  - create/update `devplan_checklist.md`,
  - start D1 only after checklist reflects current scope.
- After every step:
  - update checklist/task state,
  - run step verification,
  - mark done only when prove-done passes.
- If new tasks emerge during testing/debate/fix, add them to checklist/docs immediately.
- Hard stop: if current step has not passed verify/test/docs-sync, do not move to the next step.

### 5) Testing protocol (demo-step scope)
- Preferred scope:
  - backend `dotnet test`,
  - frontend `npx ng build --configuration=development`,
  - manual smoke for UC1-UC5 + bill/revenue regression.
- Add frontend unit tests only if explicitly requested by user.

### 6) Documentation protocol
- Log every mistake/issue immediately in `Planning_doc_demo/issues_history.md`.
- Distill every reusable lesson in `Planning_doc_demo/Lessonlearn.md`.
- Log major decisions in `Planning_doc_demo/Promt_History.md`.
- Follow mirror policy with `Planning_doc` (issues + lessons), including specialized/extra entries if useful for training.

### 7) Mandatory review gates
- After D2: adversarial backend council review.
- After D6: adversarial frontend council review.
- Before close: final council review for prompt/docs completeness.

### 8) Final output format
- tests/build status,
- key files changed/created,
- smoke checks verified,
- final branch + commit hash.

### 9) Failure modes & recovery rules (high-rigor)
- If `dotnet build`/`ef` fails with file-lock issues (`MSB3021/MSB3027`), stop processes holding exe/ports before rebuilding/migrating.
- For “UI data not loading”, use fixed runtime triage order:
  1. FE up?,
  2. API up?,
  3. `/health` ok?,
  4. one data endpoint returns 200?;
  only debug business/filter logic after runtime gates pass.
- Any change to shared list/filter endpoint contracts requires cross-consumer smoke on:
  - `/lab/products`,
  - `/lab/bill`,
  - `/lab/revenue`.
- Integration tests must be deterministic:
  - test data scoped by unique prefix/GUID tag,
  - assertions isolated from shared seed data.
- When mirroring lessons/issues between `Planning_doc` and `Planning_doc_demo`, keep specialized entries if useful for training and include source tags for traceability.
