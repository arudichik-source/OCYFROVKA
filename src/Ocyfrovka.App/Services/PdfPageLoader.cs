using Ocyfrovka.Core.Documents;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Ocyfrovka.App.Services;

internal static class PdfPageLoader
{
    private const double RenderScale = 2.0;
    private const uint MaxRenderDimension = 6000;

    public static async Task<IReadOnlyList<DocumentPage>> LoadAsync(
        string path,
        string applicationBaseDirectory,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("PDF-файл не знайдено.", path);
        }

        PdfDocument pdf;
        try
        {
            var storageFile = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(path));
            pdf = await PdfDocument.LoadFromFileAsync(storageFile);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidDataException(
                "PDF не вдалося відкрити. Файл може бути пошкоджений, захищений паролем або мати непідтримувану структуру.",
                ex);
        }

        if (pdf.PageCount == 0)
        {
            throw new InvalidDataException("PDF не містить сторінок.");
        }

        var outputDirectory = GetOutputDirectory(path, applicationBaseDirectory);
        Directory.CreateDirectory(outputDirectory);

        var pages = new List<DocumentPage>(checked((int)pdf.PageCount));

        for (uint index = 0; index < pdf.PageCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var pdfPage = pdf.GetPage(index);
                var (width, height) = GetRenderSize(pdfPage.Size.Width, pdfPage.Size.Height);
                var renderedPath = Path.Combine(outputDirectory, $"page-{index + 1:0000}.png");

                using var renderStream = new InMemoryRandomAccessStream();
                var options = new PdfPageRenderOptions
                {
                    DestinationWidth = width,
                    DestinationHeight = height
                };

                await pdfPage.RenderToStreamAsync(renderStream, options);
                await SaveRandomAccessStreamAsync(renderStream, renderedPath, cancellationToken);

                pages.Add(new DocumentPage(
                    sourcePath: path,
                    sourceFrameIndex: checked((int)index),
                    pixelWidth: checked((int)width),
                    pixelHeight: checked((int)height),
                    renderedImagePath: renderedPath));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new InvalidDataException(
                    $"Не вдалося відобразити сторінку {index + 1} PDF-файлу.",
                    ex);
            }
        }

        return pages;
    }

    private static (uint Width, uint Height) GetRenderSize(double sourceWidth, double sourceHeight)
    {
        if (sourceWidth <= 0 || sourceHeight <= 0)
        {
            throw new InvalidDataException("PDF містить сторінку з некоректним розміром.");
        }

        var width = sourceWidth * RenderScale;
        var height = sourceHeight * RenderScale;
        var largest = Math.Max(width, height);

        if (largest > MaxRenderDimension)
        {
            var downscale = MaxRenderDimension / largest;
            width *= downscale;
            height *= downscale;
        }

        return (
            Math.Max(1u, checked((uint)Math.Round(width))),
            Math.Max(1u, checked((uint)Math.Round(height))));
    }

    private static string GetOutputDirectory(string path, string applicationBaseDirectory)
    {
        var file = new FileInfo(path);
        var identity = $"{Path.GetFullPath(path)}|{file.Length}|{file.LastWriteTimeUtc.Ticks}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..20];

        return Path.Combine(
            applicationBaseDirectory,
            "Workspace",
            "Temp",
            "PdfPages",
            hash);
    }

    private static async Task SaveRandomAccessStreamAsync(
        IRandomAccessStream stream,
        string path,
        CancellationToken cancellationToken)
    {
        if (stream.Size > int.MaxValue)
        {
            throw new InvalidDataException("Відрендерена сторінка PDF занадто велика.");
        }

        stream.Seek(0);
        using var input = stream.GetInputStreamAt(0);
        using var reader = new DataReader(input);

        var byteCount = checked((uint)stream.Size);
        await reader.LoadAsync(byteCount);

        var bytes = new byte[checked((int)byteCount)];
        reader.ReadBytes(bytes);
        await File.WriteAllBytesAsync(path, bytes, cancellationToken);
    }
}
