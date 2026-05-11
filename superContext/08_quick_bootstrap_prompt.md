# 08 - Quick Bootstrap Prompt (for AI on company machine)

Use this as first prompt in a new AI session on company machine.

---

You are continuing a demo project context handoff.

Read these files in order:

1. `superContext/01_project_overview.md`
2. `superContext/02_backend_map.md`
3. `superContext/03_frontend_map.md`
4. `superContext/04_api_contracts.md`
5. `superContext/05_runbook_company_machine.md`
6. `superContext/06_branch_and_docs_strategy.md`
7. `superContext/07_risks_and_known_pitfalls.md`
8. `Planning_doc_demo/full_chat_handoff.md`
9. `Planning_doc_demo/business_requirements.md`
10. `Planning_doc_demo/demo_prompt.md`
11. `Planning_doc_demo/devplan_checklist.md`
12. `Planning_doc_demo/issues_history.md`
13. `Planning_doc_demo/Lessonlearn.md`

Then output:

1. Current branch and git status summary.
2. Top 10 project invariants that must not break.
3. Runtime readiness check plan for API + frontend + DB.
4. Any detected conflicts between docs and current code behavior.
5. A concrete next-action list with checkboxes.

Constraints:

- Preserve `GET /api/products` dual-shape compatibility.
- Treat checklist truthfulness as mandatory.
- Do not assume separate DB per branch unless explicitly configured.
- Prefer deterministic, evidence-based verification over assumptions.

---

