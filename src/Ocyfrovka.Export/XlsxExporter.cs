using ClosedXML.Excel;
using Ocyfrovka.Review;

namespace Ocyfrovka.Export;

public static class XlsxExporter
{
    public static void Export(
        ReviewTableSession session,
        string outputPath,
        ExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException(
                "Output path is required.",
                nameof(outputPath));
        }

        options ??= new ExportOptions();
        var table = ReviewExportProjection.Create(
            session,
            options);

        var fullPath = Path.GetFullPath(outputPath);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var workbook = new XLWorkbook();
        workbook.Properties.Title = "ОЦИФРОВКА";
        workbook.Properties.Subject =
            "Локально оцифрована та перевірена таблиця";

        var dataSheet = workbook.Worksheets.Add(
            SanitizeSheetName(
                options.DataSheetName,
                "Дані"));

        WriteDataSheet(
            dataSheet,
            table);

        if (options.IncludeMetadata)
        {
            var metadataName = SanitizeSheetName(
                options.MetadataSheetName,
                "Метадані");

            if (string.Equals(
                    metadataName,
                    dataSheet.Name,
                    StringComparison.OrdinalIgnoreCase))
            {
                metadataName = UniqueSheetName(
                    workbook,
                    "Метадані");
            }

            var metadataSheet =
                workbook.Worksheets.Add(metadataName);

            WriteMetadataSheet(
                metadataSheet,
                table);
        }

        workbook.SaveAs(fullPath);
    }

    private static void WriteDataSheet(
        IXLWorksheet sheet,
        ExportTable table)
    {
        for (var column = 0;
             column < table.Headers.Count;
             column++)
        {
            var cell = sheet.Cell(1, column + 1);
            cell.Value = table.Headers[column];
            cell.DataType = XLDataType.Text;
        }

        for (var rowIndex = 0;
             rowIndex < table.Rows.Count;
             rowIndex++)
        {
            var row = table.Rows[rowIndex];

            for (var column = 0;
                 column < table.Headers.Count;
                 column++)
            {
                var text = column < row.Values.Count
                    ? row.Values[column]
                    : string.Empty;

                WriteTypedValue(
                    sheet.Cell(rowIndex + 2, column + 1),
                    text);
            }
        }

        var lastRow = Math.Max(
            1,
            table.Rows.Count + 1);

        var lastColumn = table.Headers.Count;
        var used = sheet.Range(
            1,
            1,
            lastRow,
            lastColumn);

        var header = sheet.Range(
            1,
            1,
            1,
            lastColumn);

        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Fill.BackgroundColor =
            XLColor.FromHtml("#315A7D");
        header.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;
        header.Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Center;

        used.Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Top;
        used.Style.Alignment.WrapText = true;
        used.Style.Border.OutsideBorder =
            XLBorderStyleValues.Thin;
        used.Style.Border.InsideBorder =
            XLBorderStyleValues.Hair;

        sheet.SheetView.FreezeRows(1);

        if (table.Rows.Count > 0)
        {
            used.SetAutoFilter();
        }

        for (var column = 1;
             column <= lastColumn;
             column++)
        {
            var worksheetColumn = sheet.Column(column);
            worksheetColumn.AdjustToContents();

            worksheetColumn.Width = Math.Clamp(
                worksheetColumn.Width,
                10,
                40);
        }

        sheet.Row(1).Height = 22;
    }

    private static void WriteTypedValue(
        IXLCell cell,
        string? text)
    {
        var parsed = ExportValueParser.Parse(text);

        switch (parsed.Kind)
        {
            case ExportValueKind.Empty:
                cell.Clear(XLClearOptions.Contents);
                break;

            case ExportValueKind.Number:
                cell.Value = parsed.Number!.Value;
                cell.Style.NumberFormat.Format =
                    "0.################";
                break;

            default:
                cell.Value = parsed.Text;
                cell.DataType = XLDataType.Text;
                break;
        }
    }

    private static void WriteMetadataSheet(
        IXLWorksheet sheet,
        ExportTable table)
    {
        var headers = new[]
        {
            "Рядок",
            "Колонка",
            "Заголовок",
            "Поточне значення",
            "OCR-джерело",
            "Confidence",
            "Стан",
            "Пропозиція"
        };

        for (var column = 0;
             column < headers.Length;
             column++)
        {
            var cell = sheet.Cell(1, column + 1);
            cell.Value = headers[column];
            cell.DataType = XLDataType.Text;
        }

        var rowNumber = 2;

        foreach (var row in table.SourceSession.Rows)
        {
            if (row.Index ==
                table.SourceSession.HeaderRowIndex)
            {
                continue;
            }

            foreach (var cell in row.Cells
                         .OrderBy(value =>
                             value.Key.ColumnIndex))
            {
                var header =
                    (uint)cell.Key.ColumnIndex <
                    (uint)table.Headers.Count
                        ? table.Headers[
                            cell.Key.ColumnIndex]
                        : $"Колонка {cell.Key.ColumnIndex + 1}";

                sheet.Cell(rowNumber, 1).Value =
                    cell.Key.RowIndex + 1;
                sheet.Cell(rowNumber, 2).Value =
                    cell.Key.ColumnIndex + 1;

                SetText(
                    sheet.Cell(rowNumber, 3),
                    header);
                SetText(
                    sheet.Cell(rowNumber, 4),
                    cell.CurrentText);
                SetText(
                    sheet.Cell(rowNumber, 5),
                    cell.SourceText);

                sheet.Cell(rowNumber, 6).Value =
                    cell.Confidence;
                sheet.Cell(rowNumber, 6)
                    .Style.NumberFormat.Format =
                    "0.0";

                SetText(
                    sheet.Cell(rowNumber, 7),
                    cell.State.ToString());

                SetText(
                    sheet.Cell(rowNumber, 8),
                    cell.SuggestedText ?? string.Empty);

                rowNumber++;
            }
        }

        var lastRow = Math.Max(
            1,
            rowNumber - 1);

        var used = sheet.Range(
            1,
            1,
            lastRow,
            headers.Length);

        var headerRange = sheet.Range(
            1,
            1,
            1,
            headers.Length);

        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor =
            XLColor.White;
        headerRange.Style.Fill.BackgroundColor =
            XLColor.FromHtml("#5B6573");

        used.Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Top;
        used.Style.Alignment.WrapText = true;

        sheet.SheetView.FreezeRows(1);

        for (var column = 1;
             column <= headers.Length;
             column++)
        {
            var worksheetColumn = sheet.Column(column);
            worksheetColumn.AdjustToContents();

            worksheetColumn.Width = Math.Clamp(
                worksheetColumn.Width,
                9,
                45);
        }
    }

    private static void SetText(
        IXLCell cell,
        string value)
    {
        cell.Value = value ?? string.Empty;
        cell.DataType = XLDataType.Text;
    }

    private static string SanitizeSheetName(
        string? value,
        string fallback)
    {
        var name = string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();

        foreach (var invalid in
                 new[] { '[', ']', ':', '*', '?', '/', '\\' })
        {
            name = name.Replace(
                invalid,
                '_');
        }

        if (name.Length > 31)
        {
            name = name[..31];
        }

        return string.IsNullOrWhiteSpace(name)
            ? fallback
            : name;
    }

    private static string UniqueSheetName(
        XLWorkbook workbook,
        string baseName)
    {
        var candidate = baseName;
        var suffix = 2;

        while (workbook.Worksheets.Any(
                   sheet => string.Equals(
                       sheet.Name,
                       candidate,
                       StringComparison.OrdinalIgnoreCase)))
        {
            var suffixText = $" ({suffix++})";
            var prefixLength = Math.Min(
                baseName.Length,
                31 - suffixText.Length);

            candidate =
                baseName[..prefixLength] +
                suffixText;
        }

        return candidate;
    }
}
