# CONTINUITY / HANDOFF

**Updated:** 2026-10-07  
**Repository:** `arudichik-source/OCYFROVKA`  
**Active branch:** `stage/03-preprocessing`  
**Latest merged PR:** #2 — Stage 2: document input, image preview and local PDF import  
**Green main commit after Stage 2:** `254128a668ac8821e13cc1720fe28f732fbf297f`  
**Latest verified Stage 3 functional commit:** `d9369dfa7b7fb072ab0e5f1f419ff4ce5cb98178`

## Purpose

Read this file first in any continuation, then inspect `stage/03-preprocessing`, PR #3 if present, and the newest GitHub Actions run.

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

Stage 3 implementation is now complete for the preprocessing milestone in `stage/03-preprocessing`.

## Stage 3 — implemented

- independent `Ocyfrovka.Imaging` project;
- immutable `GrayImage` model;
- Auto, Grayscale, HighContrast, Binary, AdaptiveBinary, ShadowCorrected and Sharpened profiles;
- histogram contrast stretch;
- Otsu global threshold;
- adaptive/local threshold for uneven lighting;
- local illumination/shadow normalization;
- 3×3 sharpen and median-denoise primitive;
- OCR-oriented quality score and warnings;
- automatic skew-angle estimation;
- non-destructive deskew;
- content/page-boundary detection;
- deterministic crop primitive;
- reversible detected-crop preview in WPF;
- deterministic four-corner perspective correction using a solved projective homography and inverse bilinear sampling;
- perspective validation for degenerate/out-of-frame input;
- Original / Processed UI switch;
- processing outside the UI thread;
- processed preview cache invalidation after source-page rotation/removal;
- original source files remain unchanged;
- synthetic regression tests for deskew, crop, perspective, adaptive threshold and illumination normalization;
- Windows integration tests for reversible crop preview.

The interactive four-corner editor is intentionally listed as a later optional UI refinement. The perspective transform engine itself is implemented and tested, so it is not a blocker for Stage 4 OCR.

## Latest verified CI

Workflow run:

- Run: `37616986805`
- Commit: `d9369dfa7b7fb072ab0e5f1f419ff4ce5cb98178`
- Result: **SUCCESS**
- Core/imaging tests: **43/43 PASS**
- Windows integration tests: **4/4 PASS**
- Total: **47/47 PASS**
- Portable Windows x64 publish: **SUCCESS**
- Artifact upload: **SUCCESS**

## Immediate next work

1. Check the final documentation-only Stage 3 workflow.
2. Open/check PR #3 to `main`.
3. On the next user-approved pass, if PR #3 remains green, merge it.
4. Verify post-merge `main`.
5. Create `stage/04-local-ocr`.
6. Stage 4:
   - pin the exact UB-Mannheim Tesseract Windows runtime;
   - package runtime DLL dependencies into the portable artifact;
   - pin `ukr`, `eng`, `rus`, `pol`, `deu` and `osd` traineddata with hashes;
   - add UI language selector driven by installed models;
   - execute real OCR through the existing `TesseractOcrEngine`;
   - verify text/confidence/word-coordinate output;
   - keep all OCR local/offline.

## Technical notes

- App target: `net10.0-windows10.0.19041.0`.
- `DocumentPage.ImagePath` is the original image or local rendered PDF PNG.
- PDF pages remain under `Workspace/Temp/PdfPages/<hash>/`.
- Current Auto profile: deskew → illumination normalization → contrast stretch → sharpen.
- Detected crop is never silently applied; the user explicitly chooses crop preview and can restore the full processed frame.
- Perspective correction accepts an explicit four-corner `ImageQuad`; it never mutates the source.
- Deskew keeps the same canvas size with a white background.
- Adaptive/shadow algorithms use bounded block statistics.
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
