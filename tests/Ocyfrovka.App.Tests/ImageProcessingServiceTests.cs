using Ocyfrovka.App.Services;
using Ocyfrovka.Imaging;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Ocyfrovka.App.Tests;

public sealed class ImageProcessingServiceTests
{
    [Fact]
    public void DetectedCropPreview_IsReversibleAndKeepsFullImage()
    {
        const int width = 20;
        const int height = 16;
        var pixels = Enumerable.Repeat((byte)255, width * height).ToArray();

        var bitmap = BitmapSource.Create(
            width,
            height,
            96,
            96,
            PixelFormats.Gray8,
            null,
            pixels,
            width);

        bitmap.Freeze();

        var preview = new ProcessedPreview(
            FullImage: bitmap,
            Image: bitmap,
            Quality: new ImageQualityReport(50, 50, 50, 50, []),
            OtsuThreshold: 127,
            DeskewAngle: 0,
            ContentBounds: new ImageRect(4, 3, 10, 8),
            Profile: PreprocessingProfile.Auto,
            CropApplied: false);

        var cropped = ImageProcessingService.ApplyDetectedCrop(preview);

        Assert.True(cropped.CropApplied);
        Assert.Equal(10, cropped.Image.PixelWidth);
        Assert.Equal(8, cropped.Image.PixelHeight);
        Assert.Same(bitmap, cropped.FullImage);

        var reset = ImageProcessingService.ResetDetectedCrop(cropped);

        Assert.False(reset.CropApplied);
        Assert.Same(bitmap, reset.Image);
        Assert.Same(bitmap, reset.FullImage);
    }

    [Fact]
    public void FullFrameBounds_DoNotCreateArtificialCrop()
    {
        const int width = 12;
        const int height = 10;
        var pixels = Enumerable.Repeat((byte)255, width * height).ToArray();

        var bitmap = BitmapSource.Create(
            width,
            height,
            96,
            96,
            PixelFormats.Gray8,
            null,
            pixels,
            width);

        bitmap.Freeze();

        var preview = new ProcessedPreview(
            FullImage: bitmap,
            Image: bitmap,
            Quality: new ImageQualityReport(50, 50, 50, 50, []),
            OtsuThreshold: 127,
            DeskewAngle: 0,
            ContentBounds: new ImageRect(0, 0, width, height),
            Profile: PreprocessingProfile.Auto,
            CropApplied: false);

        var result = ImageProcessingService.ApplyDetectedCrop(preview);

        Assert.False(result.CropApplied);
        Assert.Same(bitmap, result.Image);
    }
}
