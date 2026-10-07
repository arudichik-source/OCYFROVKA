# Roadmap

## Stage 1 — Bootstrap
- [x] C# / WPF skeleton.
- [x] Portable workspace initializer.
- [x] Core OCR contracts and document models.
- [x] First unit tests.
- [x] GitHub Actions Windows build.

## Stage 2 — Input
- [x] Import JPG/JPEG/PNG/BMP/TIF/TIFF.
- [x] Local PDF page extraction and preview rendering.
- [x] Multi-page document model.
- [x] Page ordering and non-destructive 90° rotation.

## Stage 3 — Image preprocessing
- [x] Page-content boundary detection and crop primitive.
- [x] Automatic skew-angle estimation and non-destructive deskew.
- [x] Deterministic four-corner perspective correction engine.
- [x] Grayscale / Otsu threshold / contrast / sharpen.
- [x] Median 3×3 denoise foundation.
- [x] Adaptive/local threshold for uneven lighting.
- [x] Local illumination / shadow normalization.
- [x] Image quality score and warnings.
- [x] Original / processed preview switch.
- [x] User-controlled detected-crop preview with reversible full-frame reset.
- [x] Synthetic deskew/crop/perspective/adaptive regression coverage.
- [x] Stage 3 portable build verification.

## Stage 4 — Local OCR
- [x] Pin Tesseract Windows x64 runtime to 5.5.3.20260724.
- [x] Verify runtime installer SHA256 during every portable build.
- [x] Pin and package Ukrainian / English / Russian / Polish / German + OSD traineddata.
- [x] Verify every traineddata SHA256 during every portable build.
- [x] Dynamic installed-language catalog and multi-language expressions.
- [x] Editable UI language selector driven by installed models.
- [x] Real `TesseractOcrEngine : IOcrEngine` execution.
- [x] Materialize original/processed preview to private local PNG for OCR.
- [x] TSV parser preserves OCR lines, confidence and word coordinates.
- [x] OCR result panel in WPF.
- [x] Runtime `--version` and `--list-langs` smoke checks in CI.
- [x] Portable artifact contains runtime, DLL dependencies, models and licenses.
- [x] Stage 4 portable build verification.

## Stage 5 — Layout and table reconstruction
- [x] Convert OCR words + coordinates into normalized text lines.
- [x] Detect table-like row/column structure.
- [x] Reconstruct cells from word geometry and spacing.
- [x] Preserve source word confidence/provenance for reconstructed cells.
- [x] Detect likely headers and data rows.
- [x] Show raw OCR text and structured table preview in separate UI tabs.
- [x] Regression tests for line/row/column reconstruction.
- [x] Portable build verification.

## Stage 6 — Review workflow
- [ ] Per-document review state.
- [ ] Per-cell review state.
- [ ] User edits without silently overwriting OCR source provenance.
- [ ] Confirm/reject/suggest workflow.
- [ ] Highlight low-confidence cells.
- [ ] Preserve user-confirmed values across OCR re-runs.
- [ ] Regression tests for state transitions and protected confirmed values.

## Stage 7+
- [ ] Dictionaries and SmartCorrector.
- [ ] SQLite persistence and correction memory.
- [ ] XLSX export.
- [ ] Queue/background processing.
- [ ] Optional interactive four-corner editor for manual perspective correction.
- [ ] Regression dataset and production check.
- [ ] Portable RC.
