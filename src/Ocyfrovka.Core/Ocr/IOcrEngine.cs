namespace Ocyfrovka.Core.Ocr;

public interface IOcrEngine
{
    string Name { get; }

    Task<OcrResult> RecognizeAsync(
        OcrRequest request,
        CancellationToken cancellationToken = default);
}