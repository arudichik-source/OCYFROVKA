using ClosedXML.Excel;
using Ocyfrovka.Core.Ocr;
using Ocyfrovka.Export;
using Ocyfrovka.Layout;
using Ocyfrovka.Review;

namespace Ocyfrovka.Core.Tests;

public sealed class XlsxExporterTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "ocyfrovka-xlsx-tests",
        Guid.NewGuid().ToString("N"));

    public XlsxExporterTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void Export_WritesUnicodeNumericAndProtectedTextTypes()
    {
        var session = Session();
        session.ConfirmAll();

        var path = Path.Combine(_root, "result.xlsx");

        XlsxExporter.Export(
            session,
            path,
            new ExportOptions(
                IncludeMetadata: true));

        Assert.True(File.Exists(path));

        using var workbook = new XLWorkbook(path);

        var data = workbook.Worksheet("Дані");

        Assert.Equal("Назва", data.Cell("A1").GetString());
        Assert.Equal("Тест Ω", data.Cell("A2").GetString());

        Assert.Equal(
            XLDataType.Number,
            data.Cell("B2").DataType);
        Assert.Equal(
            12.5,
            data.Cell("B2").GetDouble(),
            precision: 8);

        Assert.Equal(
            XLDataType.Text,
            data.Cell("C2").DataType);
        Assert.Equal(
            "00123",
            data.Cell("C2").GetString());

        Assert.Equal(
            XLDataType.Text,
            data.Cell("D2").DataType);
        Assert.Equal(
            "=1+1",
            data.Cell("D2").GetString());
        Assert.False(data.Cell("D2").HasFormula);

        Assert.Equal(
            XLDataType.Text,
            data.Cell("C3").DataType);
        Assert.Equal("123", data.Cell("C3").GetString());
        Assert.Equal(
            XLDataType.Number,
            data.Cell("B3").DataType);

        var metadata = workbook.Worksheet("Метадані");
        Assert.Equal(
            "Поточне значення",
            metadata.Cell("D1").GetString());
        Assert.Equal(
            ReviewCellState.ConfirmedByUser.ToString(),
            metadata.Cell("G2").GetString());
    }

    [Fact]
    public void MetadataSheet_IsNotCreatedByDefault()
    {
        var session = Session();
        session.ConfirmAll();

        var path = Path.Combine(_root, "no-metadata.xlsx");

        XlsxExporter.Export(session, path);

        using var workbook = new XLWorkbook(path);

        Assert.Single(workbook.Worksheets);
        Assert.NotNull(workbook.Worksheet("Дані"));
    }

    [Fact]
    public void Export_RejectsUnreviewedSession()
    {
        var path = Path.Combine(_root, "blocked.xlsx");

        Assert.Throws<InvalidOperationException>(
            () => XlsxExporter.Export(
                Session(),
                path));

        Assert.False(File.Exists(path));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static ReviewTableSession Session()
    {
        var layout = new TableLayout(
            [],
            [
                new TableRow(
                    0,
                    [
                        Cell(0, 0, "Назва", 95),
                        Cell(0, 1, "Кількість", 95),
                        Cell(0, 2, "Код", 95),
                        Cell(0, 3, "Текст", 95)
                    ]),
                new TableRow(
                    1,
                    [
                        Cell(1, 0, "Тест Ω", 92),
                        Cell(1, 1, "12,5", 92),
                        Cell(1, 2, "00123", 92),
                        Cell(1, 3, "=1+1", 92)
                    ]),
                new TableRow(
                    2,
                    [
                        Cell(2, 0, "Інший зразок", 95),
                        Cell(2, 1, "8", 95),
                        Cell(2, 2, "123", 95),
                        Cell(2, 3, "Текст", 95)
                    ])
            ],
            4,
            true,
            0);

        return new ReviewTableSession(layout);
    }

    private static TableCell Cell(
        int row,
        int column,
        string text,
        double confidence)
    {
        var word = new OcrWord(
            text,
            confidence,
            column * 100,
            row * 30,
            70,
            12);

        return new TableCell(
            row,
            column,
            text,
            new LayoutBounds(
                word.X,
                word.Y,
                word.Width,
                word.Height),
            confidence,
            [word]);
    }
}
