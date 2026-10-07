namespace Ocyfrovka.Imaging;

public readonly record struct ImageQuad(
    ImagePoint TopLeft,
    ImagePoint TopRight,
    ImagePoint BottomRight,
    ImagePoint BottomLeft)
{
    public double SignedArea =>
        0.5 * (
            TopLeft.X * TopRight.Y - TopRight.X * TopLeft.Y +
            TopRight.X * BottomRight.Y - BottomRight.X * TopRight.Y +
            BottomRight.X * BottomLeft.Y - BottomLeft.X * BottomRight.Y +
            BottomLeft.X * TopLeft.Y - TopLeft.X * BottomLeft.Y);

    public double Area => Math.Abs(SignedArea);
}
