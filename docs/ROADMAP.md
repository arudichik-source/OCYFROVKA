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
- [x] Pinned Tesseract Windows runtime + six language models.
- [x] Hash verification and runtime smoke tests.
- [x] Dynamic language selection and real local OCR.
- [x] OCR text/confidence/word coordinates.
- [x] Portable build verification.

## Stage 5 — Layout and table reconstruction
- [x] OCR words → normalized lines.
- [x] Row/column/cell reconstruction.
- [x] Confidence/provenance preservation.
- [x] Header detection and structured preview.
- [x] Regression tests and portable build verification.

## Stage 6 — Review workflow
- [x] Per-document/page/cell review state.
- [x] Editable structured table with protected OCR provenance.
- [x] Confirm / confirm-all / suggestion / error / reset actions.
- [x] Low-confidence and state highlighting.
- [x] Preserve manual/confirmed values across OCR re-runs.
- [x] Regression tests and portable build verification.

## Stage 7 — Dictionaries and SmartCorrector
- [x] Local JSON dictionary model and loader.
- [x] Runtime creation of private `Data/Dictionaries/nomenclature.json`.
- [x] User import of JSON dictionary through UI.
- [x] Indexed exact alias lookup for large catalogs.
- [x] Text/name normalization.
- [x] Cyrillic/Latin visual-lookalike normalization for nomenclature.
- [x] Numeric lookalike correction only in explicit numeric context.
- [x] Levenshtein + token similarity.
- [x] Important numeric-token mismatch penalty.
- [x] Exact unambiguous alias may auto-correct.
- [x] Fuzzy matches become suggestions only.
- [x] Equal/ambiguous candidates are never chosen automatically.
- [x] Protected user/confirmed cells are never overwritten.
- [x] SmartCorrector integrated with Stage 6 review.
- [x] Regression tests and portable build verification.

## Stage 8 — SQLite persistence and correction memory
- [ ] Portable SQLite database initialization.
- [ ] Persist document/page/review state.
- [ ] Persist confirmed/manual cell values.
- [ ] Correction-memory table.
- [ ] Restore unfinished review after restart.
- [ ] Autosave and schema migration foundation.
- [ ] Regression tests.

## Stage 9+
- [ ] XLSX export.
- [ ] Queue/background processing.
- [ ] Optional interactive four-corner editor.
- [ ] Regression dataset and production check.
- [ ] Portable RC.
