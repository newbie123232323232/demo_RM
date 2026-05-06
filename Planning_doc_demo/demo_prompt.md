# Demo Prompt — "Add Product (Admin)" v2 replay

> **Cách dùng / How to use**
>
> 1. Trên `main` HEAD, tạo branch mới: `git checkout -b demo/add-product-page-v2`. (`main` đã có sẵn `Planning_doc_demo/` để AI tham chiếu.)
> 2. Paste TOÀN BỘ phần "PROMPT TO PASTE" bên dưới vào AI assistant duy nhất.
> 3. AI sẽ tự đọc các file trong `Planning_doc_demo/` để có context đầy đủ và thực thi từ D1 → D9.
> 4. Khi AI hoàn tất, manual-smoke + `dotnet test` + `npx ng build` để confirm v2 ra kết quả ~95% giống v1.
>
> **Repo**: `d:\BT_intdemo1\marketify-mini-ecommerce` (Windows / PowerShell). Branch v1 reference: `demo/add-product-page-v1` (giữ trên remote nếu cần xem lại implementation thực tế).
>
> **Baseline assumption (BẮT BUỘC):** `main` branch chứa toàn bộ baseline module (`module_src/backend`, `module_src/frontend`, `Planning_doc/`, `Planning_doc_demo/`) — chỉ thiếu *feature* Add Product (controller chưa có CRUD, page Angular chưa tồn tại, FK migration chưa apply, integration test chưa có). Nếu `main` không có baseline này, dừng demo và checkout từ `demo-baseline` tag (HEAD của `main` ngay trước khi tách v1).

---

## PROMPT TO PASTE — Vietnamese

Bạn là một CTO startup perfectionist đang triển khai feature "Add Product (Admin)" cho module doanh thu/VipPoint hiện có. Mục tiêu là demo "cách làm việc với trợ lý AI trong coding". Phải ship clean, không over-engineer, không compromise những thứ thực sự quan trọng.

### 1. Context bắt buộc đọc trước khi code
Đọc đầy đủ thứ tự sau (không skip):
1. `Planning_doc_demo/business_requirements.md` — spec admin CRUD + filter, validation table, DoD.
2. `Planning_doc_demo/devplan_checklist.md` — chuỗi step D1-D9 với checkpoint "prove done".
3. `Planning_doc_demo/Lessonlearn.md` — luật pattern (modal a11y full 3 nhánh focus trap, FK semantics, backward-compat list endpoint, error contract aria-live).
4. `Planning_doc_demo/issues_history.md` — issue đã gặp và rule đã chốt; **không lặp lại các bug này**.
5. `Planning_doc_demo/Promt_History.md` — chronological các quyết định lớn của v1 (làm tham chiếu khi có conflict).
6. `Planning_doc/Lessonlearn.md` — luật chung của module (chạy local, port, dọn cổng).
7. Code reference (đọc để bám pattern, không sửa):
   - `module_src/backend/RevenueModule.Api/Controllers/BillsController.cs` (pattern controller, ApiError, validation helper).
   - `module_src/backend/RevenueModule.Api.Tests/Step6IntegrationTests.cs` (pattern integration test với WebApplicationFactory + real Postgres).
   - `module_src/frontend/src/app/pages/revenue/revenue.component.{ts,html,css}` (modal pattern hardened).
   - `module_src/frontend/src/styles.css` (design tokens modern-soft).

### 2. Goal
Bổ sung **1 page admin** `/lab/products` ngang hàng `/lab/bill` và `/lab/revenue`, **full CRUD** + **filter** trên entity `Product` (`Id`, `Name`, `UnitPriceVnd` — không thêm field mới). Không động nghiệp vụ bill/revenue.

### 3. Constraints (cứng)
- **Không** sửa logic bill/revenue/chart/export/reconcile. Chỉ sửa `app.component.html` (thêm 1 nav link) và `app.routes.ts` (thêm 1 route).
- **Backward-compat** `GET /api/products`: không param → mảng phẳng (giữ nguyên cho `/lab/bill`, `/lab/revenue` tiêu thụ); có param → object phân trang `{items,page,pageSize,totalCount,totalPages}`.
- Validation BR-aligned: Name trim, không rỗng, ≤ **160 ký tự** (khớp DB constraint `HasMaxLength(160)`); UnitPriceVnd > 0; lỗi `400 invalid_payload` với message tiếng Việt named field cụ thể.
- DELETE phải có 2 lớp: app-check `BillLines.Any(...)` + DB FK `BillLines.ProductId → Products.Id ON DELETE RESTRICT` (migration `DemoProductFkOnBillLine`). Catch `DbUpdateException` → map về cùng 409 contract.
- Lọc range `priceMin > priceMax` → `400 invalid_price_range` (đối xứng `BillsController` `from > to`).
- Tất cả error message hiển thị user là **tiếng Việt** (404 product_not_found, 409 conflict_product_in_use, 400 invalid_payload / invalid_price_range).
- Modal Add/Edit + Confirm Delete bắt buộc đầy đủ a11y theo pattern Lessonlearn §3 §4 §5: role/aria-modal/aria-labelledby + aria-describedby (cho confirm), tabindex=-1, focus dialog/first-input khi mở, Esc đóng, click backdrop đóng, click trong dialog stopPropagation, body scroll lock + restore overflow cũ, return focus tới opener khi đóng, **focus trap 3 nhánh** (Shift+Tab từ first/shell → last; Tab từ last → first; Tab từ shell → first), initial focus trên Hủy cho destructive confirm.
- Errors trong page/dialog có `role="alert"` + `aria-live="polite"` (field) hoặc `"assertive"` (dialog/page level).
- Row action button có `[attr.aria-label]="'<verb> ' + entity.<name>"`.
- Inputs disabled trong submit lifetime (không chỉ button).
- Search debounce 300ms, priceMin/priceMax debounce 300ms (chống DoS API).
- Timeout client 10s + message "Request timeout (>10s). Kiểm tra API/DB hoặc kết nối mạng.".

### 4. Definition of Done
- Backend: 5 endpoint mới/mở rộng (`GET list with filter+paging+backward-compat`, `GET by id`, `POST`, `PUT`, `DELETE`). Migration FK đã apply. `dotnet test` pass đầy đủ (không regression Step 1-6.5 cũ).
- Frontend: page `/lab/products` có list + filter + pagination + Add/Edit modal + Confirm Delete modal hoạt động. `npx ng build --configuration=development` pass. 2 page cũ (`/lab/bill`, `/lab/revenue`) không regression (smoke nhanh).
- A11y: modal pass đủ 3 nhánh focus trap, errors có aria-live, row buttons có aria-label, destructive confirm focus trên Hủy.
- Tài liệu `Planning_doc_demo/` đã sync (issues_history, Promt_History) cho các quyết định lớn v2.

### 5. Non-goals (rejected scope)
- Auth thật (không cần login, ai vào page cũng admin).
- Soft-delete / audit log / lịch sử thay đổi / bulk import-export.
- Liên kết qua lại từ product trong page bill/revenue (không touch bill/revenue UI).
- Optimistic concurrency / rowversion.
- Cap upper bound `UnitPriceVnd`.
- Escape `%`/`_`/`\` trong ILike (acceptable cho admin demo; document trong Lessonlearn nếu chưa có).

### 6. Execution protocol
Đi tuần tự D1 → D9 trong `devplan_checklist.md`. Mỗi step kết thúc bắt buộc tick checklist + chạy "prove done" rồi mới chuyển step:
- **D1** Backend CRUD + filter + 409 + DB FK migration.
- **D2** Integration tests (≥ 12 case: happy POST/PUT/DELETE/GET-list-with-filter/paging-clamp + 400/404/409 paths + backward-compat array shape + name=null + name>160 + price≤0 + priceMin>priceMax + pageSize>100 clamp).
- **D3** FE route + nav + skeleton component.
- **D4** FE list + filter (debounced) + pagination + states.
- **D5** FE modal Add/Edit a11y full (3-branch focus trap) + validation client + map 400.
- **D6** FE modal Confirm Delete a11y + handle 409/404 (no auto-close timeout race).
- **D7** Verification: `dotnet test` 26+ pass; `ng build` pass; smoke API end-to-end (xem checklist `business_requirements.md` §5); smoke 2 page cũ không regression.
- **D8** Sync `Planning_doc_demo/issues_history.md` + `Promt_History.md` cho v2 (không sửa BR/devplan/Lessonlearn vì v1 đã chốt).
- **D9** Cherry-pick docs về `main` (chỉ `Planning_doc_demo/`, không code).

### 7. Council review gates (bắt buộc)
- **Sau D2:** trigger council adversarial backend (API contract, FK semantics, edge cases, test gaps). Triage CRITICAL + MAJOR rồi fix trước khi tiếp.
- **Sau D6:** trigger council adversarial frontend (a11y modal đầy đủ 3 nhánh focus trap, behavior preservation 2 page cũ, role/aria-live/aria-label). Triage rồi fix.
- **Trước close (sau D8):** council final verify `demo_prompt.md` này có còn đủ thông tin tái lập không (nếu phát hiện gap, update prompt cho lần demo sau).

### 8. Commit cadence
Plain commit, không trailer. Tách commit code và commit docs:
- `demo(product): scaffold Planning_doc_demo skeleton...` (đã có sẵn ở main).
- `demo(product): backend CRUD + filter + 409 FK conflict (with hardened DB FK + N integration tests)`.
- `demo(product): docs sync after backend D2 + council review`.
- `demo(product): frontend products page (list + filter + pagination + modal Add/Edit/Delete a11y)`.
- `demo(product): docs sync after frontend D6 + council review`.
- `demo(product): backend message localization + final smoke pass`.

### 9. Khi nào hỏi user
Chỉ hỏi khi missing context **thực sự thay đổi correctness/architecture/tradeoffs/Definition of Done**. Không hỏi performative. Khi nghi ngờ, đọc lại context bắt buộc ở §1 — câu trả lời gần như luôn ở đó.

### 10. Báo cáo cuối
Khi xong D9, output gọn:
- Số test backend pass / total.
- File chính đã tạo / sửa.
- Smoke checklist đã verify.
- Branch và commit hash cuối của `demo/add-product-page-v2`.

---

## PROMPT TO PASTE — English

You are a perfectionist startup CTO implementing the "Add Product (Admin)" feature on top of the existing revenue/VipPoint module. Goal is to demo "how to work with an AI assistant in coding". Ship clean, don't over-engineer, don't compromise on what matters.

### 1. Mandatory reading order
Read in full, in this order, no skipping:
1. `Planning_doc_demo/business_requirements.md` — admin CRUD + filter spec, validation table, DoD.
2. `Planning_doc_demo/devplan_checklist.md` — D1-D9 step chain with prove-done checkpoints.
3. `Planning_doc_demo/Lessonlearn.md` — pattern rules (modal a11y full 3-branch focus trap, FK semantics, backward-compat list endpoint, aria-live error contract).
4. `Planning_doc_demo/issues_history.md` — issues already faced and rules locked in; **do not repeat these**.
5. `Planning_doc_demo/Promt_History.md` — chronological key v1 decisions (reference when conflict).
6. `Planning_doc/Lessonlearn.md` — module-wide rules (local run, ports, kill ports).
7. Reference code (read to follow patterns, don't modify):
   - `module_src/backend/RevenueModule.Api/Controllers/BillsController.cs` (controller pattern, ApiError, validation helper).
   - `module_src/backend/RevenueModule.Api.Tests/Step6IntegrationTests.cs` (integration test pattern with WebApplicationFactory + real Postgres).
   - `module_src/frontend/src/app/pages/revenue/revenue.component.{ts,html,css}` (hardened modal pattern).
   - `module_src/frontend/src/styles.css` (modern-soft design tokens).

### 2. Goal
Add **one admin page** at `/lab/products` peer to `/lab/bill` and `/lab/revenue`, **full CRUD** + **filter** on the existing `Product` entity (`Id`, `Name`, `UnitPriceVnd` — no new fields). Do not touch bill/revenue business logic.

### 3. Hard constraints
- **Do not** modify bill/revenue/chart/export/reconcile logic. Only edit `app.component.html` (add 1 nav link) and `app.routes.ts` (add 1 route).
- **Backward-compat** `GET /api/products`: no params → flat array (existing `/lab/bill`, `/lab/revenue` consume this); any param → paged object `{items,page,pageSize,totalCount,totalPages}`.
- Validation aligned with BR: Name trim, non-empty, ≤ **160 chars** (matches DB constraint `HasMaxLength(160)`); UnitPriceVnd > 0; `400 invalid_payload` with Vietnamese message naming the failing field.
- DELETE must have 2 layers: app-level `BillLines.Any(...)` check + DB FK `BillLines.ProductId → Products.Id ON DELETE RESTRICT` (migration `DemoProductFkOnBillLine`). Catch `DbUpdateException` → map to same 409 contract.
- Range filter `priceMin > priceMax` → `400 invalid_price_range` (symmetric with `BillsController` `from > to`).
- All user-visible error messages are **Vietnamese** (404 product_not_found, 409 conflict_product_in_use, 400 invalid_payload / invalid_price_range).
- Modal Add/Edit + Confirm Delete must implement full a11y per Lessonlearn §3 §4 §5: role/aria-modal/aria-labelledby + aria-describedby (confirm only), tabindex=-1, focus dialog/first-input on open, Esc closes, backdrop click closes, dialog click stopPropagation, body scroll lock + restore previous overflow, return focus to opener on close, **3-branch focus trap** (Shift+Tab from first/shell → last; Tab from last → first; Tab from shell → first), initial focus on Cancel button for destructive confirm.
- Errors in page/dialog have `role="alert"` + `aria-live="polite"` (field) or `"assertive"` (dialog/page level).
- Row action buttons have `[attr.aria-label]="'<verb> ' + entity.<name>"`.
- Inputs disabled during submit lifetime (not just buttons).
- Search debounce 300ms, priceMin/priceMax debounce 300ms (anti-DoS).
- Client timeout 10s + message in Vietnamese.

### 4. Definition of Done
- Backend: 5 new/extended endpoints. FK migration applied. `dotnet test` passes everything (no regression to Step 1-6.5).
- Frontend: `/lab/products` has list + filter + pagination + Add/Edit modal + Confirm Delete modal working. `npx ng build --configuration=development` passes. 2 existing pages (`/lab/bill`, `/lab/revenue`) don't regress.
- A11y: modals pass 3-branch focus trap, errors have aria-live, row buttons have aria-label, destructive confirm focuses Cancel.
- Docs `Planning_doc_demo/` synced (issues_history, Promt_History) for major v2 decisions.

### 5. Non-goals (rejected scope)
- Real auth (no login; anyone visiting the page is admin).
- Soft-delete / audit log / change history / bulk import-export.
- Cross-linking from product in bill/revenue pages (do not touch bill/revenue UI).
- Optimistic concurrency / rowversion.
- Upper-bound cap on `UnitPriceVnd`.
- Escape `%`/`_`/`\` in ILike (acceptable for admin demo; document in Lessonlearn if not already).

### 6. Execution protocol
Walk D1 → D9 sequentially per `devplan_checklist.md`. Each step ends with checklist tick + prove-done before moving on:
- **D1** Backend CRUD + filter + 409 + FK migration.
- **D2** Integration tests (≥ 12 cases as listed in §3 of devplan).
- **D3** FE route + nav + skeleton component.
- **D4** FE list + filter (debounced) + pagination + states.
- **D5** FE modal Add/Edit a11y full + client validation + 400 mapping.
- **D6** FE modal Confirm Delete a11y + 409/404 handling (no auto-close timeout race).
- **D7** Verification: `dotnet test` ≥ 26 pass; `ng build` pass; smoke API end-to-end; smoke 2 existing pages.
- **D8** Sync `Planning_doc_demo/issues_history.md` + `Promt_History.md` for v2 (do not modify BR/devplan/Lessonlearn — v1 locked them).
- **D9** Cherry-pick docs to `main` (only `Planning_doc_demo/`, no code).

### 7. Mandatory council review gates
- **After D2:** trigger backend adversarial council (API contract, FK semantics, edge cases, test gaps). Triage CRITICAL + MAJOR and fix before continuing.
- **After D6:** trigger frontend adversarial council (full 3-branch focus trap a11y, behavior preservation of 2 existing pages, role/aria-live/aria-label). Triage and fix.
- **Before close (after D8):** final council review verifying this `demo_prompt.md` still has enough info to reproduce (if gaps found, update the prompt for the next demo run).

### 8. Commit cadence
Plain commits, no trailer. Separate code and docs commits — see Vietnamese §8 for suggested messages.

### 9. When to ask the user
Only when missing context **actually changes correctness/architecture/tradeoffs/Definition of Done**. Don't ask performatively. When in doubt, re-read mandatory context in §1 — the answer is almost always there.

### 10. Final report
When D9 is done, output concisely:
- Backend tests pass count / total.
- Files created / modified (main ones).
- Smoke checklist verified.
- Branch and final commit hash on `demo/add-product-page-v2`.
