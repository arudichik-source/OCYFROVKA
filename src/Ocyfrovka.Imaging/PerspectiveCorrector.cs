namespace Ocyfrovka.Imaging;

public static class PerspectiveCorrector
{
    public static GrayImage Rectify(
        GrayImage source,
        ImageQuad sourceQuad,
        int? outputWidth = null,
        int? outputHeight = null,
        byte background = 255)
    {
        ArgumentNullException.ThrowIfNull(source);
        ValidateQuad(source, sourceQuad);

        var width = outputWidth ?? EstimateWidth(sourceQuad);
        var height = outputHeight ?? EstimateHeight(sourceQuad);

        if (width < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(outputWidth));
        }

        if (height < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(outputHeight));
        }

        var destination = new[]
        {
            new ImagePoint(0, 0),
            new ImagePoint(width - 1, 0),
            new ImagePoint(width - 1, height - 1),
            new ImagePoint(0, height - 1)
        };

        var sourcePoints = new[]
        {
            sourceQuad.TopLeft,
            sourceQuad.TopRight,
            sourceQuad.BottomRight,
            sourceQuad.BottomLeft
        };

        var homography = SolveHomography(destination, sourcePoints);
        var output = Enumerable.Repeat(background, checked(width * height)).ToArray();

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var denominator =
                    homography[6] * x +
                    homography[7] * y +
                    1.0;

                if (Math.Abs(denominator) < 1e-12)
                {
                    continue;
                }

                var sourceX =
                    (homography[0] * x +
                     homography[1] * y +
                     homography[2]) / denominator;

                var sourceY =
                    (homography[3] * x +
                     homography[4] * y +
                     homography[5]) / denominator;

                output[y * width + x] =
                    SampleBilinear(source, sourceX, sourceY, background);
            }
        }

        return new GrayImage(width, height, output);
    }

    private static int EstimateWidth(ImageQuad quad)
    {
        var top = Distance(quad.TopLeft, quad.TopRight);
        var bottom = Distance(quad.BottomLeft, quad.BottomRight);
        return Math.Max(2, checked((int)Math.Round(Math.Max(top, bottom))) + 1);
    }

    private static int EstimateHeight(ImageQuad quad)
    {
        var left = Distance(quad.TopLeft, quad.BottomLeft);
        var right = Distance(quad.TopRight, quad.BottomRight);
        return Math.Max(2, checked((int)Math.Round(Math.Max(left, right))) + 1);
    }

    private static double Distance(ImagePoint a, ImagePoint b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static void ValidateQuad(GrayImage source, ImageQuad quad)
    {
        if (quad.Area < 1.0)
        {
            throw new ArgumentException(
                "Perspective quadrilateral is degenerate.",
                nameof(quad));
        }

        foreach (var point in new[]
                 {
                     quad.TopLeft,
                     quad.TopRight,
                     quad.BottomRight,
                     quad.BottomLeft
                 })
        {
            if (point.X < 0 ||
                point.Y < 0 ||
                point.X > source.Width - 1 ||
                point.Y > source.Height - 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(quad),
                    "Perspective corner lies outside the source image.");
            }
        }
    }

    private static double[] SolveHomography(
        IReadOnlyList<ImagePoint> from,
        IReadOnlyList<ImagePoint> to)
    {
        var matrix = new double[8, 9];

        for (var i = 0; i < 4; i++)
        {
            var u = from[i].X;
            var v = from[i].Y;
            var x = to[i].X;
            var y = to[i].Y;

            var row = i * 2;
            matrix[row, 0] = u;
            matrix[row, 1] = v;
            matrix[row, 2] = 1;
            matrix[row, 6] = -u * x;
            matrix[row, 7] = -v * x;
            matrix[row, 8] = x;

            matrix[row + 1, 3] = u;
            matrix[row + 1, 4] = v;
            matrix[row + 1, 5] = 1;
            matrix[row + 1, 6] = -u * y;
            matrix[row + 1, 7] = -v * y;
            matrix[row + 1, 8] = y;
        }

        for (var pivot = 0; pivot < 8; pivot++)
        {
            var bestRow = pivot;
            var bestValue = Math.Abs(matrix[pivot, pivot]);

            for (var row = pivot + 1; row < 8; row++)
            {
                var candidate = Math.Abs(matrix[row, pivot]);
                if (candidate > bestValue)
                {
                    bestRow = row;
                    bestValue = candidate;
                }
            }

            if (bestValue < 1e-12)
            {
                throw new InvalidOperationException(
                    "Perspective transform cannot be solved for the supplied corners.");
            }

            if (bestRow != pivot)
            {
                for (var column = pivot; column < 9; column++)
                {
                    (matrix[pivot, column], matrix[bestRow, column]) =
                        (matrix[bestRow, column], matrix[pivot, column]);
                }
            }

            var divisor = matrix[pivot, pivot];
            for (var column = pivot; column < 9; column++)
            {
                matrix[pivot, column] /= divisor;
            }

            for (var row = 0; row < 8; row++)
            {
                if (row == pivot)
                {
                    continue;
                }

                var factor = matrix[row, pivot];
                if (Math.Abs(factor) < 1e-16)
                {
                    continue;
                }

                for (var column = pivot; column < 9; column++)
                {
                    matrix[row, column] -= factor * matrix[pivot, column];
                }
            }
        }

        var result = new double[8];
        for (var i = 0; i < 8; i++)
        {
            result[i] = matrix[i, 8];
        }

        return result;
    }

    private static byte SampleBilinear(
        GrayImage source,
        double x,
        double y,
        byte background)
    {
        if (x < 0 ||
            y < 0 ||
            x > source.Width - 1 ||
            y > source.Height - 1)
        {
            return background;
        }

        var x0 = (int)Math.Floor(x);
        var y0 = (int)Math.Floor(y);
        var x1 = Math.Min(source.Width - 1, x0 + 1);
        var y1 = Math.Min(source.Height - 1, y0 + 1);

        var fx = x - x0;
        var fy = y - y0;

        var top =
            source[x0, y0] * (1.0 - fx) +
            source[x1, y0] * fx;

        var bottom =
            source[x0, y1] * (1.0 - fx) +
            source[x1, y1] * fx;

        var value =
            top * (1.0 - fy) +
            bottom * fy;

        return (byte)Math.Clamp(
            (int)Math.Round(value),
            0,
            255);
    }
}
