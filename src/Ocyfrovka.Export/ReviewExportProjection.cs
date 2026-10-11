using Ocyfrovka.Review;

namespace Ocyfrovka.Export;

public static class ReviewExportProjection
{
    public static ExportTable Create(
        ReviewTableSession session,
        ExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        options ??= new ExportOptions();

        var summary = session.GetSummary();

        if (options.RequireReviewed &&
            summary.State != ReviewSessionState.Reviewed)
        {
            throw new InvalidOperationException(
                "Таблиця ще не повністю перевірена. Підтвердьте всі значення перед експортом.");
        }

        var rows = session.Rows;
        var columnCount = Math.Max(
            session.ColumnCount,
            rows.SelectMany(row => row.Cells)
                .Select(cell => cell.Key.ColumnIndex + 1)
                .DefaultIfEmpty(0)
                .Max());

        if (columnCount <= 0)
        {
            throw new InvalidOperationException(
                "Немає структурованих даних для експорту.");
        }

        var headerRow = session.HeaderRowIndex is int headerIndex
            ? rows.FirstOrDefault(row => row.Index == headerIndex)
            : null;

        var headers = BuildHeaders(
            headerRow,
            columnCount);

        var exportRows = rows
            .Where(row => row.Index != session.HeaderRowIndex)
            .Select(row =>
            {
                var values = Enumerable
                    .Repeat(string.Empty, columnCount)
                    .ToArray();

                foreach (var cell in row.Cells)
                {
                    if ((uint)cell.Key.ColumnIndex < (uint)values.Length)
                    {
                        values[cell.Key.ColumnIndex] =
                            cell.CurrentText ?? string.Empty;
                    }
                }

                return new ExportRow(
                    row.Index,
                    values);
            })
            .ToArray();

        return new ExportTable(
            headers,
            exportRows,
            session);
    }

    private static IReadOnlyList<string> BuildHeaders(
        ReviewRow? headerRow,
        int columnCount)
    {
        var headers = new string[columnCount];
        var used = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        for (var column = 0; column < columnCount; column++)
        {
            var proposed = headerRow?.Cells
                .FirstOrDefault(cell =>
                    cell.Key.ColumnIndex == column)?
                .CurrentText
                .Trim();

            var baseName = string.IsNullOrWhiteSpace(proposed)
                ? $"Колонка {column + 1}"
                : proposed;

            var candidate = baseName;
            var suffix = 2;

            while (!used.Add(candidate))
            {
                candidate = $"{baseName} ({suffix++})";
            }

            headers[column] = candidate;
        }

        return headers;
    }
}
