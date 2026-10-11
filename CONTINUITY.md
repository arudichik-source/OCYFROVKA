# CONTINUITY / HANDOFF

**Updated:** 2026-10-11
**Repository:** `arudichik-source/OCYFROVKA`
**Current working branch:** `stage/09-xlsx-export`
**Last merged PR:** #8 — Stage 8 SQLite persistence
**Stage 9 verified code commit:** `e92b19c0775fe17087bfdf8b676f11d5a732c48d`

## Read first in next conversation

Inspect the current main/branch/PR state via GitHub before making changes. This file documents the verified Stage 9 state **before merging PR #9**; GitHub is authoritative if later work has already moved ahead. Do not recreate completed work.

## Product constraints

- Windows x64 WPF/.NET 10, portable self-contained ZIP (no admin/installer).
- Base operation fully local/offline, multilingual OCR with pinned/hash-verified Tesseract runtime and languages.
- Original documents immutable; no uploading sensitive inputs or private dictionaries.
- OCR words/coordinates/confidence remain provenance.
- Confirmed/user-edited values must never be silently replaced.
- Fuzzy matches and correction memory yield suggestions, never forced changes.
- Runtime database, documents, dictionaries, Workspace, Export and Logs are ignored by Git.

## Completed stages

Stages 1–8 merged into main. Stage 9 is implemented on `stage/09-xlsx-export`:

- New `src/Ocyfrovka.Export` module using ClosedXML for XLSX.
- `ReviewExportProjection` exports current reviewed values; by default refuses export until review state is `Reviewed`.
- XLSX with UTF-8/Unicode values, header styling, wrap text, filters, freeze top row, bounded widths.
- Header-based **conservative** numeric classification. Quantities/prices/totals become actual numeric Excel cells only when safely parseable; identifiers/codes/serials, values with leading zeros and formula-like strings remain text.
- Optional metadata worksheet includes OCR source/current value/confidence/review state/suggestion. It is opt-in only.
- CSV semicolon-delimited and TXT tab-delimited export; UTF-8 BOM, quoting and CSV formula-injection guard.
- WPF export controls integrated, with explicit options for format and metadata.
- Deterministic tests cover export projection, Unicode, typing, metadata, CSV/TXT and identifiers.

## Latest verified Stage 9 CI

- Workflow: `38130703561`, commit `e92b19c0775fe17087bfdf8b676f11d5a732c48d`
- Result **SUCCESS**.
- Core/Export tests **112/112 PASS**.
- Windows App integration **14/14 PASS**.
- Total **126/126 PASS**.
- Tesseract smoke check **v5.5.3.20260724**.
- Portable Windows x64 publish and artifact upload **SUCCESS**.
- Artifact: `OCYFROVKA_Portable_x64`, ID `11698273247`, 170899079 bytes.
- Artifact SHA256: `42c4cd0080d0e3095dba08cac7e0f1c4fa91fb0bca7cfab65e0ae6aadbf083aa`.
- Run URL: https://github.com/arudichik-source/OCYFROVKA/actions/runs/38130703561

## Next development sequence (no arbitrary pause)

1. Open/check PR #9 and merge only if final PR CI is green.
2. Verify post-merge `main` build.
3. Create `stage/10-processing-queue` from green main.
4. Implement deterministic queue as independent domain:
   - queued/running/completed/failed/canceled states;
   - bounded parallelism; cancellation tokens; retry policy;
   - per-page progress and error diagnostics;
   - no UI-thread blocking;
   - tests for FIFO, cancellation, failure isolation, retries;
   - integrate batch processing with UI only after domain is green.
5. Update this file at each material checkpoint and preserve real source privacy.

## Known deferred items

- Optional manual four-corner perspective editor.
- Representative *non-sensitive* regression fixtures and OCR acceptance quality thresholds.
- Production check, release candidate and performance tuning on real samples.
