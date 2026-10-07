# Roadmap

## Stage 1 — Bootstrap
- [x] C# / WPF skeleton.
- [x] Portable workspace initializer.
- [x] Core OCR contracts and document models.
- [x] First unit tests.
- [x] GitHub Actions Windows build.

## Stage 2 — Input
- [ ] Import JPG/JPEG/PNG/BMP/TIF/TIFF.
- [ ] PDF page extraction.
- [ ] Multi-page document model.
- [ ] Page ordering and rotation.

## Stage 3 — Image preprocessing
- [ ] Crop / page boundary detection.
- [ ] Deskew and perspective correction.
- [ ] Grayscale / adaptive threshold / contrast / sharpen.
- [ ] Image quality score.

## Stage 4 — Local OCR
- [ ] Pin Tesseract Windows build.
- [ ] Pin and package Ukrainian/English/OSD traineddata.
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