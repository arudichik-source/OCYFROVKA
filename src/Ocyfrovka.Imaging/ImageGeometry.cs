namespace Ocyfrovka.Imaging;

public static class ImageGeometry
{
    public static double EstimateDeskewAngle(
        GrayImage source,
        double maxAbsoluteAngle = 7.0,
        double step = 0.5)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (maxAbsoluteAngle <= 0 || maxAbsoluteAngle > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAbsoluteAngle));
        }

        if (step <= 0 || step > maxAbsoluteAngle)
        {
            throw new ArgumentOutOfRangeException(nameof(step));
        }

        if (source.Width < 8 || source.Height < 8)
        {
            return 0;
        }

        var threshold = ImagePreprocessor.ComputeOtsuThreshold(source);
        var darkCutoff = Math.Min(220, Math.Max(20, threshold + 10));
        var points = CollectDarkPoints(source, darkCutoff);

        if (points.Count < 8)
        {
            return 0;
        }

        var cx = (source.Width - 1) / 2.0;
        var cy = (source.Height - 1) / 2.0;
        var bestAngle = 0.0;
        var bestScore = double.NegativeInfinity;

        for (var angle = -maxAbsoluteAngle; angle <= maxAbsoluteAngle + step / 2; angle += step)
        {
            var radians = angle * Math.PI / 180.0;
            var sin = Math.Sin(radians);
            var cos = Math.Cos(radians);
            var projection = new int[source.Height];

            foreach (var point in points)
            {
                var dx = point.X - cx;
                var dy = point.Y - cy;
                var rotatedY = sin * dx + cos * dy + cy;
                var row = (int)Math.Round(rotatedY);

                if ((uint)row < (uint)projection.Length)
                {
                    projection[row]++;
                }
            }

            double score = 0;
            for (var i = 0; i < projection.Length; i++)
            {
                score += (double)projection[i] * projection[i];
            }

            if (score > bestScore + 0.0001 ||
                (Math.Abs(score - bestScore) <= 0.0001 && Math.Abs(angle) < Math.Abs(bestAngle)))
            {
                bestScore = score;
                bestAngle = angle;
            }
        }

        return Math.Round(bestAngle, 2);
    }

    public static GrayImage Rotate(
        GrayImage source,
        double angleDegrees,
        byte background = 255)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (Math.Abs(angleDegrees) < 0.0001)
        {
            return new GrayImage(source.Width, source.Height, source.Pixels.Span);
        }

        var width = source.Width;
        var height = source.Height;
        var input = source.Pixels.Span;
        var output = Enumerable.Repeat(background, checked(width * height)).ToArray();

        var radians = angleDegrees * Math.PI / 180.0;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        var cx = (width - 1) / 2.0;
        var cy = (height - 1) / 2.0;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var dx = x - cx;
                var dy = y - cy;

                var sourceX = cos * dx + sin * dy + cx;
                var sourceY = -sin * dx + cos * dy + cy;

                var sx = (int)Math.Round(sourceX);
                var sy = (int)Math.Round(sourceY);

                if ((uint)sx < (uint)width && (uint)sy < (uint)height)
                {
                    output[y * width + x] = input[sy * width + sx];
                }
            }
        }

        return new GrayImage(width, height, output);
    }

    public static ImageRect DetectContentBounds(
        GrayImage source,
        int margin = 4)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (margin < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(margin));
        }

        var width = source.Width;
        var height = source.Height;
        var threshold = ImagePreprocessor.ComputeOtsuThreshold(source);
        var darkCutoff = Math.Min(225, Math.Max(30, threshold + 15));
        var pixels = source.Pixels.Span;

        var minRowInk = Math.Max(1, width / 250);
        var minColumnInk = Math.Max(1, height / 250);

        var top = -1;
        var bottom = -1;

        for (var y = 0; y < height; y++)
        {
            var count = 0;
            var offset = y * width;
            for (var x = 0; x < width; x++)
            {
                if (pixels[offset + x] <= darkCutoff)
                {
                    count++;
                }
            }

            if (count >= minRowInk)
            {
                if (top < 0) top = y;
                bottom = y;
            }
        }

        if (top < 0)
        {
            return new ImageRect(0, 0, width, height);
        }

        var left = -1;
        var right = -1;

        for (var x = 0; x < width; x++)
        {
            var count = 0;
            for (var y = top; y <= bottom; y++)
            {
                if (pixels[y * width + x] <= darkCutoff)
                {
                    count++;
                }
            }

            if (count >= minColumnInk)
            {
                if (left < 0) left = x;
                right = x;
            }
        }

        if (left < 0)
        {
            return new ImageRect(0, 0, width, height);
        }

        left = Math.Max(0, left - margin);
        top = Math.Max(0, top - margin);
        right = Math.Min(width - 1, right + margin);
        bottom = Math.Min(height - 1, bottom + margin);

        return new ImageRect(
            left,
            top,
            right - left + 1,
            bottom - top + 1);
    }

    public static GrayImage Crop(GrayImage source, ImageRect bounds)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (bounds.IsEmpty ||
            bounds.X < 0 ||
            bounds.Y < 0 ||
            bounds.Right > source.Width ||
            bounds.Bottom > source.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(bounds));
        }

        var output = new byte[checked(bounds.Width * bounds.Height)];
        var input = source.Pixels.Span;

        for (var y = 0; y < bounds.Height; y++)
        {
            var sourceOffset = (bounds.Y + y) * source.Width + bounds.X;
            var targetOffset = y * bounds.Width;
            input.Slice(sourceOffset, bounds.Width)
                .CopyTo(output.AsSpan(targetOffset, bounds.Width));
        }

        return new GrayImage(bounds.Width, bounds.Height, output);
    }

    private static List<(int X, int Y)> CollectDarkPoints(
        GrayImage source,
        int darkCutoff)
    {
        const int MaxPoints = 200_000;

        var totalPixels = checked(source.Width * source.Height);
        var stride = Math.Max(1, (int)Math.Sqrt(totalPixels / (double)MaxPoints));
        var pixels = source.Pixels.Span;
        var points = new List<(int X, int Y)>();

        for (var y = 0; y < source.Height; y += stride)
        {
            var row = y * source.Width;
            for (var x = 0; x < source.Width; x += stride)
            {
                if (pixels[row + x] <= darkCutoff)
                {
                    points.Add((x, y));
                }
            }
        }

        return points;
    }
}
