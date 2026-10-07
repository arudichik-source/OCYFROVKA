# Third-party components

OCYFROVKA intentionally reuses mature open-source components instead of reimplementing OCR infrastructure.

## Tesseract OCR / UB-Mannheim Windows build

- Upstream: https://github.com/UB-Mannheim/tesseract
- Core license: Apache License 2.0
- Some packaging/build helper files carry their own SPDX identifiers (for example MIT). Their original notices must be preserved when copied or modified.
- OCYFROVKA must keep the applicable upstream license/notice files alongside any redistributed Tesseract binaries or copied source.

The application wrapper, document workflow, preprocessing orchestration, table reconstruction, validation, correction memory and export logic remain separate OCYFROVKA code.

Do not add real user documents, local databases, private dictionaries, tokens or credentials to the public repository.
