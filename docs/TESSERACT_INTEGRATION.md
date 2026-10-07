# Tesseract integration strategy

## Decision

Do not write an OCR engine from scratch. Reuse the UB-Mannheim Windows Tesseract distribution/source as much as practical and keep OCYFROVKA focused on document-specific logic.

## Runtime boundary

The portable package will contain:

```text
Engine/OCR/
  tesseract.exe
  required native DLLs
  tessdata/
    ukr.traineddata
    eng.traineddata
    osd.traineddata
  LICENSES/
```

OCYFROVKA will call the local `tesseract.exe` through a dedicated adapter. This keeps native dependencies isolated and makes a future engine swap possible without rewriting the UI.

## What we reuse

- Tesseract OCR engine itself.
- UB-Mannheim Windows build/runtime layout and dependency packaging patterns.
- Official Tesseract traineddata formats and command-line capabilities.
- Upstream orientation/page-segmentation support where useful.

## What remains ours

- WPF UI and portable startup.
- Input/PDF page model.
- Image preprocessing pipeline and image-quality scoring.
- Layout and table reconstruction.
- Domain dictionaries, fuzzy matching and validation.
- Human review workflow and correction memory.
- SQLite persistence and XLSX export.
- Regression tests against our document corpus.

## Versioning

Tesseract runtime must be pinned to an exact upstream revision/release in the build process. Never silently use "latest" for production packages. The selected revision and hashes of runtime assets will be recorded before the first alpha containing OCR.
