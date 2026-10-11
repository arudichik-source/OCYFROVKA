# CONTINUITY / HANDOFF

**Updated:** 2026-10-11  
**Repository:** `arudichik-source/OCYFROVKA`  
**Active branch:** `stage/08-persistence`  
**Latest merged PR:** #7 — local dictionaries and conservative SmartCorrector  
**Green main commit after Stage 7:** `d4a3347177edcc18c8f3ce2377905cb968cb29a2`  
**Latest verified Stage 8 functional commit:** `7f83b3d6657b7c9f839fb28ed6c0dab2a2b4c2ad`

## Fixed decisions

- Windows x64, C# / WPF / .NET 10, portable self-contained ZIP.
- Base workflow is fully local/offline.
- Original source documents are immutable.
- OCR source text/coordinates/confidence remain provenance after corrections.
- User/confirmed values cannot be silently overwritten.
- Fuzzy/correction-memory matches are suggestions, not silent changes.
- Runtime databases and private dictionaries stay outside Git.

## Completed through Stage 7

Stages 1–7 are merged into `main` and green.

## Stage 8 — implemented

New `Ocyfrovka.Persistence` module:

- `Microsoft.Data.Sqlite 10.0.12`;
- database path: `Data/digitizer.db`;
- connection pooling disabled for predictable portable DB file lifecycle;
- schema version: `PRAGMA user_version = 1`;
- `PRAGMA foreign_keys = ON`;
- `PRAGMA busy_timeout = 5000`;
- `PRAGMA integrity_check` health report;
- tables:
  - `Documents`;
  - `Pages`;
  - `ReviewCells`;
  - `CorrectionMemory`;
- transactional page/review-cell save;
- OCR text persisted per page;
- review cells persist source/current/suggested text, confidence, bounds, source words and review state;
- correction memory stores normalized input, field kind, canonical value, hit count and last-use time.

Review persistence:

- `ReviewCellSnapshot` / `ReviewTableSnapshot`;
- review sessions can round-trip independently from WPF;
- protected manual/confirmed values survive restart;
- suggested/error/recognized states survive restart;
- source OCR provenance survives restart.

Stable source identity:

- deterministic document/page IDs from full path + file length + modified UTC + frame/page index;
- re-importing the same unchanged file restores the same review;
- changed/replaced files produce new IDs so stale review does not attach to changed content.

WPF integration:

- DB initialized during portable workspace startup;
- persisted OCR/review state restored when a page is re-imported;
- persisted rotation restored;
- autosave after OCR, rotation, grid edit, dictionary correction and review actions;
- per-page raw OCR/review survives application restart;
- explicit user corrections are added to correction memory;
- correction memory is re-applied conservatively as a `Suggested` state only;
- equal-hit alternatives are not chosen automatically.

Security/reliability fixes:

- initial `Microsoft.Data.Sqlite 10.0.0` transitively emitted NU1903 for vulnerable SQLitePCLRaw;
- updated to `Microsoft.Data.Sqlite 10.0.12`; final verified run contains no NU1903 warning;
- SQLite connection pooling disabled to prevent portable DB file-lock retention in deterministic tests.

## Latest verified CI

Workflow run:

- Run: `38127970292`
- Commit: `7f83b3d6657b7c9f839fb28ed6c0dab2a2b4c2ad`
- Result: **SUCCESS**
- Core/OCR/Imaging/Layout/Review/Dictionary/Persistence tests: **80/80 PASS**
- Windows integration tests: **14/14 PASS**
- Total: **94/94 PASS**
- Tesseract: **v5.5.3.20260724**
- Portable Windows x64 publish: **SUCCESS**
- Artifact upload: **SUCCESS**
- Artifact: `OCYFROVKA_Portable_x64`
- Artifact ID: `11696989795`
- Artifact size: `168143422` bytes
- Artifact SHA256: `3a915db34abdcea06be84645eafe609895a064b33677a365322e9d7c3a2519a2`

## Immediate next work

1. Open/check PR #8.
2. Merge Stage 8 after final green PR check.
3. Verify post-merge `main`.
4. Create `stage/09-xlsx-export`.
5. Implement export:
   - reviewed structured table → XLSX;
   - Unicode;
   - safe numeric parsing to actual numeric cells;
   - text identifiers remain text;
   - wrap text / reasonable column widths;
   - optional confidence/review metadata sheet;
   - CSV/TXT secondary exports;
   - deterministic tests.
6. Keep export local and never include hidden OCR provenance unless explicitly selected.

## Technical notes

- Persistence module: `src/Ocyfrovka.Persistence`.
- Review module: `src/Ocyfrovka.Review`.
- Dictionary module: `src/Ocyfrovka.Dictionary`.
- SQLite schema migration foundation starts at version 1.
- Private DB path is ignored by Git through `/Data/`.
- OCR runtime lock remains `build/ocr-runtime.lock.json`.

## Data safety

Never commit real documents, private dictionaries, runtime SQLite databases, Workspace/Export/Logs, credentials, tokens or certificates.
