using Ocyfrovka.Core.Ocr;

namespace Ocyfrovka.Layout;

public sealed record TextLine(
    int Index,
    string Text,
    LayoutBounds Bounds,
    double Confidence,
    IReadOnlyList<OcrWord> Words);
