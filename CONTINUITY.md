# CONTINUITY / HANDOFF

**Updated:** 2026-10-07  
**Repository:** `arudichik-source/OCYFROVKA`  
**Active branch:** `stage/05-layout-tables`  
**Latest merged PR:** #4 — Stage 4: pinned multilingual local OCR runtime  
**Green main commit after Stage 4:** `f286d6e4b2b798b62b739664924067a97b309a40`  
**Latest verified Stage 5 functional commit:** `16ad8ba3ace17d367c97cd9874d923dca82f3637`

## Fixed project decisions

- Windows x64, C# / WPF / .NET 10.
- Self-contained portable ZIP; no installer/admin rights required.
- Local/offline OCR only in the base product.
- Tesseract runtime/models are pinned and hash-verified.
- Original documents are never overwritten.
- OCR word coordinates/confidence remain source provenance for later review/correction.
- Structured reconstruction must be deterministic and may not invent missing cells/values.

## Completed through Stage 4

Stages 1–4 are merged into `main` and green. Stage 4 includes pinned Tesseract 5.5.3.20260724, six bundled language resources, dynamic language selection and real local OCR.

## Stage 5 — implemented

- new independent `Ocyfrovka.Layout` project;
- OCR words normalized into geometry-based `TextLine` objects;
- stable row grouping by vertical centers;
- word chunks split into candidate cells using text-height-bounded gap thresholds;
- recurring X anchors infer columns;
- table rows/cells reconstructed from source OCR words;
- each `TableCell` preserves:
  - text;
  - bounds;
  - average confidence;
  - original source words;
- header-row heuristic identifies a textual header followed by numeric/data cells;
- paragraph-like OCR is not forced into a table;
- WPF now exposes two OCR-result tabs:
  - raw text;
  - structured table;
- table preview dynamically creates columns from detected headers or generic column names;
- duplicate header names are made unique;
- UI reports reconstructed dimensions, average cell confidence and whether a header was identified.

## Stage 5 failures fixed

1. CI initially failed because `Ocyfrovka.Layout` was not referenced by the app/test projects.
2. After references were added, two layout tests exposed a threshold bug: wide table column gaps inflated the global median gap and prevented cell splitting.
3. The split threshold is now capped relative to median text height, preserving normal word spacing while separating table columns.

## Latest verified CI

Workflow run:

- Run: `37627286727`
- Commit: `16ad8ba3ace17d367c97cd9874d923dca82f3637`
- Result: **SUCCESS**
- Core/OCR/Imaging/Layout tests: **49/49 PASS**
- Windows integration tests: **5/5 PASS**
- Total: **54/54 PASS**
- Portable Windows x64 publish: **SUCCESS**
- Portable artifact upload: **SUCCESS**

## Immediate next work

1. Finalize/check PR #5 for Stage 5.
2. Merge Stage 5 into `main` after final green PR check.
3. Verify post-merge `main`.
4. Create `stage/06-review`.
5. Implement review state:
   - recognized;
   - auto-corrected;
   - suggested;
   - corrected-by-user;
   - confirmed-by-user;
   - error.
6. User-confirmed values must survive OCR refresh/re-analysis and never be silently overwritten.
7. Add editable structured-table review UI and low-confidence highlighting.
8. Add deterministic state-transition tests.

## Technical notes

- Layout project: `src/Ocyfrovka.Layout`.
- Main analyzer: `TableLayoutAnalyzer`.
- Table reconstruction uses source `OcrWord` geometry, not raw string parsing.
- Current structured preview is an in-memory `DataTable`; authoritative provenance remains in `TableLayout/TableCell`.
- Stage 4 runtime lock remains `build/ocr-runtime.lock.json`.
- OCR input remains private under `Workspace/Temp/OcrInput`.

## GitHub workflow rule

Use stage branches/PRs and keep `main` releasable. Update this file after meaningful development blocks.

## Data safety

Never commit real user documents, runtime databases, private dictionaries, Workspace/Export/Logs, credentials, tokens or certificates.
