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

## Verified Stage 11 acceptance baseline

- Latest green workflow run: `38133268883`.
- Functional code SHA: `518e7b883cde01fb6d190f22e4a6a9e82b35337d`.
- Core + Queue tests **119/119 PASS**, Windows tests **14/14 PASS** = **133/133 PASS**.
- English synthetic OCR output: `INVOICE 12345`; TSV digit-word geometry **274×73**, confidence **96.928894**.
- Ukrainian synthetic OCR output: `ТЕСТ 67890`; TSV digit-word geometry **276×74**, confidence **95.0196**.
- All recognition ran completely offline using the packaged Windows Tesseract binary and supplied language models.
- Portable x64 publish/upload **SUCCESS**.
- Artifact: `OCYFROVKA_Portable_x64`, ID `11699479498`.
- Uploaded SHA256: `bbd031fd03ffc8093d7436e9e138a42842b33b8e4983d7854ce43098b388da4f`.
- Run: https://github.com/arudichik-source/OCYFROVKA/actions/runs/38133268883

## Immediate next work

1. Open PR #11 for green bilingual OCR/TSV acceptance baseline.
2. Merge only after green final PR check and verify main.
3. Continue additional acceptance hardening (synthetic table/fixture-based tests and real batch queue end-to-end behavior).
4. Add portable production self-check and release candidate packaging.
5. Leave manual perspective corner editor as optional later UI improvement.

## Project links

- Repository: https://github.com/arudichik-source/OCYFROVKA
- Roadmap: `docs/ROADMAP.md`
- Stage 10 merged PR: https://github.com/arudichik-source/OCYFROVKA/pull/10

## Safety

No real personal/private documents, credentials, SQLite databases or OCR user files may be committed to the public repository.
