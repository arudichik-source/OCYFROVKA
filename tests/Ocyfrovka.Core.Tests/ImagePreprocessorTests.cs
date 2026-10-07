using Ocyfrovka.Imaging;

namespace Ocyfrovka.Core.Tests;

public sealed class ImagePreprocessorTests
{
    [Fact]
    public void OtsuThreshold_SeparatesTwoToneImage()
    {
        var image = new GrayImage(
            4,
            2,
            new byte[] { 10, 10, 10, 10, 240, 240, 240, 240 });

        var threshold = ImagePreprocessor.ComputeOtsuThreshold(image);
        var binary = ImagePreprocessor.Threshold(image, threshold).ToArray();

        Assert.InRange(threshold, 10, 239);
        Assert.Equal(
            new byte[] { 0, 0, 0, 0, 255, 255, 255, 255 },
            binary);
    }

    [Fact]
    public void ContrastStretch_ExpandsNarrowDynamicRange()
    {
        var image = new GrayImage(
            4,
            1,
            new byte[] { 100, 110, 120, 130 });

        var result = ImagePreprocessor.ContrastStretch(image).ToArray();

        Assert.Equal(0, result[0]);
        Assert.Equal(255, result[^1]);
        Assert.True(result[1] < result[2]);
    }

    [Fact]
    public void Sharpen_IsDeterministicAndPreservesDimensions()
    {
        var image = new GrayImage(
            3,
            3,
            new byte[]
            {
                100, 100, 100,
                100, 140, 100,
                100, 100, 100
            });

        var first = ImagePreprocessor.Sharpen(image);
        var second = ImagePreprocessor.Sharpen(image);

        Assert.Equal(3, first.Width);
        Assert.Equal(3, first.Height);
        Assert.Equal(first.ToArray(), second.ToArray());
        Assert.Equal(255, first[1, 1]);
    }

    [Fact]
    public void MedianDenoise_RemovesSinglePixelNoise()
    {
        var image = new GrayImage(
            3,
            3,
            new byte[]
            {
                100, 100, 100,
                100, 255, 100,
                100, 100, 100
            });

        var result = ImagePreprocessor.MedianDenoise3x3(image);

        Assert.Equal(100, result[1, 1]);
    }

    [Fact]
    public void QualityReport_FlagsVeryDarkFlatImage()
    {
        var image = new GrayImage(4, 4, Enumerable.Repeat((byte)5, 16).ToArray());

        var quality = ImagePreprocessor.EvaluateQuality(image);

        Assert.True(quality.Brightness < 5);
        Assert.True(quality.Contrast < 5);
        Assert.Contains("Зображення занадто темне.", quality.Warnings);
        Assert.Contains("Низький контраст.", quality.Warnings);
    }

    [Fact]
    public void AutoProfile_DoesNotMutateSource()
    {
        var sourcePixels = new byte[] { 10, 30, 50, 70, 90, 110, 130, 150, 170 };
        var image = new GrayImage(3, 3, sourcePixels);

        var result = ImagePreprocessor.Process(image, PreprocessingProfile.Auto);

        Assert.Equal(sourcePixels, image.ToArray());
        Assert.Equal(PreprocessingProfile.Auto, result.Profile);
        Assert.Equal(3, result.Image.Width);
        Assert.Equal(3, result.Image.Height);
    }
}
