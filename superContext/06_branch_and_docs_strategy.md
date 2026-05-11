# 06 - Branch and Docs Strategy

## Branch roles (current convention)

- `main`: protected baseline.
- `src-demo-test-1`: seed material branch.
- `src-demo-test-2`: first successful full demo test branch and docs hardening.
- `src-demo-test-3`: official next demo-test branch created from synced seed.

## Context docs that must stay aligned

- `Planning_doc_demo/business_requirements.md`
- `Planning_doc_demo/demo_dry_run_checklist.md`
- `Planning_doc_demo/demo_prompt.md`
- `Planning_doc_demo/issues_history.md`
- `Planning_doc_demo/Lessonlearn.md`

## Anti-drift approach

- When material-2 includes stable refinements, sync back into seed (material-1).
- Keep docs-only commits separated from code commits whenever possible.
- Validate cross-branch doc delta before demo:

```powershell
git diff --name-status src-demo-test-1..src-demo-test-2 -- Planning_doc_demo/business_requirements.md Planning_doc_demo/demo_dry_run_checklist.md Planning_doc_demo/demo_prompt.md Planning_doc_demo/issues_history.md Planning_doc_demo/Lessonlearn.md
```

## Checklist semantics that were standardized

- `-` = task (checkbox unit).
- `+` = criteria/spec/validation under a task.
- Pending dependencies must be explicit inline.

