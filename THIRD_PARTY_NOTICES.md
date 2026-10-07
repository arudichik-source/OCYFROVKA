# Third-party components

OCYFROVKA intentionally reuses mature open-source components instead of reimplementing OCR infrastructure.

## Tesseract OCR Windows runtime

- Runtime version packaged by OCYFROVKA: `5.5.3.20260724`
- Release tag: `5.5.3`
- Runtime installer SHA256:
  `bee9e3434bd94fd65387d9be28cd467a41f61b1275383b55b0f59a1331270ae4`
- Tesseract core license: Apache License 2.0.
- Exact runtime and model sources/hashes are recorded in `build/ocr-runtime.lock.json`.
- Applicable upstream license files are copied into `Engine/OCR/LICENSES` in every portable build.

## Tesseract language data

OCYFROVKA packages pinned language models from the official Tesseract data repositories.

Base portable pack:

- Ukrainian (`ukr`)
- English (`eng`)
- Russian (`rus`)
- Polish (`pol`)
- German (`deu`)
- orientation/script detection (`osd`)

Every packaged model is SHA256-verified during the build.

The application wrapper, document workflow, preprocessing orchestration, layout/table reconstruction, validation, correction memory and export logic remain separate OCYFROVKA code.

Do not add real user documents, local databases, private dictionaries, tokens or credentials to the public repository.
