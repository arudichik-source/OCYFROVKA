using Ocyfrovka.App.Review;
using Ocyfrovka.Core.Ocr;
using Ocyfrovka.Layout;
using Ocyfrovka.Review;

namespace Ocyfrovka.App.Tests;

public sealed class ReviewGridProjectionTests
{
    [Fact]
    public void Projection_UsesHeaderAndReportsReviewCounts()
    {
        var layout = new TableLayout(
            [],
            [
                new TableRow(0,
                [
                    Cell(0, 0, "Name", 95),
                    Cell(0, 1, "Qty", 95)
                ]),
                new TableRow(1,
                [
                    Cell(1, 0, "Alpha", 92),
                    Cell(1, 1, "12", 55)
                ])
            ],
            2,
            true,
            0);

        var session = new ReviewTableSession(layout);
        session.Find(new ReviewCellKey(1, 0))!.Confirm();

        var projection = ReviewGridProjection.Create(session);

        Assert.Equal(new[] { "Name", "Qty" }, projection.Headers);
        Assert.Single(projection.Rows);
        Assert.Equal(2, projection.ColumnCount);
        Assert.Equal(1, projection.ConfirmedCount);
        Assert.Equal(1, projection.ReviewedCount);
        Assert.Equal(1, projection.LowConfidenceCount);
        Assert.True(projection.Rows[0].Cells[0]!.IsConfirmed);
        Assert.True(projection.Rows[0].Cells[1]!.IsLowConfidence);
    }

    [Fact]
    public void Projection_MakesDuplicateHeadersUnique()
    {
        var layout = new TableLayout(
            [],
            [
                new TableRow(0,
                [
                    Cell(0, 0, "Value", 90),
                    Cell(0, 1, "Value", 90)
                ]),
                new TableRow(1,
                [
                    Cell(1, 0, "A", 90),
                    Cell(1, 1, "B", 90)
                ])
            ],
            2,
            true,
            0);

        var projection = ReviewGridProjection.Create(
            new ReviewTableSession(layout));

        Assert.Equal("Value", projection.Headers[0]);
        Assert.Equal("Value (2)", projection.Headers[1]);
    }

    private static TableCell Cell(
        int row,
        int column,
        string text,
        double confidence)
    {
        var word = new OcrWord(text, confidence, column * 100, row * 30, 50, 12);

        return new TableCell(
            row,
            column,
            text,
            new LayoutBounds(word.X, word.Y, word.Width, word.Height),
            confidence,
            [word]);
    }
}
