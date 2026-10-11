# CONTINUITY / HANDOFF

**Updated:** 2026-10-11
**Repository:** `arudichik-source/OCYFROVKA`
**Active branch:** `stage/12-portable-package-check`
**Last merged PR:** #11 — offline bilingual text and TSV acceptance
**Main commit after Stage 11:** `cb741749196096c7c2f813c8b3e752b17ee89010`
**Verified Stage 12 functional commit:** `71502ae24fe6d649ec46140c3c089e1af4b61ad9`

## Resume immediately

Look up live GitHub PR/branch/Actions state first. Stage 12 functional CI is green but PR #12 and post-merge main must still be verified. Continue from this branch/current main state; do not recreate Stages 1–11.

## Product invariants

- Windows x64, self-contained .NET 10 WPF folder ZIP, no installation/admin rights required.
- Base product is offline with pinned multilingual Tesseract, language models `ukr eng rus pol deu osd`.
- Original files never overwritten, private docs and runtime SQLite not committed.
- OCR provenance/confidence preserved; manual confirmed corrections protected.

## Merged stages 1–11

Image/PDF import, geometry and preprocessing, local OCR, table reconstruction, review, dictionaries/SmartCorrector, SQLite correction memory, XLSX/CSV/TXT export, cancellable FIFO batch queue, real bilingual plaintext/TSV smoke tests. Stage 10 and Stage 11 main merges were green.

## Stage 12 — fixed Unicode-path defect, acceptance green

New `build/Test-PortablePackage.ps1`:

- Expands actual ZIP into a temporary folder containing Ukrainian characters and spaces.
- Verifies `ОЦИФРОВКА.exe`, `coreclr.dll`, `hostfxr.dll`, `hostpolicy.dll`, `tesseract.exe`, all six traineddata models.
- Fails if a private `Data/digitizer.db`, `Workspace/Input`, `Workspace/Temp` or `Logs` was accidentally included.
- Executes bilingual OCR **and** TSV confidence/geometry smoke tests after extraction.

Defect found in first run `38133796941`: Windows Tesseract ANSI argv lost Cyrillic characters in absolute `--tessdata-dir` path; unpacked Unicode-path OCR failed to open `eng.traineddata`.

Fix:

- `TesseractOcrEngine` uses Unicode Windows process working directory but relative input and tessdata arguments, preventing lossy absolute argv paths for application-staged OCR input.
- `Smoke-Test-Local-Ocr.ps1` switches current directory to the portable OCR runtime and uses relative `tessdata`.
- The Unicode-path extraction acceptance remained enforced, not skipped.

## Latest verified CI

- Run `38134038117` — **SUCCESS**.
- Functional SHA `71502ae24fe6d649ec46140c3c089e1af4b61ad9`.
- Core/Queue/other module tests **119/119 PASS**, Windows integration **14/14 PASS**; total **133/133 PASS**.
- Plain OCR and TSV both pass `INVOICE 12345` and `ТЕСТ 67890` in build folder and again after ZIP extraction into Unicode path.
- Confidence/word geometry checks PASS.
- Self-contained dependency/language/private-data checks PASS.
- Portable Windows x64 artifact upload **SUCCESS**.
- Artifact `OCYFROVKA_Portable_x64` ID `11700915208`, 170920571 bytes.
- SHA256 `63db33f9dab86058c0a48a1186da9b4305882831945cbad8c095a61a8dad538d`.
- https://github.com/arudichik-source/OCYFROVKA/actions/runs/38134038117

## Next steps

1. Open PR #12, verify final PR CI, merge and verify post-merge `main`.
2. Add user-friendly portable RC download path/release notes and a Windows GUI manual test checklist.
3. Perform real desktop launch tests without admin on user's machine. CI cannot certify actual interactive WPF usability.
4. Add non-sensitive scan/table fixtures and validate page count, table reconstruction and XLSX export end to end.
5. Improve queue persistence/UX and optional manual perspective corner editor before production readiness.

## Data safety

No real user/private scans, dictionaries, SQLite DB, Export/Workspace/Logs or secrets in the public repository.
