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
- [x] Local JSON dictionary model/loader/import.
- [x] Indexed exact alias lookup.
- [x] Cyrillic/Latin visual-lookalike normalization.
- [x] Numeric correction only in explicit numeric context.
- [x] Levenshtein/token fuzzy matching.
- [x] Exact unambiguous aliases may auto-correct.
- [x] Fuzzy/ambiguous matches require review.
- [x] Protected user/confirmed values are never overwritten.
- [x] Regression tests and portable build verification.

## Stage 8 — SQLite persistence and correction memory
- [x] Portable SQLite database under `Data/digitizer.db`.
- [x] Secure `Microsoft.Data.Sqlite 10.0.12` dependency.
- [x] Schema/version foundation via `PRAGMA user_version`.
- [x] SQLite integrity self-check.
- [x] Transactional document/page/review-cell persistence.
- [x] Persist OCR text and source provenance.
- [x] Persist manual/confirmed/suggested/error cell states.
- [x] Stable document/page IDs for unchanged source files.
- [x] Restore unfinished review after application restart/re-import.
- [x] Autosave after OCR, table editing and review actions.
- [x] Correction-memory table with hit counts.
- [x] Correction memory reused as suggestions only.
- [x] Equal-frequency memory candidates remain ambiguous.
- [x] Regression tests for SQLite round-trip, stable identity and correction memory.
- [x] Stage 8 portable build verification.

## Stage 9 — XLSX export
- [ ] Export reviewed structured table to XLSX.
- [ ] Preserve Unicode text.
- [ ] Emit actual numeric cells when safely parseable.
- [ ] Keep textual identifiers as text.
- [ ] Wrap text and reasonable column widths.
- [ ] Export review/provenance metadata optionally.
- [ ] CSV/TXT secondary export foundation.
- [ ] Regression tests and portable build verification.

## Stage 10+
- [ ] Queue/background document processing.
- [ ] Optional interactive four-corner editor.
- [ ] Regression dataset and production check.
- [ ] Portable RC.
