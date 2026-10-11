using Ocyfrovka.Core.Ocr;
using Ocyfrovka.Export;
using Ocyfrovka.Layout;
using Ocyfrovka.Review;
using System.Text;

namespace Ocyfrovka.Core.Tests;

public sealed class DelimitedExporterTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "ocyfrovka-delimited-tests",
        Guid.NewGuid().ToString("N"));

    public DelimitedExporterTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void Csv_UsesUtf8BomAndNeutralizesFormulaLikeText()
    {
        var session = Session();
        session.ConfirmAll();

        var path = Path.Combine(_root, "result.csv");
        DelimitedExporter.ExportCsv(session, path);

        var bytes = File.ReadAllBytes(path);
        Assert.True(
            bytes.Length >= 3 &&
            bytes[0] == 0xEF &&
            bytes[1] == 0xBB &&
            bytes[2] == 0xBF);

        var text = File.ReadAllText(
            path,
            Encoding.UTF8);

        Assert.Contains("Тест Ω", text);
        Assert.Contains("'=1+1", text);
        Assert.Contains("Назва;Текст", text);
    }

    [Fact]
    public void TextExport_IsTabDelimitedAndSingleLinePerRow()
    {
        var session = Session();
        session.ConfirmAll();

        var path = Path.Combine(_root, "result.txt");
        DelimitedExporter.ExportText(session, path);

        var lines = File.ReadAllLines(
            path,
            Encoding.UTF8);

        Assert.Equal(2, lines.Length);
        Assert.Equal("Назва\tТекст", lines[0]);
        Assert.Equal("Тест Ω\t=1+1", lines[1]);
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
                        Cell(0, 0, "Назва"),
                        Cell(0, 1, "Текст")
                    ]),
                new TableRow(
                    1,
                    [
                        Cell(1, 0, "Тест Ω"),
                        Cell(1, 1, "=1+1")
                    ])
            ],
            2,
            true,
            0);

        return new ReviewTableSession(layout);
    }

    private static TableCell Cell(
        int row,
        int column,
        string text)
    {
        var word = new OcrWord(
            text,
            90,
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
            90,
            [word]);
    }
}
