namespace Ocyfrovka.Imaging;

public sealed class GrayImage
{
    private readonly byte[] _pixels;

    public GrayImage(int width, int height, ReadOnlySpan<byte> pixels)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (pixels.Length != checked(width * height))
        {
            throw new ArgumentException("Pixel buffer size does not match image dimensions.", nameof(pixels));
        }

        Width = width;
        Height = height;
        _pixels = pixels.ToArray();
    }

    public int Width { get; }

    public int Height { get; }

    public ReadOnlyMemory<byte> Pixels => _pixels;

    public byte this[int x, int y] => _pixels[checked(y * Width + x)];

    public byte[] ToArray() => (byte[])_pixels.Clone();
}
