# CONTINUITY / HANDOFF

**Updated:** 2026-10-11
**Repository:** `arudichik-source/OCYFROVKA`
**Active branch:** `stage/11-acceptance-tests`
**Last merged PR:** #10 — cancellable batch OCR queue
**Green main Stage 10 merge:** `ab201a0be6c820cead2919c7b92fc7751edf26a8`

## Resume instructions

Always read this file, then query live GitHub for current branch/PR status and latest workflow. Do not redo stages already merged. Stage 11 is **in progress**, and must not be described as completed until actual OCR smoke test and full Windows portable build pass.

## Product invariants

- Standalone Windows x64 .NET 10/WPF self-contained portable ZIP; no installation/admin required.
- All OCR offline; pinned Tesseract runtime and multilingual data.
- Original scan remains unchanged.
- User/confirmed review values protected from any re-recognition or automatic fuzzy changes.
- OCR provenance and confidence remain available.
- Runtime documents, databases, private dictionaries and logs are not stored in the public repository.

## Merged stages 1–10

1–8: document input and PDF, preprocessing/perspective foundation, multilingual offline OCR, layout reconstruction, protected review, local dictionaries/SmartCorrector, SQLite autosave and correction memory.

9: XLSX/CSV/TXT export of reviewed tables, conservative numeric typing, optional OCR metadata.

10: `Ocyfrovka.Queue` with sequential FIFO batch OCR, cancellation/retries/error isolation, WPF controls/status list and SQLite persistence integration.

Latest Stage 10 functional CI: run `38132632200`, **119/119 Core + 14/14 Windows = 133/133 PASS**; portable x64 ZIP uploaded. Stage 10 PR #10 was merged, and post-merge main run `38132926440` completed SUCCESS.

## Stage 11 work in branch

- Added `build/Smoke-Test-Local-Ocr.ps1`: uses Windows System.Drawing to generate disposable synthetic images. No private files or network upload.
- Generates `INVOICE 12345` and `ТЕСТ 67890` fixture images.
- Runs **bundled portable** `tesseract.exe` against English and Ukrainian samples; requires expected digit strings and zero exit code.
- Updated `.github/workflows/build-windows.yml` to run acceptance script after the bundled OCR runtime is packaged and **before** the portable ZIP is built.
- Earlier Stage 4 smoke tested only `--version`/`--list-langs`. This stage checks real recognizer execution.

## Current CI / next steps

Latest known Stage 11 workflow: `38133022416` (possibly superseded after this handoff commit). Read latest job logs.

1. Fix System.Drawing/Tesseract or OCR recognition fixture issues surfaced by CI.
2. Keep script deterministic and independent of private documents.
3. Add follow-up test coverage for real pipeline and batch queue as feasible.
4. Only once Stage 11 checks are green, update roadmap, open PR #11, merge after green PR check and verify main.
5. Prepare portable release/production-check workflow after Stage 11 acceptance baseline.

## Project links

- Repository: https://github.com/arudichik-source/OCYFROVKA
- Roadmap: `docs/ROADMAP.md`
- Stage 10 merged PR: https://github.com/arudichik-source/OCYFROVKA/pull/10

## Safety

No real personal/private documents, credentials, SQLite databases or OCR user files may be committed to the public repository.
