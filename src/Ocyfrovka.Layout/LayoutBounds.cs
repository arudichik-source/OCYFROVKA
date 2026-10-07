namespace Ocyfrovka.Layout;

public readonly record struct LayoutBounds(int X, int Y, int Width, int Height)
{
    public int Right => checked(X + Width);
    public int Bottom => checked(Y + Height);

    public static LayoutBounds Union(IEnumerable<LayoutBounds> bounds)
    {
        ArgumentNullException.ThrowIfNull(bounds);

        var items = bounds.ToArray();
        if (items.Length == 0)
        {
            return default;
        }

        var left = items.Min(x => x.X);
        var top = items.Min(x => x.Y);
        var right = items.Max(x => x.Right);
        var bottom = items.Max(x => x.Bottom);

        return new LayoutBounds(left, top, right - left, bottom - top);
    }
}
