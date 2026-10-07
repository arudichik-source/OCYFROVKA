using System.IO;
using System.Windows.Media.Imaging;
using Ocyfrovka.Core.Documents;
using Ocyfrovka.Core.Input;

namespace Ocyfrovka.App.Services;

internal static class ImagePageLoader
{
    public static IReadOnlyList<DocumentPage> Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Файл не знайдено.", path);
        }

        if (!InputFileClassifier.IsImage(path))
        {
            throw new NotSupportedException($"Непідтримуваний формат зображення: {Path.GetExtension(path)}");
        }

        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var decoder = BitmapDecoder.Create(
            stream,
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad);

        if (decoder.Frames.Count == 0)
        {
            throw new InvalidDataException("Файл не містить доступних для читання кадрів.");
        }

        return decoder.Frames
            .Select((frame, index) => new DocumentPage(
                sourcePath: path,
                sourceFrameIndex: index,
                pixelWidth: frame.PixelWidth,
                pixelHeight: frame.PixelHeight))
            .ToArray();
    }
}
