# CONTINUITY / HANDOFF

**Updated:** 2026-10-07  
**Repository:** `arudichik-source/OCYFROVKA`  
**Active branch:** `stage/02-input`  
**Open pull request:** #2 — Stage 2: document input, image preview and local PDF import  
**Stage 1 main commit:** `23a8224ae1950d1394822b22ae6d4afda9e53da5`  
**Latest verified Stage 2 code commit:** `23cd48dccfbb12ff00e1d8ab2790f903a5a919f2`

## Purpose

This file is the restart point for the next ChatGPT branch/conversation. Read it first, then inspect the active branch, PR #2 and the newest GitHub Actions run before changing code.

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
- Portable workspace creation.
- Core OCR contracts and first models.
- `Ocyfrovka.Ocr.Tesseract` adapter.
- Local Tesseract process wrapper producing TSV/word coordinates.
- Dynamic multilingual `TesseractLanguageCatalog`.
- First text normalizer and tests.
- GitHub Actions Windows x64 self-contained publish.
- Technical specification v0.2, roadmap, third-party notices and fail-fast build scripts.

## Stage 2 — implemented

Stage 2 input/document layer is complete in `stage/02-input` and PR #2 is open.

Implemented:

- `InputFileKind` and `InputFileClassifier`.
- `DocumentPage` and `DigitizationDocument`.
- JPG/JPEG/PNG/BMP/TIF/TIFF import.
- Multi-frame TIFF expansion into separate pages.
- Duplicate prevention.
- Page add/remove/clear.
- Page ordering with move up/down.
- Non-destructive 90° left/right rotation.
- Selected-page image preview.
- PDF classification and real local PDF page import.
- PDF rasterization through Windows `Windows.Data.Pdf`.
- Application target updated to `net10.0-windows10.0.19041.0` so the built-in Windows PDF API is available.
- Each PDF page is rendered locally to PNG under `Workspace/Temp/PdfPages/<hash>/`.
- Original PDF files are never modified.
- PDF-backed `DocumentPage` keeps the original PDF path/page index plus a private rendered working image path.
- Clear local errors for invalid/unreadable/password-protected or unsupported PDF input.
- PDF import is asynchronous in the WPF UI.
- Synthetic PDF integration test verifies actual Windows PDF rasterization and PNG output.
- Invalid-PDF integration test verifies the user-facing failure path.
- CI/local build runs both core tests and Windows integration tests.

## Latest verified CI

Workflow run:

- Run: `37609784340`
- Commit: `23cd48dccfbb12ff00e1d8ab2790f903a5a919f2`
- Result: **SUCCESS**
- Core tests: **26/26 PASS**
- Windows PDF integration tests: **2/2 PASS**
- Total tests: **28/28 PASS**
- Portable Windows x64 publish: **SUCCESS**
- Artifact upload: **SUCCESS**

A later documentation-only commit marks PDF input complete in `docs/ROADMAP.md`. PR #2 must be checked again at its final head before merge.

## Immediate next work

1. Check the final PR #2 GitHub Actions run.
2. If PR #2 is green, merge it into `main`.
3. Verify the post-merge `main` workflow.
4. Create `stage/03-preprocessing` from the verified green `main`.
5. Start Stage 3 only on that new branch:
   - page-boundary/crop foundation;
   - deskew;
   - grayscale;
   - threshold/high-contrast variants;
   - sharpen/noise/shadow handling;
   - image-quality score;
   - preserve original vs processed preview.

Do not package the real Tesseract Windows runtime or move into OCR execution until Stage 3 preprocessing is stable.

## Known technical notes

- xUnit global usings are present in both test projects.
- `WorkspaceInitializer.cs` has explicit `System.IO` imports.
- CI/local PowerShell scripts explicitly fail on non-zero `dotnet` exit codes.
- Current image preview uses WPF `BitmapDecoder`.
- PDF rendering uses Windows' built-in PDF API, not a cloud service.
- PDF rasterization is currently capped at 6000 px on the longest side and uses a 2× render scale.
- Page rotation is metadata only. Original input files are never rewritten.
- Multi-frame TIFF pages use `SourceFrameIndex`; PDF pages use the same index plus `RenderedImagePath`.

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

Synthetic non-sensitive test fixtures may be generated by tests in temporary directories but should not contain user data.

See `.gitignore`.

## Key documentation

- `docs/TECHNICAL_SPECIFICATION.md`
- `docs/ROADMAP.md`
- `docs/TESSERACT_INTEGRATION.md`
- `THIRD_PARTY_NOTICES.md`
