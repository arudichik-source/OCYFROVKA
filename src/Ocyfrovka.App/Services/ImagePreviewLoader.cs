using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Ocyfrovka.Core.Documents;

namespace Ocyfrovka.App.Services;

internal static class ImagePreviewLoader
{
    public static BitmapSource Load(DocumentPage page)
    {
        var imagePath = page.ImagePath;
        var frameIndex = page.RenderedImagePath is null ? page.SourceFrameIndex : 0;

        using var stream = File.Open(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var decoder = BitmapDecoder.Create(
            stream,
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad);

        if (frameIndex >= decoder.Frames.Count)
        {
            throw new InvalidDataException("Кадр зображення більше недоступний у файлі попереднього перегляду.");
        }

        BitmapSource source = decoder.Frames[frameIndex];

        if (page.RotationDegrees != 0)
        {
            source = new TransformedBitmap(source, new RotateTransform(page.RotationDegrees));
        }

        if (source.CanFreeze)
        {
            source.Freeze();
        }

        return source;
    }
}
