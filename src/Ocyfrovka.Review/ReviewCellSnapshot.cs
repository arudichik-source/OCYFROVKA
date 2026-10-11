using Ocyfrovka.Core.Ocr;
using Ocyfrovka.Layout;

namespace Ocyfrovka.Review;

public sealed record ReviewCellSnapshot(
    int RowIndex,
    int ColumnIndex,
    string SourceText,
    string CurrentText,
    string? SuggestedText,
    double Confidence,
    LayoutBounds Bounds,
    IReadOnlyList<OcrWord> SourceWords,
    ReviewCellState State);
