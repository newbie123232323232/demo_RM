# 09 - Raw Chat Export Note

## What is available inside repo

- Structured handoff:
  - `Planning_doc_demo/full_chat_handoff.md`
- Code/runtime/context pack:
  - `superContext/*`

## What is NOT auto-exported

- Verbatim raw chat transcript from Cursor UI cannot be force-exported by agent autonomously.

## If you want raw transcript backup

User should export manually from chat UI (if the current Cursor version supports export), then place it under:

- `Planning_doc_demo/chat_export_raw/`

Suggested naming:

- `chat_export_YYYYMMDD_session_01.md`
- or `.jsonl` if available.

This allows future sessions to consume both structured and raw context.

