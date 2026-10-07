using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Ocyfrovka.Core.Documents;

namespace Ocyfrovka.App.Services;

internal static class ImagePreviewLoader
{
    public static BitmapSource Load(DocumentPage page)
    {
        using var stream = File.Open(page.SourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var decoder = BitmapDecoder.Create(
            stream,
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad);

        if (page.SourceFrameIndex >= decoder.Frames.Count)
        {
            throw new InvalidDataException("Кадр зображення більше недоступний у вихідному файлі.");
        }

        BitmapSource source = decoder.Frames[page.SourceFrameIndex];

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
