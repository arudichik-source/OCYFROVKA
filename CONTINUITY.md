# CONTINUITY / HANDOFF

**Updated:** 2026-10-07  
**Repository:** `arudichik-source/OCYFROVKA`  
**Active branch:** `stage/03-preprocessing`  
**Latest merged PR:** #2 — Stage 2: document input, image preview and local PDF import  
**Green main commit after Stage 2:** `254128a668ac8821e13cc1720fe28f732fbf297f`  
**Latest verified Stage 3 functional commit:** `934e3f685e330c3e29450d314ece302770b01e1d`

## Purpose

Read this file first in any continuation, then inspect `stage/03-preprocessing` and the newest GitHub Actions run.

## Fixed project decisions

- Windows x64, C# / WPF / .NET 10.
- Self-contained portable ZIP; no installer/admin rights required.
- Local/offline base workflow; no required paid/cloud OCR services.
- OCR remains replaceable through `IOcrEngine`; Tesseract/UB-Mannheim is the planned first runtime.
- OCR is multilingual and discovers installed `*.traineddata` dynamically.
- Runtime/private data must never enter the public repository.

## Completed

Stage 1 and Stage 2 are merged into `main` and green.

Stage 2 includes image/PDF import, multi-page document model, preview, ordering, non-destructive rotation, Windows PDF rasterization, and integration tests.

## Stage 3 — current implementation

Implemented in `stage/03-preprocessing`:

- new independent `Ocyfrovka.Imaging` project;
- immutable `GrayImage` pixel model;
- preprocessing profiles: Auto, Grayscale, HighContrast, Binary, Sharpened;
- deterministic grayscale pipeline boundary;
- histogram-based contrast stretching;
- Otsu global thresholding;
- binary output;
- 3×3 sharpen convolution;
- 3×3 median denoise foundation;
- image quality metrics:
  - brightness;
  - contrast;
  - edge/detail score;
  - overall OCR-oriented score;
  - warnings for dark/overexposed/flat/blurred input;
- WPF bridge converting frozen previews to/from the pure imaging model;
- processing runs via `Task.Run`, not on the UI thread;
- per-page processed preview cache;
- processed cache invalidates after page rotation/removal;
- UI controls for:
  - Original;
  - Processed;
  - preprocessing profile;
  - Process;
- Original input is never overwritten;
- deterministic preprocessing unit tests.

## Latest verified CI

Workflow run:

- Run: `37613938568`
- Commit: `934e3f685e330c3e29450d314ece302770b01e1d`
- Result: **SUCCESS**
- Core/imaging tests: **32/32 PASS**
- Windows PDF integration tests: **2/2 PASS**
- Total: **34/34 PASS**
- Portable Windows x64 publish: **SUCCESS**
- Artifact upload: **SUCCESS**

A documentation-only roadmap commit followed this verified functional commit.

## Immediate next work

Continue Stage 3 only:

1. automatic skew-angle estimation;
2. non-destructive deskew transform;
3. page-boundary / crop detection foundation;
4. adaptive/local threshold for uneven lighting;
5. shadow/illumination normalization;
6. improve noise handling using OCR-oriented heuristics;
7. add tests for deskew/crop/adaptive-threshold determinism;
8. run CI;
9. when full Stage 3 is green, open PR #3.

Do not package the real Tesseract runtime or begin production OCR execution before Stage 3 is complete.

## Technical notes

- App target: `net10.0-windows10.0.19041.0`.
- `DocumentPage.ImagePath` is the original image or local rendered PDF PNG.
- PDF pages remain under `Workspace/Temp/PdfPages/<hash>/`.
- Current `Auto` profile = contrast stretch + sharpen.
- Otsu threshold is calculated and surfaced in processed-preview diagnostics.
- Median denoise exists as a deterministic primitive but is not forced into Auto yet.
- Page rotation remains metadata only.

## GitHub workflow rule

Use stage branches and PRs; keep `main` releasable. Every meaningful pass ends with CI check, this file updated, then a pause.

## Data safety

Never commit real user documents, runtime databases, private dictionaries, Workspace/Export/Logs, credentials, tokens or certificates.

## Key documentation

- `docs/TECHNICAL_SPECIFICATION.md`
- `docs/ROADMAP.md`
- `docs/TESSERACT_INTEGRATION.md`
- `THIRD_PARTY_NOTICES.md`
