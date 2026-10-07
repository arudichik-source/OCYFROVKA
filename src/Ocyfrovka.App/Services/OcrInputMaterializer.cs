using System.Windows.Media.Imaging;

namespace Ocyfrovka.App.Services;

internal static class OcrInputMaterializer
{
    public static async Task<string> SavePngAsync(
        BitmapSource source,
        Guid pageId,
        string applicationBaseDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        var directory = Path.Combine(
            applicationBaseDirectory,
            "Workspace",
            "Temp",
            "OcrInput");

        Directory.CreateDirectory(directory);

        var outputPath = Path.Combine(
            directory,
            $"{pageId:N}.png");

        var bytes = await Task.Run(
            () => EncodePng(source),
            cancellationToken);

        await File.WriteAllBytesAsync(outputPath, bytes, cancellationToken);
        return outputPath;
    }

    private static byte[] EncodePng(BitmapSource source)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }
}
