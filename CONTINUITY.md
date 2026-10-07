# CONTINUITY / HANDOFF

**Updated:** 2026-10-07  
**Repository:** `arudichik-source/OCYFROVKA`  
**Active branch:** `stage/06-review`  
**Latest merged PR:** #5 — Stage 5: deterministic OCR layout and table reconstruction  
**Green main commit after Stage 5:** `7098ecaa533512893c207bddc3b0d2f0631b2773`  
**Latest verified Stage 6 functional commit:** `25920ada350d581d4f428efe5dbe3a6d7c08e05c`

## Fixed decisions

- Windows x64, C# / WPF / .NET 10.
- Self-contained portable ZIP; no installer/admin rights required.
- Local/offline OCR only in the base product.
- Tesseract runtime/models are pinned and hash-verified.
- Original documents are never overwritten.
- OCR source text, word coordinates and confidence remain provenance even after user edits.
- User edits and confirmed values must never be silently overwritten by OCR refresh or auto-correction.
- Smart correction may propose uncertain changes, but acceptance remains explicit.

## Completed through Stage 5

Stages 1–5 are merged into `main` and green.

## Stage 6 — implemented

New independent `Ocyfrovka.Review` domain:

- `ReviewCellState`:
  - Recognized;
  - CorrectedAutomatically;
  - Suggested;
  - CorrectedByUser;
  - ConfirmedByUser;
  - Error.
- stable `ReviewCellKey` by row/column;
- `ReviewCell` stores current value separately from OCR source value;
- confidence, bounds and source words remain attached as provenance;
- manual/confirmed cells are protected from recognition refresh;
- automatic correction cannot overwrite protected cells;
- suggestions do not change current text until explicitly accepted;
- stale confirmed cells are retained rather than silently discarded;
- `ReviewTableSession` merges new OCR/table analysis into an existing review session;
- `ReviewSessionState`: New / NeedsReview / Reviewed / Error;
- `ReviewSummary` and document-wide `ReviewDocumentSummary`;
- confirm-all excludes detected header row.

WPF review UI:

- structured table is editable;
- per-page review sessions persist while switching pages;
- raw OCR text persists per page in-session;
- selected-cell actions:
  - Confirm;
  - Confirm all;
  - Accept suggestion;
  - Mark error;
  - Reset to OCR;
- low-confidence cells are highlighted;
- confirmed/error/suggested/user-corrected states have distinct visual states;
- page review state and document-wide review state are displayed;
- source OCR text is not replaced inside the provenance model when a user edits a cell.

## Latest verified CI

Workflow run:

- Run: `37632254623`
- Commit: `25920ada350d581d4f428efe5dbe3a6d7c08e05c`
- Result: **SUCCESS**
- Core/OCR/Imaging/Layout/Review tests: **59/59 PASS**
- Windows integration tests: **7/7 PASS**
- Total: **66/66 PASS**
- Tesseract: **v5.5.3.20260724**
- Portable Windows x64 publish: **SUCCESS**
- Artifact upload: **SUCCESS**
- Artifact: `OCYFROVKA_Portable_x64`
- Artifact ID: `11486417675`
- Artifact size: `166858939` bytes
- Artifact SHA256: `e93bf116968cdb23849df856006fca7dbb2e0b98bf077e720bc5a0cbf9f8b106`

## Immediate next work

1. Open/check PR #6 for Stage 6.
2. Merge after final green PR check.
3. Verify post-merge `main`.
4. Create `stage/07-smart-corrector`.
5. Implement local dictionary + deterministic SmartCorrector:
   - normalization;
   - Unicode/Cyrillic/Latin lookalikes;
   - numeric-context correction only inside numeric fields;
   - Levenshtein/token matching;
   - confidence-ranked suggestions;
   - no uncertain silent replacement;
   - feed suggestions into `ReviewCell.SetSuggestion`.
6. Add tests and keep portable build green.

## Technical notes

- Review domain: `src/Ocyfrovka.Review`.
- Layout domain: `src/Ocyfrovka.Layout`.
- Review sessions currently persist in memory for the application session.
- Durable persistence across restarts belongs to the later SQLite stage.
- OCR runtime lock remains `build/ocr-runtime.lock.json`.
- Private OCR inputs remain under `Workspace/Temp/OcrInput`.

## Data safety

Never commit real user documents, runtime databases, private dictionaries, Workspace/Export/Logs, credentials, tokens or certificates.
