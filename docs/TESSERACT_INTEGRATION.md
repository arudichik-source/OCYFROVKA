# Tesseract integration strategy

## Decision

OCYFROVKA uses a pinned local Tesseract Windows runtime rather than a cloud OCR API. OCR remains behind `IOcrEngine`, so the engine can be replaced later without rewriting the document workflow or UI.

## Pinned runtime

Portable builds use:

- Tesseract: `5.5.3.20260724`
- release tag: `5.5.3`
- Windows x64 installer SHA256:
  `bee9e3434bd94fd65387d9be28cd467a41f61b1275383b55b0f59a1331270ae4`

The build downloads the exact pinned installer, verifies its SHA256, performs a silent install into a temporary build directory, and copies the resulting runtime into the portable package. The user's PC does not run an installer.

The canonical lock file is `build/ocr-runtime.lock.json`.

## Portable runtime boundary

```text
Engine/OCR/
  tesseract.exe
  required native DLLs
  runtime-manifest.json
  tessdata/
    ukr.traineddata
    eng.traineddata
    rus.traineddata
    pol.traineddata
    deu.traineddata
    osd.traineddata
  LICENSES/
```

The CI build validates `tesseract.exe --version` and `--list-langs` before creating the portable ZIP.

## Pinned language models

Language models are pinned by both source commit and SHA256.

`tessdata_best` commit:
`e12c65a915945e4c28e237a9b52bc4a8f39a0cec`

- `ukr`: `1277f6e3b6f707063a92d40e7678e7f57154e8414e328e340be9ee9275eea9c8`
- `eng`: `8280aed0782fe27257a68ea10fe7ef324ca0f8d85bd2fd145d1c2b560bcb66ba`
- `rus`: `b617eb6830ffabaaa795dd87ea7fd251adfe9cf0efe05eb9a2e8128b7728d6b6`
- `pol`: `e80cc4cefbdface06e9223f43f089556b9dcf104020fbc0a200f6863c57d4405`
- `deu`: `8407331d6aa0229dc927685c01a7938fc5a641d1a9524f74838cdac599f0d06e`

`tessdata` commit:
`ced78752cc61322fb554c280d13360b35b8684e4`

- `osd`: `e19f2ae860792fdf372cf48d8ce70ae5da3c4052962fe22e9de1f680c374bb0e`

The build fails if a model hash is missing or does not match.

## Multilingual OCR

`TesseractLanguageCatalog` enumerates installed `*.traineddata` files at runtime. The UI exposes installed language codes and common mixed expressions such as `ukr+eng` while remaining editable for any installed compatible combination.

Before OCR starts, the adapter checks that every requested model exists locally.

## OCR input path

OCR never rewrites the source document.

1. The selected original or processed preview is frozen in memory.
2. `OcrInputMaterializer` writes a private PNG under `Workspace/Temp/OcrInput`.
3. `TesseractOcrEngine` runs the bundled executable locally.
4. TSV output is parsed into:
   - line-preserving text;
   - average confidence;
   - individual words;
   - X/Y/width/height coordinates.
5. The recognized text is displayed in the application.

## Build and licenses

`build/Prepare-OcrRuntime.ps1` performs runtime/model acquisition, SHA256 validation, license collection and smoke tests.

Applicable upstream license files are copied to `Engine/OCR/LICENSES` in the portable package.

No API key, cloud upload or external OCR service is required at runtime.
