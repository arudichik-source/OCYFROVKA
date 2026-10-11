# CONTINUITY / HANDOFF

**Updated:** 2026-10-11
**Repository:** `arudichik-source/OCYFROVKA`
**Active branch:** `stage/10-processing-queue`
**Last merged PR:** #9 — XLSX, CSV and TXT export
**Green Stage 9 main merge:** `aa451cda447e833860f235bb0629c80949179359`
**Latest Stage 10 functional checkpoint before handoff:** `6d0f7b92adb6907e2b3e41853591959848b8e8e9`

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

## Last known CI

Stage 10 is still **under verification**, NOT yet merged. Check the latest `stage/10-processing-queue` push workflow and its logs. The most recently inspected workflow was `38132584117` but may be superseded after this document update.

## Immediate remaining tasks

1. Check Stage 10 CI; fix any compile/test errors and confirm Tesseract/runtime/portable publish.
2. Add integration tests exercising queue progress and selected-page review protection.
3. Consider better cancellation/retry UX and safe queue recovery.
4. Mark Stage 10 complete only after green CI, then PR #10, green PR checks and merge.
5. Following Stage 10: manual perspective editor, non-sensitive OCR fixture-based regression, production check / portable RC.

## Data safety

Do not commit original documents, confidential images, private dictionaries, DB, Workspace/Export/Logs, or credentials.
