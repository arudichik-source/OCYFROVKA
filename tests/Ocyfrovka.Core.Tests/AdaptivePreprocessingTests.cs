using Ocyfrovka.Imaging;

namespace Ocyfrovka.Core.Tests;

public sealed class AdaptivePreprocessingTests
{
    [Fact]
    public void AdaptiveThreshold_ProducesOnlyBinaryPixelsOnUnevenLighting()
    {
        const int width = 60;
        const int height = 12;
        var pixels = new byte[width * height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var background = 80 + x * 150 / (width - 1);
                pixels[y * width + x] = (byte)background;
            }
        }

        for (var x = 4; x < width - 4; x++)
        {
            var index = 6 * width + x;
            pixels[index] = (byte)Math.Max(0, pixels[index] - 60);
        }

        var image = new GrayImage(width, height, pixels);
        var result = ImagePreprocessor.AdaptiveThreshold(image, blockSize: 9, bias: 10);
        var values = result.ToArray();

        Assert.All(values, value => Assert.True(value is 0 or 255));
        Assert.Contains((byte)0, values);
        Assert.Contains((byte)255, values);
    }

    [Fact]
    public void NormalizeIllumination_IsDeterministicAndNonDestructive()
    {
        const int width = 30;
        const int height = 8;
        var pixels = new byte[width * height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                pixels[y * width + x] = (byte)(70 + x * 160 / (width - 1));
            }
        }

        var image = new GrayImage(width, height, pixels);

        var first = ImagePreprocessor.NormalizeIllumination(image, blockSize: 7);
        var second = ImagePreprocessor.NormalizeIllumination(image, blockSize: 7);

        Assert.Equal(first.ToArray(), second.ToArray());
        Assert.Equal(pixels, image.ToArray());
        Assert.Equal(width, first.Width);
        Assert.Equal(height, first.Height);
    }

    [Fact]
    public void Process_ReturnsDeskewAndContentBoundsDiagnostics()
    {
        const int width = 40;
        const int height = 30;
        var pixels = Enumerable.Repeat((byte)255, width * height).ToArray();

        for (var y = 8; y <= 20; y++)
        {
            for (var x = 10; x <= 30; x++)
            {
                if (y is 8 or 20 || x is 10 or 30)
                {
                    pixels[y * width + x] = 0;
                }
            }
        }

        var image = new GrayImage(width, height, pixels);
        var result = ImagePreprocessor.Process(image, PreprocessingProfile.ShadowCorrected);

        Assert.InRange(result.DeskewAngle, -7.0, 7.0);
        Assert.False(result.ContentBounds.IsEmpty);
        Assert.InRange(result.ContentBounds.Width, 1, width);
        Assert.InRange(result.ContentBounds.Height, 1, height);
        Assert.Equal(PreprocessingProfile.ShadowCorrected, result.Profile);
    }
}
