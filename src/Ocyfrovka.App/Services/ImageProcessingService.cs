using Ocyfrovka.Imaging;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Ocyfrovka.App.Services;

internal sealed record ProcessedPreview(
    BitmapSource Image,
    ImageQualityReport Quality,
    int OtsuThreshold,
    PreprocessingProfile Profile);

internal static class ImageProcessingService
{
    public static ProcessedPreview Process(
        BitmapSource source,
        PreprocessingProfile profile)
    {
        ArgumentNullException.ThrowIfNull(source);

        var gray = ToGrayImage(source);
        var result = ImagePreprocessor.Process(gray, profile);
        var bitmap = ToBitmapSource(result.Image);

        return new ProcessedPreview(
            bitmap,
            result.Quality,
            result.OtsuThreshold,
            result.Profile);
    }

    private static GrayImage ToGrayImage(BitmapSource source)
    {
        BitmapSource graySource = source.Format == PixelFormats.Gray8
            ? source
            : new FormatConvertedBitmap(source, PixelFormats.Gray8, null, 0);

        if (graySource.CanFreeze)
        {
            graySource.Freeze();
        }

        var stride = graySource.PixelWidth;
        var pixels = new byte[checked(stride * graySource.PixelHeight)];
        graySource.CopyPixels(pixels, stride, 0);

        return new GrayImage(
            graySource.PixelWidth,
            graySource.PixelHeight,
            pixels);
    }

    private static BitmapSource ToBitmapSource(GrayImage image)
    {
        var pixels = image.ToArray();
        var bitmap = BitmapSource.Create(
            image.Width,
            image.Height,
            96,
            96,
            PixelFormats.Gray8,
            null,
            pixels,
            image.Width);

        if (bitmap.CanFreeze)
        {
            bitmap.Freeze();
        }

        return bitmap;
    }
}
