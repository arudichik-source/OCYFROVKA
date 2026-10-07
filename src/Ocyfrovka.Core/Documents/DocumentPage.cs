namespace Ocyfrovka.Core.Documents;

public sealed class DocumentPage
{
    public DocumentPage(
        string sourcePath,
        int sourceFrameIndex,
        int pixelWidth,
        int pixelHeight,
        Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Source path is required.", nameof(sourcePath));
        }

        if (sourceFrameIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceFrameIndex));
        }

        if (pixelWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelWidth));
        }

        if (pixelHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelHeight));
        }

        Id = id ?? Guid.NewGuid();
        SourcePath = sourcePath;
        SourceFrameIndex = sourceFrameIndex;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
    }

    public Guid Id { get; }

    public string SourcePath { get; }

    public int SourceFrameIndex { get; }

    public int PixelWidth { get; }

    public int PixelHeight { get; }

    public int RotationDegrees { get; private set; }

    public string FileName => Path.GetFileName(SourcePath);

    public string DisplayName => SourceFrameIndex == 0
        ? FileName
        : $"{FileName} — кадр {SourceFrameIndex + 1}";

    public void RotateClockwise() => RotationDegrees = NormalizeRotation(RotationDegrees + 90);

    public void RotateCounterClockwise() => RotationDegrees = NormalizeRotation(RotationDegrees - 90);

    private static int NormalizeRotation(int value)
    {
        var result = value % 360;
        return result < 0 ? result + 360 : result;
    }
}
