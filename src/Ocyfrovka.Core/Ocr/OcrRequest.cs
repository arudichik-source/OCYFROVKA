namespace Ocyfrovka.Core.Ocr;

public sealed record OcrRequest(
    string FilePath,
    string Language = "ukr",
    string Profile = "auto");