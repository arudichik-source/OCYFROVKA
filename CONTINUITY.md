# CONTINUITY / HANDOFF

**Updated:** 2026-10-07  
**Repository:** `arudichik-source/OCYFROVKA`  
**Active branch:** `stage/04-local-ocr`  
**Latest merged PR:** #3 — Stage 3: deterministic image preprocessing pipeline  
**Green main commit after Stage 3:** `76eca9c8c8191c7311ecd2f7cc921990f9acecb7`  
**Latest verified Stage 4 functional commit:** `55181fc11a8ffcad0c8d04c572f9ffbd2d64b4d9`

## Purpose

Read this file first in any continuation, then inspect `stage/04-local-ocr`, its PR if present, and the newest GitHub Actions run.

## Fixed project decisions

- Windows x64, C# / WPF / .NET 10.
- Self-contained portable ZIP; no installer/admin rights required on the user's PC.
- Base operation is local/offline; no required OCR API keys or cloud services.
- OCR remains replaceable behind `IOcrEngine`.
- Original photos/PDFs are never overwritten.
- Runtime/private document data must never enter the public repository.
- Every third-party OCR binary/model used in a production portable build must be pinned and integrity-checked.

## Completed

Stages 1, 2 and 3 are merged into `main` and green.

## Stage 4 — implemented and verified

- pinned Tesseract Windows x64 runtime `5.5.3.20260724`;
- runtime installer SHA256 locked;
- portable build performs a silent temporary install and copies runtime files into `Engine/OCR`;
- `ukr`, `eng`, `rus`, `pol`, `deu` and `osd` are packaged;
- all model source commits and SHA256 hashes are locked;
- build rejects missing/unexpected hashes;
- license files are copied into the portable package;
- runtime smoke checks run `tesseract --version` and `--list-langs`;
- dynamic language discovery at application startup;
- editable language expression selector with common combinations;
- real local `TesseractOcrEngine` execution;
- process arguments use `ProcessStartInfo.ArgumentList`;
- cancellation kills the OCR process tree;
- original or processed preview can be materialized to a private PNG for OCR;
- recognized text is shown inside the WPF UI;
- TSV parser preserves lines, word confidence and word coordinates;
- clear errors for missing runtime/models;
- OCR input stays under `Workspace/Temp/OcrInput`.

## Latest verified CI

Strict locked Stage 4 workflow:

- Run: `37620614213`
- Commit: `55181fc11a8ffcad0c8d04c572f9ffbd2d64b4d9`
- Result: **SUCCESS**
- Core/OCR/Imaging tests: **45/45 PASS**
- Windows integration tests: **5/5 PASS**
- Total: **50/50 PASS**
- Tesseract reported: **v5.5.3.20260724**
- Available bundled language models: **6**
- Portable Windows x64 publish: **SUCCESS**
- Artifact upload: **SUCCESS**
- Artifact: `OCYFROVKA_Portable_x64`
- Artifact ID: `11481193899`
- Artifact size: `166805119` bytes (~166.8 MB)

## Pinned OCR model SHA256

- `ukr` — `1277f6e3b6f707063a92d40e7678e7f57154e8414e328e340be9ee9275eea9c8`
- `eng` — `8280aed0782fe27257a68ea10fe7ef324ca0f8d85bd2fd145d1c2b560bcb66ba`
- `rus` — `b617eb6830ffabaaa795dd87ea7fd251adfe9cf0efe05eb9a2e8128b7728d6b6`
- `pol` — `e80cc4cefbdface06e9223f43f089556b9dcf104020fbc0a200f6863c57d4405`
- `deu` — `8407331d6aa0229dc927685c01a7938fc5a641d1a9524f74838cdac599f0d06e`
- `osd` — `e19f2ae860792fdf372cf48d8ce70ae5da3c4052962fe22e9de1f680c374bb0e`

## Immediate next work

1. Open/check PR #4 for Stage 4.
2. Merge PR #4 after its final green check.
3. Verify post-merge `main`.
4. Create `stage/05-layout-tables`.
5. Stage 5:
   - normalize OCR words into line objects;
   - group words into likely rows;
   - infer column boundaries from X geometry;
   - reconstruct cells;
   - preserve confidence/provenance from source words;
   - add raw-text / structured-table preview;
   - add deterministic synthetic regression tests.

## Technical notes

- App target: `net10.0-windows10.0.19041.0`.
- Runtime lock: `build/ocr-runtime.lock.json`.
- Runtime packager: `build/Prepare-OcrRuntime.ps1`.
- OCR executable path: `Engine/OCR/tesseract.exe`.
- OCR data path: `Engine/OCR/tessdata`.
- PDF pages remain under `Workspace/Temp/PdfPages/<hash>/`.
- OCR temporary PNGs remain under `Workspace/Temp/OcrInput`.
- Stage 3 Auto preprocessing remains deskew → illumination normalization → contrast stretch → sharpen.
- OCR of the processed preview uses exactly the preview chosen by the user.

## GitHub workflow rule

Use stage branches and PRs; keep `main` releasable. Update this file after every meaningful development block.

## Data safety

Never commit real user documents, runtime databases, private dictionaries, Workspace/Export/Logs, credentials, tokens or certificates.

## Key documentation

- `docs/TECHNICAL_SPECIFICATION.md`
- `docs/ROADMAP.md`
- `docs/TESSERACT_INTEGRATION.md`
- `THIRD_PARTY_NOTICES.md`
