using Ocyfrovka.App.Services;
using System.Text;

namespace Ocyfrovka.App.Tests;

public sealed class PdfPageLoaderTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "ocyfrovka-pdf-tests",
        Guid.NewGuid().ToString("N"));

    public PdfPageLoaderTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public async Task LoadAsync_RendersSinglePagePdfToLocalPng()
    {
        var pdfPath = Path.Combine(_root, "one-page.pdf");
        await File.WriteAllBytesAsync(pdfPath, CreateSinglePagePdf());

        var pages = await PdfPageLoader.LoadAsync(pdfPath, _root);

        var page = Assert.Single(pages);
        Assert.True(page.IsPdfPage);
        Assert.Equal(0, page.SourceFrameIndex);
        Assert.True(page.PixelWidth > 0);
        Assert.True(page.PixelHeight > 0);
        Assert.NotNull(page.RenderedImagePath);
        Assert.True(File.Exists(page.RenderedImagePath));

        var signature = new byte[8];
        await using var stream = File.OpenRead(page.RenderedImagePath!);
        Assert.Equal(8, await stream.ReadAsync(signature));

        Assert.Equal(
            new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A },
            signature);
    }

    [Fact]
    public async Task LoadAsync_InvalidPdfReturnsClearError()
    {
        var pdfPath = Path.Combine(_root, "broken.pdf");
        await File.WriteAllTextAsync(pdfPath, "not a pdf");

        var error = await Assert.ThrowsAsync<InvalidDataException>(
            () => PdfPageLoader.LoadAsync(pdfPath, _root));

        Assert.Contains("PDF не вдалося відкрити", error.Message);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static byte[] CreateSinglePagePdf()
    {
        var builder = new StringBuilder();
        var offsets = new int[5];

        builder.Append("%PDF-1.4\n");

        AppendObject(1, "<< /Type /Catalog /Pages 2 0 R >>");
        AppendObject(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        AppendObject(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 400] /Resources << >> /Contents 4 0 R >>");
        AppendObject(4, "<< /Length 0 >>\nstream\n\nendstream");

        var xrefOffset = ByteCount(builder);

        builder.Append("xref\n0 5\n");
        builder.Append("0000000000 65535 f \n");

        for (var i = 1; i <= 4; i++)
        {
            builder.Append($"{offsets[i]:D10} 00000 n \n");
        }

        builder.Append("trailer\n");
        builder.Append("<< /Size 5 /Root 1 0 R >>\n");
        builder.Append("startxref\n");
        builder.Append(xrefOffset);
        builder.Append("\n%%EOF\n");

        return Encoding.ASCII.GetBytes(builder.ToString());

        void AppendObject(int number, string body)
        {
            offsets[number] = ByteCount(builder);
            builder.Append($"{number} 0 obj\n");
            builder.Append(body);
            builder.Append("\nendobj\n");
        }

        static int ByteCount(StringBuilder value)
            => Encoding.ASCII.GetByteCount(value.ToString());
    }
}
