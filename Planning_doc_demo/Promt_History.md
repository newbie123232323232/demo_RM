# Promt History — Demo "Add Product (Admin)" v1

> Log chronological các exchange có quyết định nghiệp vụ/kỹ thuật trong v1 demo.
> Format: số thứ tự, prompt user (rút gọn, giữ ý), quyết định/output AI, file ảnh hưởng.

---

1. **User:** sắp tới có yêu cầu demo về chủ đề "hướng dẫn cách làm việc với trợ lý AI trong coding"; chọn module hiện tại để demo. Quyết định thêm 1 chức năng mới: page admin Add Product (không phải buyer fake). Sẽ làm việc với AI trước để tạo chức năng đó, sau đó undo về trạng thái cũ để demo lần 2 trước khán giả.
   **AI:** đề xuất 2 hướng (A: branch riêng + cherry-pick docs về main; B: làm chung main rồi undo theo version), kèm phân tích rủi ro. Đề xuất prompt contract 5 phần (Goal/Constraints/Definition of Done/Non-goals/Execution protocol).

2. **User:** đã push toàn bộ src code hiện tại lên GitHub `newbie123232323232/demo_RM` nhánh main. Yêu cầu lập một bộ Planning_doc riêng cho chức năng demo, gồm 4 file tương tự `Planning_doc/` cũ + 1 file `Promt_History.md` để AI auto-fill trong quá trình làm v1, từ đó distill ra prompt hoàn chỉnh cho v2. Hỏi nên làm trên branch riêng (Option A) hay làm trên main rồi undo theo version (Option B).
   **AI:** mạnh mẽ khuyến nghị **Option A + docs-on-main**: implementation v1 sống trên branch `demo/add-product-page-v1`, chỉ folder `Planning_doc_demo/` được cherry-pick về main để v2 luôn có nguồn tham chiếu ổn định. Đề xuất 6 file: 4 mirror + `Promt_History.md` + `demo_prompt.md` (deliverable cuối cùng, distill từ Promt_History).

3. **User:** chốt Option A + docs-on-main. Chốt scope: 1 page admin `/lab/products` ngang hàng `lab/bill` và `lab/revenue`, full CRUD (thêm/xoá/sửa/view + filter), dùng đúng các thuộc tính hiện có của Product (`Id`, `Name`, `UnitPriceVnd`), không cần truy xuất/liên kết từ product trong các danh sách khác.
   **AI:** xác nhận `Product` chỉ có 3 trường (đọc `Models/Product.cs`); `ProductsController` hiện chỉ có `GET all`. Lập plan chính thức với:
   - Branch `demo/add-product-page-v1` từ main HEAD + tag `demo-baseline`.
   - Folder `Planning_doc_demo/` ở root, 6 file.
   - Implementation chia 9 step (D1 backend CRUD + filter, D2 backend tests, D3 FE route+nav+skeleton, D4 FE list+filter+pagination, D5 FE modal Add/Edit, D6 FE confirm delete + 409, D7 verify smoke, D8 finalize docs + distill demo_prompt, D9 cherry-pick docs về main).
   - Council review 3 lần (sau D2, sau D6, trước close).
   - Backward-compat `GET /api/products`: không param → array (cũ), có param → paged object (mới).
   - FK semantics khi xoá: explicit check `BillLines`, trả 409 `conflict_product_in_use`, không silent.

4. **User:** confirm plan, yêu cầu execute từ đầu đến hết todos.
   **AI:** tạo branch `demo/add-product-page-v1` + tag `demo-baseline` từ main HEAD (commit `5ab99cb`). Scaffold folder `Planning_doc_demo/` với 6 file:
   - `business_requirements.md` (đầy đủ spec UC1-UC5, API contract chi tiết, validation table, DoD).
   - `devplan_checklist.md` (D1-D9 với checkpoint prove-done từng step).
   - `Lessonlearn.md` (7 luật rút sẵn cho v1 demo).
   - `issues_history.md` (template trống, fill dần).
   - `Promt_History.md` (file này).
   - `demo_prompt.md` (stub, distill cuối v1).

5. **AI (self-driven backend council):** sau khi Step D1 + D2 backend xong (12 test mới + 11 cũ pass), tự trigger council review adversarial theo `using-superpowers` rule. Subagent reviewer phát hiện:
   - CRITICAL: `BillLine.ProductId` thiếu DB-level FK → race + orphan rows.
   - MAJOR: 409 message English không khớp BR; thiếu validate `priceMin > priceMax`; thiếu test `name=null`, `pageSize > 100` clamp.
   - MINOR: ILike không escape wildcard, không có optimistic concurrency, `UnitPriceVnd` không cap upper bound.
   **Quyết định triage:**
   - Accept critical + 2 major (FK migration `DemoProductFkOnBillLine` + VN message + 400 invalid_price_range + 3 tests mới).
   - Reject "field-level invalid_payload" (message đã name field), "totalPages overflow" (math bounded), "hasFilterParam ambiguity" (intent rõ).
   - Defer minors vào `Lessonlearn.md` mục "Known limitations".
   **File bị ảnh hưởng:**
   - `module_src/backend/RevenueModule.Api/Data/RevenueDbContext.cs` (thêm FK Restrict).
   - `module_src/backend/RevenueModule.Api/Data/Migrations/20260506041628_DemoProductFkOnBillLine.cs` (mới).
   - `module_src/backend/RevenueModule.Api/Controllers/ProductsController.cs` (VN msg + try/catch DbUpdateException + 400 invalid_price_range).
   - `module_src/backend/RevenueModule.Api.Tests/DemoProductCrudIntegrationTests.cs` (+3 test, total 15 test).
   - `Planning_doc_demo/business_requirements.md` + `Lessonlearn.md` + `issues_history.md` đồng bộ.
   - Re-run full suite: 26/26 pass.

6. **AI (self-driven implementation D3-D6):** triển khai full FE products page trong 1 component standalone (`ProductsPageComponent`):
   - Route `/lab/products` + nav link "Sản phẩm" giữa "Lập bill" và "Quản lý doanh thu" (giữ nguyên `routerLinkActive` + `aria-current` Phase 1).
   - List + filter (search debounce 300ms, priceMin/Max debounce 300ms) + pagination + empty/loading/error.
   - Modal Add/Edit shared (mode-based) + Modal Confirm Delete — cả hai bám pattern hardened revenue page (role/aria-modal/aria-labelledby/focus-trap/scroll-lock/return-focus).
   - Validation client-side (blur), map lỗi 400/404/409 từ backend, hiển thị inline trong dialog.
   - `npx ng build --configuration=development` pass.

7. **AI (self-driven frontend council):** trigger council adversarial sau D6. Subagent reviewer phát hiện:
   - CRITICAL: forward Tab từ dialog shell escape modal (focus trap incomplete).
   - MAJOR: text/number input không disable trong submit; aria-live thiếu trên error spans; row action button không có context sản phẩm cho AT; nested `setTimeout(0) + setTimeout(30)` racy; delete dialog thiếu `aria-describedby`; initial focus trên dialog shell thay vì Hủy cho destructive; price filter chưa debounce (DoS risk).
   - MINOR: placeholder không exposed; API shape coupling.
   - NIT: backdrop hardcoded rgba (matches reference).
   **Quyết định triage:**
   - Accept critical + 6 majors + 1 minor (debounce price filter).
   - Reject major #3 (focus restore on destroy — opener có thể đã unmount, restore overflow là đủ).
   - Reject major #5 (label-wrap cấu trúc khớp existing fake-bill/revenue, partial fix qua aria-live).
   - Defer #10/#13.
   **File bị ảnh hưởng:**
   - `module_src/frontend/src/app/pages/products/products.component.ts` (+ deleteCancelBtn ref, priceFilterDebounce$, single setTimeout focus, forward Tab from shell wrap, removed auto-close 404 setTimeout race).
   - `module_src/frontend/src/app/pages/products/products.component.html` (role="alert"/aria-live trên errors; aria-label row buttons; disabled inputs trong submit; aria-describedby delete dialog).
   - Re-build pass; lint sạch.

8. **AI (self-driven D7 verification):**
   - Full backend `dotnet test` 26/26 pass (sau khi sửa thêm `product_not_found` message sang VN cho khớp BR).
   - Smoke API end-to-end: `/health` ok; `GET /api/products` no-param → array (backward-compat); `?page=1&pageSize=5` → paged object; `?priceMin>priceMax` → 400 invalid_price_range; POST/PUT happy + POST invalid → 400; DELETE happy → 204; DELETE not-found → 404; DELETE Product Id 1 (đang dùng trong bill) → 409 conflict_product_in_use.
   - Frontend `npx ng build --configuration=development` pass.
   - 2 page hiện hữu (`/lab/bill`, `/lab/revenue`) dùng `GET /api/products` no-param vẫn nhận array — không regression.

9. **AI (self-driven final council on demo_prompt):** sau khi distill `demo_prompt.md`, trigger council adversarial cuối để verify đủ tái lập v2. Reviewer phát hiện 4 landmine:
   - D5-F5 vẫn ghi "200 ký tự" thay vì "160".
   - D6-F4 ghi "đóng dialog" khi 404 — drift với v1 (giữ dialog mở).
   - Null body trả English message.
   - `demo_prompt.md` thiếu baseline assumption (main phải có baseline module).
   **Đã fix tất cả 4 landmine** rồi mới close. File ảnh hưởng: `devplan_checklist.md`, `demo_prompt.md`, `ProductsController.cs`, `issues_history.md`. Re-run `dotnet test` lần cuối để confirm 26/26 vẫn pass sau khi đổi message.

10. **User:** manual test Add Product nhìn chung OK, chỉ còn UX lỗi: lọc theo tên chưa apply ngay; phải click ra ngoài hoặc làm action khác mới thấy list lọc. Yêu cầu fix và hỏi test coverage.
    **AI:** lần đầu thử chuyển event sang `(input)` + thêm unit test frontend để khóa behavior debounce/live search; build pass nhưng môi trường chặn ChromeHeadless nên không chạy unit test runtime được. Sau đó user xác nhận không cần unit test theo scope step hiện tại.
    **File bị ảnh hưởng (vòng thử nghiệm):**
    - `module_src/frontend/src/app/pages/products/products.component.html` (đổi event input).
    - `module_src/frontend/src/app/pages/products/products.component.spec.ts` (được tạo tạm, rồi xóa theo yêu cầu user).
    - `module_src/frontend/src/app/app.component.spec.ts` (được chỉnh tạm, rồi revert theo yêu cầu user).

11. **User:** xác nhận không cần unit test, chỉ test theo style các step trước; yêu cầu bỏ unit test.
    **AI:** xóa `products.component.spec.ts`, revert `app.component.spec.ts` về trạng thái cũ để nhánh demo không có thay đổi test ngoài scope.

12. **User:** báo bug vẫn còn: gõ `"demo"` chưa lọc ngay, chỉ khi bấm trang sau mới refresh list đúng.
    **AI:** chốt fix chắc chắn hơn:
    - Template dùng `(ngModelChange)="onSearchChanged($event)"`.
    - Component `onSearchChanged(value: string)` set trực tiếp `searchTerm = value` trước khi trigger debounce stream.
    - Giữ debounce 300ms, không phụ thuộc blur/click-out.
    - Rebuild frontend pass.
    **File bị ảnh hưởng:**
    - `module_src/frontend/src/app/pages/products/products.component.html`
    - `module_src/frontend/src/app/pages/products/products.component.ts`

13. **User:** yêu cầu cập nhật tài liệu theo đúng các chỉnh sửa UI/UX vừa phát hiện (bao gồm cả đoạn unit test đã thử rồi bỏ).
    **AI:** cập nhật:
    - `Planning_doc_demo/issues_history.md`: thêm 2 incident mới (text-filter live bug; unit-test scope drift + rollback).
    - `Planning_doc_demo/Lessonlearn.md`: thêm rule chuẩn event cho text filter debounce và rule phạm vi test theo demo-step.
    - Sau đó user yêu cầu cập nhật timeline `Promt_History.md` (entry này).

14. **User:** xác nhận cần tuân thủ rule mirror tài liệu cho training người mới: cứ ghi `issues_history` của `Planning_doc` vào `Planning_doc_demo`, tương tự với `Lessonlearn`; nội dung thừa/chuyên hóa vẫn chấp nhận.
    **AI:** áp dụng ngay bằng cách bổ sung policy bắt buộc vào docs demo:
    - `Planning_doc_demo/issues_history.md` thêm section **Mirror policy với Planning_doc/issues_history.md**.
    - `Planning_doc_demo/Lessonlearn.md` thêm section **Mirror policy với Planning_doc/Lessonlearn.md**.
    - Chốt nguyên tắc: ưu tiên "memory đầy đủ để training", mỗi entry mirror có thể giữ source để trace.

15. **User:** yêu cầu làm luôn sync pass để v2 Add Product có thể code nhanh và chuẩn như v1.
    **AI:** thực hiện mirror batch #1 từ `Planning_doc` sang `Planning_doc_demo`:
    - `issues_history.md`: import 5 issue workflow leverage cao cho v2 (contract drift, runtime gate, deterministic tests, provider parity, BE/FE error-contract rollout).
    - `Lessonlearn.md`: import 5 lesson vận hành quan trọng (runbook-first, port hygiene, close-step gate, cross-consumer smoke, runtime-signal over task-noise).
    - Tất cả batch mới đều gắn `Source` rõ ràng để trace nguồn.

16. **User:** yêu cầu distill-final `demo_prompt.md` để sẵn sàng code lại v2 thật nhanh và chuẩn.
    **AI:** rewrite `Planning_doc_demo/demo_prompt.md` thành bản distilled:
    - Giữ protocol training 4 bước ở đầu (context-check -> FA/checklist -> step intent -> step gates).
    - Rút prompt paste xuống dạng gọn/chặt, ưu tiên nguồn từ chính `Planning_doc_demo`.
    - Chốt rõ technical contract + UI/a11y contract + test/doc/review gates + final output format.
    - Giữ song ngữ VI-EN nhưng loại bỏ phần dư/nhắc lại không cần thiết.

17. **User:** đồng ý làm thêm dry-run checklist cho demo live.
    **AI:** tạo file mới `Planning_doc_demo/demo_dry_run_checklist.md` (1 trang, dạng tick-box) gồm:
    - pre-flight 5 checks trước khi paste prompt,
    - 3 checks sau mỗi step,
    - review gates bắt buộc (sau D2, sau D6, trước close),
    - định nghĩa done cuối buổi demo,
    - fallback ngắn khi demo lệch kỳ vọng.

18. **User:** yêu cầu harden prompt theo đúng tinh thần ban đầu lúc build Add Product v1: phải khởi động bằng plan mode, step-gated chặt, không cheat bằng copy checklist cũ.
    **AI:** cập nhật `Planning_doc_demo/demo_prompt.md`:
    - thêm `Startup rule` cho cả VI/EN: bắt đầu bằng Plan Mode, bắt buộc 2 câu context-check + regenerate checklist + step intent/prove-done rồi mới được code.
    - thêm `No-cheat` rule: cấm copy checklist cũ nguyên trạng; chỉ dùng làm reference.
    - thêm `Hard stop` ở execution: step chưa pass verify/test/docs-sync thì không được qua step kế.

19. **User:** yêu cầu nâng prompt lên mức "siêu chuẩn".
    **AI:** thêm section `Failure modes & recovery rules` (VI + EN) vào `demo_prompt.md`, chốt 5 nhóm guardrail:
    - file-lock recovery cho `dotnet build`/`ef` (`MSB3021/MSB3027`),
    - runtime-first triage khi UI lỗi data (`FE -> API -> /health -> data endpoint`),
    - bắt buộc cross-consumer smoke khi đổi endpoint dùng chung (`/lab/products`, `/lab/bill`, `/lab/revenue`),
    - deterministic integration test discipline (GUID scoped data, assert cô lập),
    - mirror policy high-rigor (giữ cả entry chuyên hóa + source tag traceability).

---

(Tiếp tục cập nhật theo từng exchange quyết định lớn.)
