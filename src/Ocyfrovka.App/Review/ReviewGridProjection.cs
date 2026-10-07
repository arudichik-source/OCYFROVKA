using Ocyfrovka.Review;

namespace Ocyfrovka.App.Review;

internal sealed record ReviewGridProjection(
    IReadOnlyList<string> Headers,
    IReadOnlyList<ReviewGridRowViewModel> Rows,
    int ColumnCount,
    ReviewSessionState SessionState,
    int ReviewedCount,
    int ConfirmedCount,
    int ErrorCount,
    int LowConfidenceCount)
{
    public static ReviewGridProjection Create(ReviewTableSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var allRows = session.Rows;
        var columnCount = Math.Max(
            session.ColumnCount,
            allRows
                .SelectMany(row => row.Cells)
                .Select(cell => cell.Key.ColumnIndex + 1)
                .DefaultIfEmpty(0)
                .Max());

        if (columnCount <= 0)
        {
            return new ReviewGridProjection(
                [],
                [],
                0,
                ReviewSessionState.New,
                0,
                0,
                0,
                0);
        }

        var headerRow = session.HeaderRowIndex is int headerIndex
            ? allRows.FirstOrDefault(row => row.Index == headerIndex)
            : null;

        var headers = new string[columnCount];
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var column = 0; column < columnCount; column++)
        {
            var proposed = headerRow?.Cells
                .FirstOrDefault(cell => cell.Key.ColumnIndex == column)?
                .CurrentText
                .Trim();

            var baseName = string.IsNullOrWhiteSpace(proposed)
                ? $"Колонка {column + 1}"
                : proposed;

            headers[column] = MakeUnique(baseName, used);
        }

        var rows = allRows
            .Where(row => row.Index != session.HeaderRowIndex)
            .Select(row =>
            {
                var cells = new ReviewGridCellViewModel?[columnCount];

                foreach (var cell in row.Cells)
                {
                    if ((uint)cell.Key.ColumnIndex < (uint)cells.Length)
                    {
                        cells[cell.Key.ColumnIndex] =
                            new ReviewGridCellViewModel(cell);
                    }
                }

                return new ReviewGridRowViewModel(row.Index, cells);
            })
            .ToArray();

        var summary = session.GetSummary();

        return new ReviewGridProjection(
            headers,
            rows,
            columnCount,
            summary.State,
            summary.ReviewedCells,
            summary.ConfirmedCells,
            summary.ErrorCells,
            summary.LowConfidenceCells);
    }

    private static string MakeUnique(
        string baseName,
        ISet<string> used)
    {
        var candidate = baseName;
        var suffix = 2;

        while (!used.Add(candidate))
        {
            candidate = $"{baseName} ({suffix++})";
        }

        return candidate;
    }
}
