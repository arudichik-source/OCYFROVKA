using Ocyfrovka.Imaging;

namespace Ocyfrovka.Core.Tests;

public sealed class ImageGeometryTests
{
    [Fact]
    public void EstimateDeskewAngle_FindsCorrectionForSlantedTextLines()
    {
        const int width = 120;
        const int height = 80;
        var pixels = Enumerable.Repeat((byte)255, width * height).ToArray();
        var slope = Math.Tan(4.0 * Math.PI / 180.0);

        foreach (var baseY in new[] { 20, 40, 60 })
        {
            for (var x = 10; x < 110; x++)
            {
                var y = (int)Math.Round(baseY + slope * (x - 10));
                if ((uint)y < height)
                {
                    pixels[y * width + x] = 0;
                }
            }
        }

        var image = new GrayImage(width, height, pixels);
        var correction = ImageGeometry.EstimateDeskewAngle(image, 7, 0.5);

        Assert.InRange(correction, -5.5, -2.5);
    }

    [Fact]
    public void Rotate_IsNonDestructiveAndKeepsCanvasSize()
    {
        var pixels = Enumerable.Repeat((byte)255, 25).ToArray();
        pixels[2 * 5 + 2] = 0;
        var image = new GrayImage(5, 5, pixels);

        var rotated = ImageGeometry.Rotate(image, 5);

        Assert.Equal(5, rotated.Width);
        Assert.Equal(5, rotated.Height);
        Assert.Equal(pixels, image.ToArray());
    }

    [Fact]
    public void DetectContentBounds_FindsInkRectangle()
    {
        const int width = 20;
        const int height = 15;
        var pixels = Enumerable.Repeat((byte)255, width * height).ToArray();

        for (var y = 4; y <= 10; y++)
        {
            for (var x = 5; x <= 14; x++)
            {
                pixels[y * width + x] = 0;
            }
        }

        var image = new GrayImage(width, height, pixels);
        var bounds = ImageGeometry.DetectContentBounds(image, margin: 0);

        Assert.Equal(new ImageRect(5, 4, 10, 7), bounds);
    }

    [Fact]
    public void Crop_ReturnsRequestedRegionWithoutChangingSource()
    {
        var pixels = Enumerable.Range(0, 36).Select(x => (byte)x).ToArray();
        var image = new GrayImage(6, 6, pixels);

        var cropped = ImageGeometry.Crop(image, new ImageRect(2, 1, 3, 2));

        Assert.Equal(3, cropped.Width);
        Assert.Equal(2, cropped.Height);
        Assert.Equal(new byte[] { 8, 9, 10, 14, 15, 16 }, cropped.ToArray());
        Assert.Equal(pixels, image.ToArray());
    }
}
