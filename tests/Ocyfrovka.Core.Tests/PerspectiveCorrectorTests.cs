using Ocyfrovka.Imaging;

namespace Ocyfrovka.Core.Tests;

public sealed class PerspectiveCorrectorTests
{
    [Fact]
    public void Rectify_IdentityQuadPreservesPixelsExactly()
    {
        var pixels = Enumerable.Range(0, 100)
            .Select(value => (byte)value)
            .ToArray();

        var image = new GrayImage(10, 10, pixels);
        var quad = new ImageQuad(
            new ImagePoint(0, 0),
            new ImagePoint(9, 0),
            new ImagePoint(9, 9),
            new ImagePoint(0, 9));

        var result = PerspectiveCorrector.Rectify(
            image,
            quad,
            outputWidth: 10,
            outputHeight: 10);

        Assert.Equal(10, result.Width);
        Assert.Equal(10, result.Height);
        Assert.Equal(pixels, result.ToArray());
        Assert.Equal(pixels, image.ToArray());
    }

    [Fact]
    public void Rectify_SkewedQuadMapsAllFourCorners()
    {
        var pixels = Enumerable.Repeat((byte)200, 20 * 20).ToArray();

        Set(3, 2, 10);
        Set(16, 4, 40);
        Set(15, 17, 80);
        Set(2, 15, 120);

        var image = new GrayImage(20, 20, pixels);
        var quad = new ImageQuad(
            new ImagePoint(3, 2),
            new ImagePoint(16, 4),
            new ImagePoint(15, 17),
            new ImagePoint(2, 15));

        var result = PerspectiveCorrector.Rectify(
            image,
            quad,
            outputWidth: 14,
            outputHeight: 14);

        Assert.Equal((byte)10, result[0, 0]);
        Assert.Equal((byte)40, result[13, 0]);
        Assert.Equal((byte)80, result[13, 13]);
        Assert.Equal((byte)120, result[0, 13]);

        void Set(int x, int y, byte value)
            => pixels[y * 20 + x] = value;
    }

    [Fact]
    public void Rectify_IsDeterministic()
    {
        var pixels = Enumerable.Range(0, 400)
            .Select(value => (byte)(value % 256))
            .ToArray();

        var image = new GrayImage(20, 20, pixels);
        var quad = new ImageQuad(
            new ImagePoint(2, 1),
            new ImagePoint(18, 3),
            new ImagePoint(16, 18),
            new ImagePoint(1, 16));

        var first = PerspectiveCorrector.Rectify(image, quad, 16, 17);
        var second = PerspectiveCorrector.Rectify(image, quad, 16, 17);

        Assert.Equal(first.ToArray(), second.ToArray());
        Assert.Equal(pixels, image.ToArray());
    }

    [Fact]
    public void Rectify_RejectsCornerOutsideSource()
    {
        var image = new GrayImage(
            10,
            10,
            Enumerable.Repeat((byte)255, 100).ToArray());

        var quad = new ImageQuad(
            new ImagePoint(-1, 0),
            new ImagePoint(9, 0),
            new ImagePoint(9, 9),
            new ImagePoint(0, 9));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => PerspectiveCorrector.Rectify(image, quad));
    }
}
