# CONTINUITY / HANDOFF

**Updated:** 2026-10-07  
**Repository:** `arudichik-source/OCYFROVKA`  
**Active branch:** `stage/03-preprocessing`  
**Latest merged PR:** #2 — Stage 2: document input, image preview and local PDF import  
**Green main commit after Stage 2:** `254128a668ac8821e13cc1720fe28f732fbf297f`

## Purpose

This file is the restart point for the next ChatGPT branch/conversation. Read it first, then inspect `stage/03-preprocessing` and the newest GitHub Actions run before changing code.

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

Stage 1 is merged and green.

Implemented:

- WPF application skeleton and portable workspace.
- Core OCR contracts/models.
- Tesseract adapter foundation.
- Multilingual model discovery.
- Initial text normalization/tests.
- Windows x64 portable CI publish.
- Project documentation and fail-fast build scripts.

## Completed — Stage 2

Stage 2 is merged into `main` through PR #2 and verified green.

Implemented:

- JPG/JPEG/PNG/BMP/TIF/TIFF input.
- Multi-frame TIFF expansion.
- `DocumentPage` / `DigitizationDocument` model.
- Duplicate prevention.
- Page add/remove/clear and ordering.
- Non-destructive 90° rotation.
- Selected-page preview.
- Real local PDF import with Windows `Windows.Data.Pdf`.
- PDF pages rendered to local PNG under `Workspace/Temp/PdfPages/<hash>/`.
- Original PDFs/images are never rewritten.
- PDF-backed pages preserve original PDF path/page index plus private rendered working image.
- Async PDF import in WPF.
- Clear invalid/unreadable/password-protected PDF error path.
- Windows PDF integration tests with generated synthetic PDF.
- Core + Windows integration tests in CI/local build.

## Latest verified CI

Post-merge `main` workflow:

- Run: `37610356560`
- Commit: `254128a668ac8821e13cc1720fe28f732fbf297f`
- Result: **SUCCESS**
- Core tests: **26/26 PASS**
- Windows PDF integration tests: **2/2 PASS**
- Total: **28/28 PASS**
- Portable Windows x64 publish: **SUCCESS**
- Artifact upload: **SUCCESS**

Final PR #2 head checks were also green before merge.

## Current branch / Stage 3 goal

Branch `stage/03-preprocessing` was created from the verified green Stage 2 `main`.

Stage 3 scope:

1. Introduce a dedicated image preprocessing abstraction/project instead of placing processing logic in the WPF window.
2. Preserve original and processed versions separately.
3. Implement grayscale.
4. Implement high-contrast / threshold variants.
5. Implement sharpen.
6. Add basic noise/shadow handling where reliable.
7. Add deskew foundation and automatic skew-angle estimation.
8. Add page-boundary/crop foundation without destructive edits.
9. Add image-quality scoring/warnings.
10. Add UI switch/preview for Original vs Processed.
11. Add unit/integration tests for deterministic transforms.
12. Keep long operations off the UI thread.
13. When Stage 3 is fully green, open PR #3 to `main`.

Do not package the real Tesseract Windows runtime or begin production OCR execution until preprocessing is stable.

## Known technical notes

- Application target: `net10.0-windows10.0.19041.0`.
- Current image preview uses WPF `BitmapDecoder`.
- PDF rasterization is capped at 6000 px on the longest side and uses a 2× render scale.
- Page rotation remains metadata only.
- `DocumentPage.ImagePath` points to the original image or the rendered PDF working PNG.
- Multi-frame TIFF pages use `SourceFrameIndex`.
- Runtime folders and user data remain outside Git.

## GitHub workflow rule

Use stage branches and PRs. Keep `main` releasable. Every meaningful pass must end by:

1. running/checking GitHub Actions;
2. fixing failures found in that pass where practical;
3. updating this file with exact branch, PR/current state, latest success/failure and next task;
4. stopping so the user can explicitly request the next pass.

## Data safety

Never commit:

- real user JPG/JPEG/PNG/TIF/TIFF/PDF documents;
- SQLite runtime databases;
- private dictionaries;
- Workspace / Export / Logs;
- credentials, tokens or certificates.

Synthetic non-sensitive fixtures generated during tests are allowed.

See `.gitignore`.

## Key documentation

- `docs/TECHNICAL_SPECIFICATION.md`
- `docs/ROADMAP.md`
- `docs/TESSERACT_INTEGRATION.md`
- `THIRD_PARTY_NOTICES.md`
