using Ocyfrovka.Core.Ocr;
using Ocyfrovka.Layout;

namespace Ocyfrovka.Core.Tests;

public sealed class TableLayoutAnalyzerTests
{
    [Fact]
    public void BuildLines_GroupsWordsByVerticalGeometry()
    {
        var words = new[]
        {
            Word("Alpha", 10, 10, 40, 12),
            Word("12", 160, 10, 18, 12),
            Word("Beta", 10, 40, 35, 12),
            Word("7", 160, 40, 9, 12)
        };

        var lines = TableLayoutAnalyzer.BuildLines(words);

        Assert.Equal(2, lines.Count);
        Assert.Equal("Alpha 12", lines[0].Text);
        Assert.Equal("Beta 7", lines[1].Text);
        Assert.Equal(4, lines.Sum(line => line.Words.Count));
    }

    [Fact]
    public void Analyze_ReconstructsStableThreeColumnTable()
    {
        var words = new[]
        {
            Word("Item", 10, 10, 38, 12),
            Word("Qty", 180, 10, 28, 12),
            Word("Unit", 300, 10, 35, 12),

            Word("Alpha", 10, 40, 48, 12),
            Word("12", 180, 40, 18, 12),
            Word("pcs", 300, 40, 25, 12),

            Word("Beta", 10, 70, 40, 12),
            Word("7", 180, 70, 9, 12),
            Word("pcs", 300, 70, 25, 12)
        };

        var layout = TableLayoutAnalyzer.Analyze(words);

        Assert.True(layout.IsLikelyTable);
        Assert.Equal(3, layout.ColumnCount);
        Assert.Equal(3, layout.Rows.Count);
        Assert.Equal(0, layout.HeaderRowIndex);

        Assert.Equal("Alpha", Cell(layout, 1, 0).Text);
        Assert.Equal("12", Cell(layout, 1, 1).Text);
        Assert.Equal("pcs", Cell(layout, 1, 2).Text);
    }

    [Fact]
    public void Analyze_PreservesCellConfidenceAndSourceWords()
    {
        var words = new[]
        {
            Word("Name", 10, 10, 35, 12, 92),
            Word("Count", 170, 10, 40, 12, 90),
            Word("Long", 10, 40, 35, 12, 80),
            Word("name", 50, 40, 35, 12, 60),
            Word("3", 170, 40, 9, 12, 100)
        };

        var layout = TableLayoutAnalyzer.Analyze(words);
        var cell = Cell(layout, 1, 0);

        Assert.Equal("Long name", cell.Text);
        Assert.Equal(70, cell.Confidence, precision: 3);
        Assert.Equal(2, cell.SourceWords.Count);
    }

    [Fact]
    public void Analyze_DoesNotInventTableForPlainParagraph()
    {
        var words = new[]
        {
            Word("This", 10, 10, 30, 12),
            Word("is", 45, 10, 12, 12),
            Word("plain", 62, 10, 35, 12),
            Word("text", 102, 10, 28, 12),
            Word("Second", 10, 35, 45, 12),
            Word("line", 60, 35, 25, 12)
        };

        var layout = TableLayoutAnalyzer.Analyze(words);

        Assert.False(layout.IsLikelyTable);
        Assert.Equal(2, layout.Lines.Count);
    }

    private static TableCell Cell(
        TableLayout layout,
        int row,
        int column)
        => layout.Rows.Single(item => item.Index == row)
            .Cells.Single(cell => cell.ColumnIndex == column);

    private static OcrWord Word(
        string text,
        int x,
        int y,
        int width,
        int height,
        double confidence = 90)
        => new(text, confidence, x, y, width, height);
}
