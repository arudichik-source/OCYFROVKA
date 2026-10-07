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
- [ ] Perspective correction.
- [x] Grayscale / Otsu threshold / contrast / sharpen.
- [x] Median 3×3 denoise foundation.
- [x] Adaptive/local threshold for uneven lighting.
- [x] Local illumination / shadow normalization.
- [x] Image quality score and warnings.
- [x] Original / processed preview switch.
- [ ] User-controlled crop/perspective preview and final Stage 3 regression pass.

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
