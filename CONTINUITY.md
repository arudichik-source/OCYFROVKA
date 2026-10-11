# CONTINUITY / HANDOFF

**Updated:** 2026-10-11
**Repository:** `arudichik-source/OCYFROVKA`
**Active branch:** `stage/12-portable-package-check`
**Last merged PR:** #11 — real English/Ukrainian OCR + TSV acceptance
**Main merge after Stage 11:** `cb741749196096c7c2f813c8b3e752b17ee89010`

## Resume priority

Check GitHub state first: latest SHA, open PRs, CI run logs. Stage 12 has code committed, but its first Windows build must still be verified. Do not claim the extracted ZIP smoke-test is green until CI reports success. Continue on `stage/12-portable-package-check`.

## Product rules

- Windows x64 self-contained WPF/.NET 10 folder ZIP, no install/admin permissions required.
- Offline multilingual Tesseract, bundled models: ukr/eng/rus/pol/deu/osd.
- Original scans remain unchanged. No cloud dependency.
- OCR text/geometry/confidence kept as provenance.
- User-confirmed corrections are never silently overwritten.
- No private documents, SQLite runtime DB, Workspace, Export or Logs in public Git history or build.

## Verified through Stage 11

Stages 1–11 merged in `main`; local document intake, image normalization, PDF, multilingual OCR, layout/table inference, review, correction dictionaries, SQLite memory, XLSX/CSV/TXT export, batch queue and acceptance smoke tests.

Stage 10: 133/133 tests and Windows portable ZIP PASS, PR #10 merged.

Stage 11: CI `38133268883` passed (119 Core + 14 Windows = 133/133), bundled offline OCR read `INVOICE 12345` and `ТЕСТ 67890`, TSV output contained valid word bounds and confidence. PR #11 merged after green PR check.

## Stage 12 — implementation in progress

- New `build/Test-PortablePackage.ps1`.
- Expands the generated Windows x64 ZIP to a temporary folder with Ukrainian characters and spaces.
- Checks `ОЦИФРОВКА.exe`, bundled Tesseract runtime, self-contained .NET components (`coreclr.dll`, `hostfxr.dll`, `hostpolicy.dll`).
- Checks all 6 bundled `*.traineddata` language files.
- Rejects accidental private runtime data inside ZIP (`Data/digitizer.db`, `Workspace/Input`, `Workspace/Temp`, `Logs`).
- Reuses `Smoke-Test-Local-Ocr.ps1` to recognize synthetic English/Ukrainian text and verify TSV geometry/confidence **after ZIP extraction**.
- GitHub workflow runs this test before artifact upload.
- Script cleans its disposable fixtures in `finally`.

## Next steps

1. Check latest Stage 12 Windows CI; fix test/code packaging problems, especially Unicode paths or missing runtime assets.
2. Update this file with verified run and artifact details.
3. Open PR #12, merge only after green PR check, verify post-merge main.
4. Begin release-candidate hardening: manual WPF launch tests, documented test checklist, synthetic scan/TableLayout acceptance, performance/cancellation tests.
5. Do not release as production-ready before actual Windows desktop launch is verified.

## Key links

Repository: https://github.com/arudichik-source/OCYFROVKA
Roadmap: `docs/ROADMAP.md`
