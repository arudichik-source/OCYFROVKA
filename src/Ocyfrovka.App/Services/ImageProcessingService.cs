using Ocyfrovka.Imaging;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Ocyfrovka.App.Services;

internal sealed record ProcessedPreview(
    BitmapSource FullImage,
    BitmapSource Image,
    ImageQualityReport Quality,
    int OtsuThreshold,
    double DeskewAngle,
    ImageRect ContentBounds,
    PreprocessingProfile Profile,
    bool CropApplied);

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
            FullImage: bitmap,
            Image: bitmap,
            Quality: result.Quality,
            OtsuThreshold: result.OtsuThreshold,
            DeskewAngle: result.DeskewAngle,
            ContentBounds: result.ContentBounds,
            Profile: result.Profile,
            CropApplied: false);
    }

    public static ProcessedPreview ApplyDetectedCrop(ProcessedPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);

        if (preview.CropApplied)
        {
            return preview;
        }

        var bounds = preview.ContentBounds;
        if (bounds.IsEmpty ||
            bounds.X < 0 ||
            bounds.Y < 0 ||
            bounds.Right > preview.FullImage.PixelWidth ||
            bounds.Bottom > preview.FullImage.PixelHeight)
        {
            throw new InvalidOperationException(
                "Виявлені межі документа виходять за розмір обробленого зображення.");
        }

        if (bounds.X == 0 &&
            bounds.Y == 0 &&
            bounds.Width == preview.FullImage.PixelWidth &&
            bounds.Height == preview.FullImage.PixelHeight)
        {
            return preview;
        }

        var cropped = new CroppedBitmap(
            preview.FullImage,
            new Int32Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height));

        if (cropped.CanFreeze)
        {
            cropped.Freeze();
        }

        return preview with
        {
            Image = cropped,
            CropApplied = true
        };
    }

    public static ProcessedPreview ResetDetectedCrop(ProcessedPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);

        return preview with
        {
            Image = preview.FullImage,
            CropApplied = false
        };
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
