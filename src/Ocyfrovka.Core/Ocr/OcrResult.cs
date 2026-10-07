namespace Ocyfrovka.Core.Ocr;

public sealed record OcrResult(
    string Text,
    double Confidence,
    IReadOnlyList<OcrWord> Words);

public sealed record OcrWord(
    string Text,
    double Confidence,
    int X,
    int Y,
    int Width,
    int Height);