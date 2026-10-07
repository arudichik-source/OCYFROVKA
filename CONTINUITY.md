# CONTINUITY / HANDOFF

**Updated:** 2026-10-07  
**Repository:** `arudichik-source/OCYFROVKA`  
**Active branch:** `stage/02-input`  
**Latest merged PR:** #1 — Stage 1: bootstrap portable WPF application  
**Main commit after merge:** `23a8224ae1950d1394822b22ae6d4afda9e53da5`

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

## Completed — Stage 1

Stage 1 is merged into `main` and verified green.

Implemented:

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
- Fail-fast checks in CI and local build script.
- Persistent handoff file.

## Latest verified CI

Main workflow run:

- Run: `37606740259`
- Commit: `23a8224ae1950d1394822b22ae6d4afda9e53da5`
- Result: **SUCCESS**
- Unit tests: **7/7 PASS**
- Portable build artifact produced successfully.

Verified artifact:

- Name: `OCYFROVKA_Portable_x64`
- ZIP artifact size: about 65 MB
- Artifact ID: `11474349022`

## Current branch / Stage 2 goal

Branch `stage/02-input` was created from the green `main`.

Stage 2 scope:

1. Real document/page domain model.
2. Import JPG/JPEG/PNG/BMP/TIF/TIFF.
3. Image preview.
4. Multi-page document handling.
5. Page ordering.
6. 90° page rotation without destroying the original source file.
7. PDF page extraction.
8. Input validation and clear unsupported/corrupt-file errors.
9. Tests for the input model and page operations.
10. Open PR for Stage 2 only after CI is green.

Do not start image preprocessing or package the real Tesseract runtime until Stage 2 input is stable.

## Known technical notes

- A prior CI failure from missing xUnit global usings was fixed with `tests/Ocyfrovka.Core.Tests/GlobalUsings.cs`.
- A later publish failure from missing `System.IO` imports in `WorkspaceInitializer.cs` was fixed.
- CI/local PowerShell scripts now explicitly fail on non-zero `dotnet` exit codes.
- Current `main` is buildable and produces a self-contained Windows x64 ZIP.

## GitHub workflow rule

Use stage branches and PRs. Keep `main` releasable. Every meaningful pass must end by:

1. running/checking GitHub Actions;
2. fixing failures found in that pass where practical;
3. updating this file with exact branch, PR, current state, latest failure/success and next task;
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
