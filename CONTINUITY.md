# CONTINUITY / HANDOFF

**Updated:** 2026-10-07  
**Repository:** `arudichik-source/OCYFROVKA`  
**Active branch:** `stage/03-preprocessing`  
**Latest merged PR:** #2 — Stage 2: document input, image preview and local PDF import  
**Green main commit after Stage 2:** `254128a668ac8821e13cc1720fe28f732fbf297f`  
**Latest verified Stage 3 functional commit:** `286cf4e6223448c6c1177912a0c680413110ed5c`

## Purpose

Read this file first in any continuation, then inspect `stage/03-preprocessing` and the newest GitHub Actions run.

## Fixed project decisions

- Windows x64, C# / WPF / .NET 10.
- Self-contained portable ZIP; no installer/admin rights required.
- Local/offline base workflow; no required paid/cloud OCR services.
- OCR remains replaceable through `IOcrEngine`; Tesseract/UB-Mannheim is the planned first runtime.
- OCR is multilingual and discovers installed `*.traineddata` dynamically.
- Runtime/private data must never enter the public repository.
- Original photos/PDFs are never overwritten by preprocessing.

## Completed

Stage 1 and Stage 2 are merged into `main` and green.

Stage 2 includes image/PDF import, multi-page document model, preview, ordering, non-destructive rotation, Windows PDF rasterization and integration tests.

## Stage 3 — current implementation

Implemented in `stage/03-preprocessing`:

- independent `Ocyfrovka.Imaging` project;
- immutable `GrayImage` model;
- profiles: Auto, Grayscale, HighContrast, Binary, AdaptiveBinary, ShadowCorrected, Sharpened;
- histogram contrast stretch;
- Otsu global threshold;
- adaptive block-local threshold for uneven lighting;
- multiplicative local illumination/shadow normalization;
- 3×3 sharpen;
- 3×3 median denoise primitive;
- OCR-oriented image quality scoring and warnings;
- automatic skew-angle estimation over a bounded ±7° search;
- non-destructive deskew rotation;
- content/page-boundary detection;
- deterministic crop primitive;
- preprocessing diagnostics expose:
  - quality;
  - Otsu threshold;
  - deskew correction angle;
  - detected content bounds;
- WPF Original / Processed switch;
- WPF selection for all current preprocessing profiles;
- processing runs via `Task.Run`, not the UI thread;
- processed preview cache is invalidated after page rotation/removal;
- source input remains unchanged.

## Latest verified CI

Workflow run:

- Run: `37615373572`
- Commit: `286cf4e6223448c6c1177912a0c680413110ed5c`
- Result: **SUCCESS**
- Core/imaging tests: **39/39 PASS**
- Windows PDF integration tests: **2/2 PASS**
- Total: **41/41 PASS**
- Portable Windows x64 publish: **SUCCESS**
- Artifact upload: **SUCCESS**

## Immediate next work

Continue Stage 3 only:

1. perspective correction foundation with deterministic four-corner transform;
2. user-controlled application/preview of detected crop;
3. ensure crop/perspective never modify the original source;
4. add synthetic perspective/crop regression tests;
5. inspect preprocessing behavior on representative non-sensitive synthetic fixtures;
6. run CI;
7. if Stage 3 is fully green, open PR #3 to `main`;
8. only after merged/green Stage 3 begin packaging the real Tesseract runtime in Stage 4.

## Technical notes

- App target: `net10.0-windows10.0.19041.0`.
- `DocumentPage.ImagePath` is the original image or local rendered PDF PNG.
- PDF pages remain under `Workspace/Temp/PdfPages/<hash>/`.
- Current `Auto` profile: deskew → illumination normalization → contrast stretch → sharpen.
- Content bounds are detected and reported but are not silently cropped in Auto.
- Deskew keeps the same canvas size with a white background.
- Deskew search samples dark pixels and caps sampled points for predictable runtime.
- Adaptive/shadow algorithms use block statistics rather than a full-image integral buffer to keep memory bounded on large pages.
- Page rotation from Stage 2 remains metadata only.

## GitHub workflow rule

Use stage branches and PRs; keep `main` releasable. Every meaningful pass ends with CI check, this file updated, then a pause.

## Data safety

Never commit real user documents, runtime databases, private dictionaries, Workspace/Export/Logs, credentials, tokens or certificates.

## Key documentation

- `docs/TECHNICAL_SPECIFICATION.md`
- `docs/ROADMAP.md`
- `docs/TESSERACT_INTEGRATION.md`
- `THIRD_PARTY_NOTICES.md`
