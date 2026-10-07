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
- [ ] Crop / page boundary detection.
- [ ] Deskew and perspective correction.
- [ ] Grayscale / adaptive threshold / contrast / sharpen.
- [ ] Image quality score.

## Stage 4 — Local OCR
- [ ] Pin Tesseract Windows build.
- [ ] Pin and package base multilingual traineddata: Ukrainian/English/Russian/Polish/German + OSD.
- [x] Dynamic installed-language catalog and multi-language expressions.
- [ ] Language selector in UI driven by installed models.
- [ ] `TesseractOcrEngine : IOcrEngine`.
- [ ] OCR confidence and word coordinates.

## Stage 5+
- [ ] Layout/table reconstruction.
- [ ] Review workflow.
- [ ] Dictionaries and SmartCorrector.
- [ ] SQLite persistence and correction memory.
- [ ] XLSX export.
- [ ] Queue/background processing.
- [ ] Regression dataset and production check.
- [ ] Portable RC.