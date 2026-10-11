# CONTINUITY / HANDOFF

**Updated:** 2026-10-11
**Repository:** `arudichik-source/OCYFROVKA`
**Active branch:** `stage/10-processing-queue`
**Last merged PR:** #9 — XLSX, CSV and TXT export
**Green Stage 9 main merge:** `aa451cda447e833860f235bb0629c80949179359`
**Latest verified Stage 10 code:** `2ebf1d8d1c83d028bc97a0bf32b8c539d9d9b362`

## Start here when reconnecting

1. Inspect GitHub branches, latest Stage 10 SHA, open PRs and GitHub Actions.
2. Do not assume Stage 10 CI is green until latest code's runner finishes.
3. Fix compiler/test failures before declaring completion or merging.
4. Continue working on Stage 10 from `stage/10-processing-queue`, not old stage branches.
5. Keep this file updated at major verified checkpoints.

## Product invariants

- Windows x64, .NET 10/WPF portable self-contained folder ZIP, no admin rights or installer.
- Offline OCR via pinned local multilingual Tesseract (Ukrainian, English, Russian, Polish, German + OSD; extensible).
- Original document images/PDFs never overwritten.
- Source OCR word geometry/confidence preserved after review/correction.
- Confirmed user values cannot be silently overwritten by OCR or dictionary.
- Only explicit review allows uncertain fuzzy/memory suggestions to affect data.
- Runtime documents, private dictionaries, SQLite DB, Workspace, Export and Logs are not committed publicly.

## Stage 1–9 status

Merged into `main`. Stage 9 includes local XLSX/CSV/TXT, conservative numeric typing and text identifier protection, optional OCR metadata and WPF export UI. Last Stage 9 PR-check run `38132021279` was SUCCESS; 112 Core + 14 Windows = **126/126 PASS**. Post-merge `main` run `38132164990` was SUCCESS.

## Stage 10 code added

- `src/Ocyfrovka.Queue`: generic `ProcessingQueue<T>` with FIFO execution, bounded parallelism = 1, cancellation, max attempt count, reentrancy guard, per-item status/error/attempt count, retry-failed.
- `QueueJobStatus`: Queued, Running, Completed, Failed, Canceled.
- `QueueJobSnapshot` and `QueueProgress` immutable reporting models.
- `tests/Ocyfrovka.Core.Tests/ProcessingQueueTests.cs`: 7 synthetic scenarios (FIFO, isolation, retry, manual retry, cancel, reentrancy and empty queue).
- WPF `MainWindow`: batch OCR across captured document pages, cancel current subprocess, retry failed pages, progress/status indicators, scrollable queue listing; single-page OCR preserved.
- OCR progress retains local SQLite per-page persistence and protected existing review edits.
- Guard against removal/clear/rotation/import during active batch; stale queue is reset after removal/clear.
- User-facing left-side `Черга` link now focuses queue list.

## Latest verified CI

- Run: `38132632200`, head SHA `2ebf1d8d1c83d028bc97a0bf32b8c539d9d9b362`
- Result: **SUCCESS**, Core/Queue **119/119 PASS**, Windows **14/14 PASS**, total **133/133 PASS**.
- Tesseract smoke test: **v5.5.3.20260724**.
- Portable Windows x64 publish and artifact upload: **SUCCESS**.
- Artifact: `OCYFROVKA_Portable_x64`, ID `11699353228`, size `170920393` bytes.
- Artifact SHA256: `db318bd7a5ab4aaeb89502347ce115b564846808e631047b75642454e3560253`.
- URL: https://github.com/arudichik-source/OCYFROVKA/actions/runs/38132632200
- This green run covers all functional changes; subsequent documentation-only commits need their final check before merge.

## Immediate remaining tasks

1. Open PR #10 and confirm final PR run is green before merging.
2. Verify post-merge main.
3. Start Stage 11 acceptance hardening with synthetic/non-sensitive OCR fixtures and end-to-end batch testing.
4. Consider durable queue recovery and advanced cancellation UX as later refinements.
5. Continue with manual perspective editor and portable release candidate only after verified acceptance checks.

## Data safety

Do not commit original documents, confidential images, private dictionaries, DB, Workspace/Export/Logs, or credentials.
