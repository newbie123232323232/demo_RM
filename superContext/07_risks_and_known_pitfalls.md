# 07 - Risks and Known Pitfalls

## Runtime and environment risks

- Shared DB across branches if connection strings are unchanged.
  - Effect: test data cross-pollution between branch runs.
- Locked API executable causing build failures (`MSB3027`/`MSB3021`).
- Duplicate FE dev-server start causing interactive prompt and automation break.

## Contract drift risks

- `GET /api/products` dual-shape contract is easy to accidentally break.
- Error messages/codes can drift between backend and frontend handling.
- Docs can diverge across material branches if not regularly reconciled.

## UX/A11y regression risks already seen

- Search only updates after blur if event binding drifts.
- Price min/max typing can produce confusing generic list errors without guard.
- Modal focus trap can leak focus if shell branch is not handled.

## Governance risks

- Checklist truth drift ("done but not ticked").
- Mixing task lines and criteria lines without explicit format can confuse review.
- Time ordering in issues log is not strict chronological by design; use severity markers for scanability.

## Mitigations currently used

- 3-channel backend testing evidence.
- Council/adversarial reviews at key milestones.
- Dependency annotations in checklist.
- Consolidated handoff docs (`Planning_doc_demo/full_chat_handoff.md` + this `superContext` pack).

