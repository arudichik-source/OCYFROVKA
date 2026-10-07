using Ocyfrovka.Core.Ocr;

namespace Ocyfrovka.Layout;

public sealed record TableCell(
    int RowIndex,
    int ColumnIndex,
    string Text,
    LayoutBounds Bounds,
    double Confidence,
    IReadOnlyList<OcrWord> SourceWords);
