namespace Ocyfrovka.Core.Input;

public static class InputFileClassifier
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff"
    };

    public static InputFileKind GetKind(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return InputFileKind.Unsupported;
        }

        var extension = Path.GetExtension(path);
        if (ImageExtensions.Contains(extension))
        {
            return InputFileKind.Image;
        }

        if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return InputFileKind.Pdf;
        }

        return InputFileKind.Unsupported;
    }

    public static bool IsImage(string? path) => GetKind(path) == InputFileKind.Image;
}
