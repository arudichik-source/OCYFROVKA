using Ocyfrovka.Core.Ocr;
using Ocyfrovka.Export;
using Ocyfrovka.Layout;
using Ocyfrovka.Review;

namespace Ocyfrovka.Core.Tests;

public sealed class ReviewExportProjectionTests
{
    [Fact]
    public void Projection_UsesCurrentReviewedValuesAndUniqueHeaders()
    {
        var session = BuildSession();
        var edited = session.Find(new ReviewCellKey(1, 0))!;
        edited.ApplyUserEdit("Виправлене Ω");
        session.ConfirmAll();

        var table = ReviewExportProjection.Create(session);

        Assert.Equal(
            new[] { "Назва", "Назва (2)", "Код" },
            table.Headers);
        Assert.Single(table.Rows);
        Assert.Equal("Виправлене Ω", table.Rows[0].Values[0]);
        Assert.Equal("12,5", table.Rows[0].Values[1]);
        Assert.Equal("00123", table.Rows[0].Values[2]);
    }

    [Fact]
    public void Projection_RejectsUnreviewedSessionByDefault()
    {
        var session = BuildSession();

        var error = Assert.Throws<InvalidOperationException>(
            () => ReviewExportProjection.Create(session));

        Assert.Contains(
            "не повністю перевірена",
            error.Message);
    }

    [Fact]
    public void Projection_CanBeCreatedForDiagnosticExportWhenExplicitlyAllowed()
    {
        var session = BuildSession();

        var table = ReviewExportProjection.Create(
            session,
            new ExportOptions(RequireReviewed: false));

        Assert.Single(table.Rows);
    }

    private static ReviewTableSession BuildSession()
    {
        var layout = new TableLayout(
            [],
            [
                new TableRow(
                    0,
                    [
                        Cell(0, 0, "Назва", 95),
                        Cell(0, 1, "Назва", 95),
                        Cell(0, 2, "Код", 95)
                    ]),
                new TableRow(
                    1,
                    [
                        Cell(1, 0, "OCR Ω", 80),
                        Cell(1, 1, "12,5", 90),
                        Cell(1, 2, "00123", 90)
                    ])
            ],
            3,
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
