using Ocyfrovka.Core.Ocr;

namespace Ocyfrovka.Layout;

public static class TableLayoutAnalyzer
{
    private sealed record MutableLine(List<OcrWord> Words)
    {
        public double CenterY =>
            Words.Count == 0
                ? 0
                : Words.Average(word => word.Y + word.Height / 2.0);
    }

    private sealed record Chunk(
        int RowIndex,
        IReadOnlyList<OcrWord> Words,
        LayoutBounds Bounds,
        string Text,
        double Confidence);

    private sealed class AnchorCluster
    {
        public List<(int RowIndex, double X)> Samples { get; } = [];

        public double X => Samples.Average(sample => sample.X);

        public int DistinctRows =>
            Samples.Select(sample => sample.RowIndex).Distinct().Count();
    }

    public static TableLayout Analyze(IReadOnlyList<OcrWord> words)
    {
        ArgumentNullException.ThrowIfNull(words);

        var normalized = words
            .Where(word =>
                !string.IsNullOrWhiteSpace(word.Text) &&
                word.Width > 0 &&
                word.Height > 0)
            .ToArray();

        if (normalized.Length == 0)
        {
            return TableLayout.Empty;
        }

        var lines = BuildLines(normalized);
        if (lines.Count == 0)
        {
            return TableLayout.Empty;
        }

        var medianHeight = Median(normalized.Select(word => (double)word.Height));
        var chunks = BuildChunks(lines, medianHeight);
        var anchors = BuildColumnAnchors(chunks, lines.Count, medianHeight);

        if (anchors.Count < 2)
        {
            return new TableLayout(lines, [], anchors.Count, false, null);
        }

        var rows = BuildRows(chunks, anchors, medianHeight);
        var rowsWithMultipleCells = rows.Count(row => row.Cells.Count >= 2);
        var likelyTable = rowsWithMultipleCells >= 2;

        if (!likelyTable)
        {
            return new TableLayout(lines, rows, anchors.Count, false, null);
        }

        var headerRowIndex = DetectHeaderRow(rows);

        return new TableLayout(
            lines,
            rows,
            anchors.Count,
            true,
            headerRowIndex);
    }

    public static IReadOnlyList<TextLine> BuildLines(IReadOnlyList<OcrWord> words)
    {
        ArgumentNullException.ThrowIfNull(words);

        var normalized = words
            .Where(word =>
                !string.IsNullOrWhiteSpace(word.Text) &&
                word.Width > 0 &&
                word.Height > 0)
            .ToArray();

        if (normalized.Length == 0)
        {
            return [];
        }

        var medianHeight = Median(normalized.Select(word => (double)word.Height));
        var centerTolerance = Math.Max(2.0, medianHeight * 0.65);
        var mutableLines = new List<MutableLine>();

        foreach (var word in normalized
                     .OrderBy(word => word.Y + word.Height / 2.0)
                     .ThenBy(word => word.X))
        {
            var centerY = word.Y + word.Height / 2.0;

            var bestLine = mutableLines
                .Select(line => new
                {
                    Line = line,
                    Distance = Math.Abs(line.CenterY - centerY)
                })
                .Where(candidate => candidate.Distance <= centerTolerance)
                .OrderBy(candidate => candidate.Distance)
                .Select(candidate => candidate.Line)
                .FirstOrDefault();

            if (bestLine is null)
            {
                mutableLines.Add(new MutableLine([word]));
            }
            else
            {
                bestLine.Words.Add(word);
            }
        }

        return mutableLines
            .OrderBy(line => line.CenterY)
            .Select((line, index) =>
            {
                var ordered = line.Words.OrderBy(word => word.X).ToArray();
                return new TextLine(
                    index,
                    string.Join(' ', ordered.Select(word => word.Text.Trim())),
                    BoundsFor(ordered),
                    AverageConfidence(ordered),
                    ordered);
            })
            .ToArray();
    }

    private static IReadOnlyList<Chunk> BuildChunks(
        IReadOnlyList<TextLine> lines,
        double medianHeight)
    {
        var allPositiveGaps = lines
            .SelectMany(line => PositiveGaps(line.Words))
            .ToArray();

        var medianGap = allPositiveGaps.Length == 0
            ? medianHeight * 0.35
            : Median(allPositiveGaps);

        var splitThreshold = Math.Max(
            medianHeight * 1.35,
            Math.Max(8.0, medianGap * 2.75));

        var chunks = new List<Chunk>();

        foreach (var line in lines)
        {
            var ordered = line.Words.OrderBy(word => word.X).ToArray();
            if (ordered.Length == 0)
            {
                continue;
            }

            var current = new List<OcrWord> { ordered[0] };

            for (var i = 1; i < ordered.Length; i++)
            {
                var previous = ordered[i - 1];
                var next = ordered[i];
                var gap = next.X - (previous.X + previous.Width);

                if (gap > splitThreshold)
                {
                    chunks.Add(ToChunk(line.Index, current));
                    current = [];
                }

                current.Add(next);
            }

            chunks.Add(ToChunk(line.Index, current));
        }

        return chunks;
    }

    private static IReadOnlyList<double> PositiveGaps(
        IReadOnlyList<OcrWord> words)
    {
        var ordered = words.OrderBy(word => word.X).ToArray();
        var gaps = new List<double>();

        for (var i = 1; i < ordered.Length; i++)
        {
            var gap = ordered[i].X -
                      (ordered[i - 1].X + ordered[i - 1].Width);

            if (gap > 0)
            {
                gaps.Add(gap);
            }
        }

        return gaps;
    }

    private static Chunk ToChunk(
        int rowIndex,
        IReadOnlyList<OcrWord> words)
    {
        var ordered = words.OrderBy(word => word.X).ToArray();

        return new Chunk(
            rowIndex,
            ordered,
            BoundsFor(ordered),
            string.Join(' ', ordered.Select(word => word.Text.Trim())),
            AverageConfidence(ordered));
    }

    private static IReadOnlyList<double> BuildColumnAnchors(
        IReadOnlyList<Chunk> chunks,
        int rowCount,
        double medianHeight)
    {
        if (chunks.Count == 0)
        {
            return [];
        }

        var tolerance = Math.Max(10.0, medianHeight * 1.4);
        var clusters = new List<AnchorCluster>();

        foreach (var chunk in chunks.OrderBy(chunk => chunk.Bounds.X))
        {
            var candidate = clusters
                .Select(cluster => new
                {
                    Cluster = cluster,
                    Distance = Math.Abs(cluster.X - chunk.Bounds.X)
                })
                .Where(item => item.Distance <= tolerance)
                .OrderBy(item => item.Distance)
                .Select(item => item.Cluster)
                .FirstOrDefault();

            if (candidate is null)
            {
                candidate = new AnchorCluster();
                clusters.Add(candidate);
            }

            candidate.Samples.Add((chunk.RowIndex, chunk.Bounds.X));
        }

        var minRecurringRows = rowCount <= 1 ? 1 : 2;

        var anchors = clusters
            .Where(cluster => cluster.DistinctRows >= minRecurringRows)
            .OrderBy(cluster => cluster.X)
            .Select(cluster => cluster.X)
            .ToList();

        if (anchors.Count == 0)
        {
            return [];
        }

        return anchors;
    }

    private static IReadOnlyList<TableRow> BuildRows(
        IReadOnlyList<Chunk> chunks,
        IReadOnlyList<double> anchors,
        double medianHeight)
    {
        var assignmentTolerance = Math.Max(18.0, medianHeight * 2.4);
        var rows = new List<TableRow>();

        foreach (var group in chunks
                     .GroupBy(chunk => chunk.RowIndex)
                     .OrderBy(group => group.Key))
        {
            var byColumn = new Dictionary<int, List<Chunk>>();

            foreach (var chunk in group.OrderBy(chunk => chunk.Bounds.X))
            {
                var nearest = anchors
                    .Select((anchor, index) => new
                    {
                        Column = index,
                        Distance = Math.Abs(anchor - chunk.Bounds.X)
                    })
                    .OrderBy(item => item.Distance)
                    .First();

                if (nearest.Distance > assignmentTolerance)
                {
                    continue;
                }

                if (!byColumn.TryGetValue(nearest.Column, out var list))
                {
                    list = [];
                    byColumn[nearest.Column] = list;
                }

                list.Add(chunk);
            }

            var cells = byColumn
                .OrderBy(pair => pair.Key)
                .Select(pair =>
                {
                    var sourceWords = pair.Value
                        .SelectMany(chunk => chunk.Words)
                        .OrderBy(word => word.X)
                        .ToArray();

                    return new TableCell(
                        group.Key,
                        pair.Key,
                        string.Join(' ', pair.Value
                            .OrderBy(chunk => chunk.Bounds.X)
                            .Select(chunk => chunk.Text)),
                        BoundsFor(sourceWords),
                        AverageConfidence(sourceWords),
                        sourceWords);
                })
                .ToArray();

            rows.Add(new TableRow(group.Key, cells));
        }

        return rows;
    }

    private static int? DetectHeaderRow(
        IReadOnlyList<TableRow> rows)
    {
        if (rows.Count < 2)
        {
            return null;
        }

        var first = rows[0];
        var second = rows[1];

        if (first.Cells.Count < 2 ||
            second.Cells.Count < 2)
        {
            return null;
        }

        var firstNumeric = first.Cells.Count(cell => LooksNumeric(cell.Text));
        var secondNumeric = second.Cells.Count(cell => LooksNumeric(cell.Text));

        return firstNumeric == 0 && secondNumeric > 0
            ? first.Index
            : null;
    }

    private static bool LooksNumeric(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var digitCount = text.Count(char.IsDigit);
        var letterCount = text.Count(char.IsLetter);

        return digitCount > 0 && digitCount >= letterCount;
    }

    private static LayoutBounds BoundsFor(
        IReadOnlyList<OcrWord> words)
    {
        if (words.Count == 0)
        {
            return default;
        }

        var left = words.Min(word => word.X);
        var top = words.Min(word => word.Y);
        var right = words.Max(word => word.X + word.Width);
        var bottom = words.Max(word => word.Y + word.Height);

        return new LayoutBounds(
            left,
            top,
            right - left,
            bottom - top);
    }

    private static double AverageConfidence(
        IReadOnlyList<OcrWord> words)
    {
        var valid = words
            .Where(word => word.Confidence >= 0)
            .Select(word => word.Confidence)
            .ToArray();

        return valid.Length == 0
            ? 0
            : valid.Average();
    }

    private static double Median(IEnumerable<double> values)
    {
        var ordered = values.OrderBy(value => value).ToArray();

        if (ordered.Length == 0)
        {
            return 0;
        }

        var middle = ordered.Length / 2;

        return ordered.Length % 2 == 0
            ? (ordered[middle - 1] + ordered[middle]) / 2.0
            : ordered[middle];
    }
}
