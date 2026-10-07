# CONTINUITY / HANDOFF

**Updated:** 2026-10-07  
**Repository:** `arudichik-source/OCYFROVKA`  
**Active branch:** `stage/01-bootstrap`  
**Pull request:** #1 — Stage 1: bootstrap portable WPF application

## Purpose

This file is the restart point for the next ChatGPT branch/conversation. Read this file first, then inspect the active branch and the latest GitHub Actions run before changing code.

## Fixed project decisions

- Product name: **ОЦИФРОВКА**.
- Target: Windows x64.
- Stack: C# / WPF / .NET 10.
- Delivery: self-contained portable ZIP, no installer required.
- Must work without administrator rights.
- Base operation is local/offline, no required API keys or paid cloud services.
- OCR engine is not being written from scratch. Reuse Tesseract/UB-Mannheim runtime through an isolated adapter.
- OCR architecture must remain replaceable through `IOcrEngine`.
- OCR is multilingual. Installed `*.traineddata` files are discovered dynamically.
- Planned base language pack: `ukr`, `eng`, `rus`, `pol`, `deu` + `osd`.
- Mixed language expressions such as `ukr+eng` are supported.
- Runtime/private data must not enter the public repository.

## Current implementation

Stage 1 bootstrap currently contains:

- WPF application skeleton and dark initial UI.
- Drag & Drop / file picker shell for image/PDF input.
- Portable workspace creation under the application directory.
- Core document and OCR models.
- `IOcrEngine`, `OcrRequest`, `OcrResult`, word coordinates/confidence.
- `Ocyfrovka.Ocr.Tesseract` adapter project.
- Local Tesseract process wrapper producing TSV/word coordinates.
- Dynamic `TesseractLanguageCatalog`.
- Validation of missing requested language models.
- First text normalizer and unit tests.
- GitHub Actions Windows x64 self-contained publish.
- Technical specification v0.2 and roadmap.
- Third-party licensing/notice notes.

## Current CI issue / last action

A previous workflow run failed during unit-test compilation because xUnit attributes were not in scope. The fix is to add `tests/Ocyfrovka.Core.Tests/GlobalUsings.cs` with `global using Xunit;`.

After that commit, verify the newest workflow run. Do not move to the next stage until CI is green.

## Immediate next work after green CI

1. Finalize Stage 1 and merge PR #1 to `main`.
2. Start Stage 2 on a new branch (recommended: `stage/02-input`).
3. Implement real page/document input model.
4. Implement image preview, page ordering and rotation.
5. Add PDF page extraction.
6. Then Stage 3 preprocessing.
7. Stage 4 packages/pins the actual portable Tesseract Windows runtime and multilingual traineddata.

## GitHub workflow rule

Use feature/stage branches and PRs. Keep `main` releasable. Every meaningful pass must end by:

1. running/checking GitHub Actions;
2. fixing failures found in that pass where practical;
3. updating this file with the exact active branch, PR, current state, latest failure/success and next task;
4. stopping so the user can explicitly request the next pass.

## Data safety

Never commit:

- real user JPG/JPEG/PNG/TIF/TIFF/PDF documents;
- SQLite runtime databases;
- private dictionaries;
- Workspace / Export / Logs;
- credentials, tokens or certificates.

See `.gitignore`.

## Key documentation

- `docs/TECHNICAL_SPECIFICATION.md`
- `docs/ROADMAP.md`
- `docs/TESSERACT_INTEGRATION.md`
- `THIRD_PARTY_NOTICES.md`
