# CONTINUITY / HANDOFF

**Updated:** 2026-10-07  
**Repository:** `arudichik-source/OCYFROVKA`  
**Active branch:** `stage/02-input`  
**Latest merged PR:** #1 — Stage 1: bootstrap portable WPF application  
**Main commit after merge:** `23a8224ae1950d1394822b22ae6d4afda9e53da5`  
**Latest functional Stage 2 code commit:** `36fa127c3adb77b5c63f917738bab3b64bb456bf`

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
- Drag & Drop / file picker shell.
- Portable workspace creation under the application directory.
- Core OCR contracts and first models.
- `Ocyfrovka.Ocr.Tesseract` adapter project.
- Local Tesseract process wrapper producing TSV/word coordinates.
- Dynamic multilingual `TesseractLanguageCatalog`.
- Validation of missing requested language models.
- First text normalizer and tests.
- GitHub Actions Windows x64 self-contained publish.
- Technical specification v0.2, roadmap and third-party notices.
- Fail-fast CI/local PowerShell build scripts.
- Persistent handoff file.

## Current Stage 2 implementation

The current branch already implements the image-input half of Stage 2:

- `InputFileKind` and `InputFileClassifier`.
- Real `DocumentPage` model with:
  - source path;
  - source frame index;
  - source dimensions;
  - stable page ID;
  - non-destructive rotation metadata.
- `DigitizationDocument` with:
  - duplicate prevention;
  - add/remove;
  - move page up/down;
  - clear.
- JPG/JPEG/PNG/BMP/TIF/TIFF loading through WPF `BitmapDecoder`.
- Multi-frame TIFF is expanded into individual document pages.
- Image preview is loaded with `BitmapCacheOption.OnLoad` so source files are not held open.
- Preview respects 90° rotation without modifying the source image.
- WPF UI now has:
  - page list;
  - selected-page preview;
  - page count;
  - move up/down;
  - rotate left/right;
  - remove page;
  - clear document.
- PDF is recognized by the classifier but intentionally deferred until the next Stage 2 pass.
- Unsupported/corrupt image files are reported to the user instead of silently failing.
- Roadmap marks image input, multi-page model and page ordering/rotation complete.

## Latest verified CI

Stage 2 workflow run:

- Run: `37607998683`
- Commit: `36fa127c3adb77b5c63f917738bab3b64bb456bf`
- Result: **SUCCESS**
- Unit tests: **23/23 PASS**
- Portable Windows x64 publish: **SUCCESS**
- Artifact upload: **SUCCESS**

Previous green `main` remains at commit `23a8224ae1950d1394822b22ae6d4afda9e53da5`.

## Immediate next work

Continue Stage 2 only:

1. Add real PDF page extraction.
2. Convert each PDF page into a document page/previewable image without changing the original PDF.
3. Keep all PDF processing local/offline.
4. Add validation/errors for invalid, encrypted or unreadable PDFs.
5. Add tests for PDF page import where practical.
6. Re-run GitHub Actions.
7. When the entire Stage 2 scope is green, open PR #2 to `main`.

Do not begin Stage 3 preprocessing or package the real Tesseract runtime until Stage 2 is complete and green.

## Known technical notes

- xUnit global usings are fixed in `tests/Ocyfrovka.Core.Tests/GlobalUsings.cs`.
- `WorkspaceInitializer.cs` has explicit `System.IO` imports.
- CI/local PowerShell scripts explicitly fail on non-zero `dotnet` exit codes.
- Current image preview is intentionally WPF-specific; core document/page logic remains UI-independent.
- Page rotation is metadata only. The original file is never rewritten.
- Multi-frame TIFF pages are identified by `SourceFrameIndex`.

## GitHub workflow rule

Use stage branches and PRs. Keep `main` releasable. Every meaningful pass must end by:

1. running/checking GitHub Actions;
2. fixing failures found in that pass where practical;
3. updating this file with exact branch, current state, latest failure/success and next task;
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
