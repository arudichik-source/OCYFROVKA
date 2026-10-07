namespace Ocyfrovka.Imaging;

public readonly record struct ImageRect(int X, int Y, int Width, int Height)
{
    public bool IsEmpty => Width <= 0 || Height <= 0;

    public int Right => checked(X + Width);

    public int Bottom => checked(Y + Height);
}
