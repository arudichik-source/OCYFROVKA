using Ocyfrovka.App.Services;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Ocyfrovka.App.Tests;

public sealed class OcrInputMaterializerTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "ocyfrovka-ocr-input-tests",
        Guid.NewGuid().ToString("N"));

    public OcrInputMaterializerTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public async Task SavePngAsync_WritesValidPngIntoPrivateWorkspace()
    {
        var pixels = Enumerable.Repeat((byte)240, 16 * 12).ToArray();
        var bitmap = BitmapSource.Create(
            16,
            12,
            96,
            96,
            PixelFormats.Gray8,
            null,
            pixels,
            16);

        bitmap.Freeze();

        var pageId = Guid.NewGuid();
        var path = await OcrInputMaterializer.SavePngAsync(
            bitmap,
            pageId,
            _root);

        Assert.True(File.Exists(path));
        Assert.Contains(
            Path.Combine("Workspace", "Temp", "OcrInput"),
            path,
            StringComparison.OrdinalIgnoreCase);

        var signature = new byte[8];
        await using var stream = File.OpenRead(path);
        Assert.Equal(8, await stream.ReadAsync(signature));

        Assert.Equal(
            new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A },
            signature);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
