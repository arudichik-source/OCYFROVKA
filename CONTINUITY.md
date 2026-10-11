# CONTINUITY / HANDOFF

**Updated:** 2026-10-07  
**Repository:** `arudichik-source/OCYFROVKA`  
**Active branch:** `stage/07-smart-corrector`  
**Latest merged PR:** #6 — Stage 6: protected OCR review workflow  
**Green main commit after Stage 6:** `930bce2cd2179644d885a023c28800810ab36c57`  
**Latest verified Stage 7 functional commit:** `7595b2a8c15ccf303e9f2f2dec1e927642ab7fd0`

## Fixed decisions

- Fully local/offline base workflow.
- Original source documents are immutable.
- OCR provenance is preserved after corrections.
- Protected user/confirmed review values cannot be silently overwritten.
- Numeric OCR lookalike substitutions are allowed only in explicitly numeric context.
- Fuzzy dictionary matches are suggestions, never silent corrections.
- Real/private dictionaries remain runtime data and are not committed.

## Completed through Stage 6

Stages 1–6 are merged into `main` and green.

## Stage 7 — implemented

New `Ocyfrovka.Dictionary` module:

- `CorrectionFieldKind`: Text / Numeric / Nomenclature;
- strict schema-v1 JSON dictionary loader;
- empty private dictionary created automatically under `Data/Dictionaries/nomenclature.json`;
- UI can validate/import an external JSON dictionary into the portable workspace;
- dictionary entries contain canonical name, aliases and optional category;
- exact normalized forms are indexed for fast lookup;
- nomenclature comparison normalizes whitespace/punctuation and Cyrillic/Latin visual lookalikes;
- explicit numeric-context correction supports OCR confusions such as O→0, I/l→1, S→5, B→8 only when the resulting value is numeric;
- arbitrary text is never subjected to numeric substitutions;
- deterministic Levenshtein similarity and token Jaccard scoring;
- numeric token mismatches are strongly penalized;
- unambiguous exact aliases may be auto-corrected;
- exact canonical matches do not create fake edits;
- fuzzy matches become `Suggested` review state only;
- equal-score ambiguous matches remain unresolved;
- protected manual/confirmed cells are skipped;
- pending suggestion is visible in the review tooltip;
- explicit `Прийняти пропозицію` remains required for fuzzy correction.

## Latest verified CI

Workflow run:

- Run: `37634623703`
- Commit: `7595b2a8c15ccf303e9f2f2dec1e927642ab7fd0`
- Result: **SUCCESS**
- Core/OCR/Imaging/Layout/Review/Dictionary tests: **71/71 PASS**
- Windows integration tests: **9/9 PASS**
- Total: **80/80 PASS**
- Tesseract: **v5.5.3.20260724**
- Portable Windows x64 publish: **SUCCESS**
- Artifact upload: **SUCCESS**
- Artifact ID: `11488270743`
- Artifact size: `166894846` bytes
- Artifact SHA256: `8c6f155ecc02fe0a8253c51f3f696183fa1bf21cb57ecc7376f0870ef779aeb4`

## Immediate next work

1. Open/check PR #7.
2. Merge Stage 7 after final green check and verify `main`.
3. Create `stage/08-persistence`.
4. Add local SQLite persistence:
   - database under `Data/digitizer.db`;
   - schema/version table;
   - documents/pages/review cells/correction memory;
   - transactional autosave;
   - restore unfinished review;
   - preserve confirmed/manual values across application restart;
   - deterministic tests using temporary databases.
5. Do not introduce cloud persistence.

## Technical notes

- Dictionary module: `src/Ocyfrovka.Dictionary`.
- Private dictionary path: `Data/Dictionaries/nomenclature.json`.
- Review module: `src/Ocyfrovka.Review`.
- Durable persistence has not yet been implemented; current review sessions are in-memory until Stage 8.
- OCR runtime lock remains `build/ocr-runtime.lock.json`.

## Data safety

Never commit real documents, private dictionaries, runtime SQLite databases, Workspace/Export/Logs, credentials, tokens or certificates.
