namespace Ocyfrovka.Imaging;

public static class ImagePreprocessor
{
    public static PreprocessingResult Process(
        GrayImage source,
        PreprocessingProfile profile = PreprocessingProfile.Auto)
    {
        ArgumentNullException.ThrowIfNull(source);

        var threshold = ComputeOtsuThreshold(source);
        var quality = EvaluateQuality(source);

        var output = profile switch
        {
            PreprocessingProfile.Grayscale => Clone(source),
            PreprocessingProfile.HighContrast => ContrastStretch(source),
            PreprocessingProfile.Binary => Threshold(source, threshold),
            PreprocessingProfile.Sharpened => Sharpen(source),
            _ => Sharpen(ContrastStretch(source))
        };

        return new PreprocessingResult(output, quality, threshold, profile);
    }

    public static GrayImage ContrastStretch(GrayImage source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var histogram = BuildHistogram(source);
        var total = source.Pixels.Length;
        var lowTarget = Math.Max(1, total / 100);
        var highTarget = Math.Max(1, total / 100);

        var low = 0;
        var cumulative = 0;
        for (; low < 255; low++)
        {
            cumulative += histogram[low];
            if (cumulative >= lowTarget) break;
        }

        var high = 255;
        cumulative = 0;
        for (; high > 0; high--)
        {
            cumulative += histogram[high];
            if (cumulative >= highTarget) break;
        }

        if (high <= low)
        {
            return Clone(source);
        }

        var input = source.Pixels.Span;
        var output = new byte[input.Length];
        var scale = 255.0 / (high - low);

        for (var i = 0; i < input.Length; i++)
        {
            var value = input[i];
            output[i] = value <= low
                ? (byte)0
                : value >= high
                    ? (byte)255
                    : (byte)Math.Clamp(
                        (int)Math.Round((value - low) * scale),
                        0,
                        255);
        }

        return new GrayImage(source.Width, source.Height, output);
    }

    public static GrayImage Threshold(GrayImage source, int threshold)
    {
        ArgumentNullException.ThrowIfNull(source);

        threshold = Math.Clamp(threshold, 0, 255);
        var input = source.Pixels.Span;
        var output = new byte[input.Length];

        for (var i = 0; i < input.Length; i++)
        {
            output[i] = input[i] <= threshold ? (byte)0 : (byte)255;
        }

        return new GrayImage(source.Width, source.Height, output);
    }

    public static GrayImage Sharpen(GrayImage source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.Width < 3 || source.Height < 3)
        {
            return Clone(source);
        }

        var input = source.Pixels.Span;
        var output = source.ToArray();
        var width = source.Width;
        var height = source.Height;

        for (var y = 1; y < height - 1; y++)
        {
            for (var x = 1; x < width - 1; x++)
            {
                var index = y * width + x;
                var value =
                    5 * input[index]
                    - input[index - 1]
                    - input[index + 1]
                    - input[index - width]
                    - input[index + width];

                output[index] = (byte)Math.Clamp(value, 0, 255);
            }
        }

        return new GrayImage(width, height, output);
    }

    public static GrayImage MedianDenoise3x3(GrayImage source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.Width < 3 || source.Height < 3)
        {
            return Clone(source);
        }

        var input = source.Pixels.Span;
        var output = source.ToArray();
        var width = source.Width;
        var height = source.Height;
        Span<byte> values = stackalloc byte[9];

        for (var y = 1; y < height - 1; y++)
        {
            for (var x = 1; x < width - 1; x++)
            {
                var position = 0;
                for (var dy = -1; dy <= 1; dy++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        values[position++] = input[(y + dy) * width + x + dx];
                    }
                }

                values.Sort();
                output[y * width + x] = values[4];
            }
        }

        return new GrayImage(width, height, output);
    }

    public static int ComputeOtsuThreshold(GrayImage source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var histogram = BuildHistogram(source);
        var total = source.Pixels.Length;

        long weightedSum = 0;
        for (var i = 0; i < histogram.Length; i++)
        {
            weightedSum += (long)i * histogram[i];
        }

        long backgroundWeight = 0;
        long backgroundSum = 0;
        var bestThreshold = 127;
        var bestVariance = -1.0;

        for (var threshold = 0; threshold < 256; threshold++)
        {
            backgroundWeight += histogram[threshold];
            if (backgroundWeight == 0) continue;

            var foregroundWeight = total - backgroundWeight;
            if (foregroundWeight == 0) break;

            backgroundSum += (long)threshold * histogram[threshold];

            var meanBackground = backgroundSum / (double)backgroundWeight;
            var meanForeground = (weightedSum - backgroundSum) / (double)foregroundWeight;
            var difference = meanBackground - meanForeground;
            var variance = backgroundWeight * (double)foregroundWeight * difference * difference;

            if (variance > bestVariance)
            {
                bestVariance = variance;
                bestThreshold = threshold;
            }
        }

        return bestThreshold;
    }

    public static ImageQualityReport EvaluateQuality(GrayImage source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var pixels = source.Pixels.Span;
        if (pixels.Length == 0)
        {
            return new ImageQualityReport(0, 0, 0, 0, ["Порожнє зображення."]);
        }

        double sum = 0;
        for (var i = 0; i < pixels.Length; i++)
        {
            sum += pixels[i];
        }

        var mean = sum / pixels.Length;

        double varianceSum = 0;
        for (var i = 0; i < pixels.Length; i++)
        {
            var delta = pixels[i] - mean;
            varianceSum += delta * delta;
        }

        var standardDeviation = Math.Sqrt(varianceSum / pixels.Length);
        var edgeScoreRaw = ComputeEdgeScore(source);

        var brightness = mean / 255.0 * 100.0;
        var contrast = Math.Clamp(standardDeviation / 64.0 * 100.0, 0, 100);
        var edgeScore = Math.Clamp(edgeScoreRaw / 32.0 * 100.0, 0, 100);

        var brightnessPenalty = Math.Clamp(
            100.0 - Math.Abs(brightness - 55.0) * 1.6,
            0,
            100);

        var overall = Math.Clamp(
            contrast * 0.35 + edgeScore * 0.45 + brightnessPenalty * 0.20,
            0,
            100);

        var warnings = new List<string>();
        if (brightness < 25) warnings.Add("Зображення занадто темне.");
        if (brightness > 90) warnings.Add("Зображення може бути пересвічене.");
        if (contrast < 18) warnings.Add("Низький контраст.");
        if (edgeScore < 12) warnings.Add("Зображення може бути розмитим.");
        if (overall < 35) warnings.Add("Якість зображення низька для OCR.");

        return new ImageQualityReport(
            Math.Round(brightness, 1),
            Math.Round(contrast, 1),
            Math.Round(edgeScore, 1),
            Math.Round(overall, 1),
            warnings);
    }

    private static double ComputeEdgeScore(GrayImage source)
    {
        if (source.Width < 2 || source.Height < 2)
        {
            return 0;
        }

        var pixels = source.Pixels.Span;
        var width = source.Width;
        var height = source.Height;
        long totalDifference = 0;
        long samples = 0;

        for (var y = 0; y < height - 1; y++)
        {
            for (var x = 0; x < width - 1; x++)
            {
                var index = y * width + x;
                totalDifference += Math.Abs(pixels[index] - pixels[index + 1]);
                totalDifference += Math.Abs(pixels[index] - pixels[index + width]);
                samples += 2;
            }
        }

        return samples == 0 ? 0 : totalDifference / (double)samples;
    }

    private static int[] BuildHistogram(GrayImage source)
    {
        var histogram = new int[256];
        foreach (var value in source.Pixels.Span)
        {
            histogram[value]++;
        }

        return histogram;
    }

    private static GrayImage Clone(GrayImage source)
        => new(source.Width, source.Height, source.Pixels.Span);
}
