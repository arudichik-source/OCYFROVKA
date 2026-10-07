using Ocyfrovka.Core.Ocr;
using Ocyfrovka.Layout;
using Ocyfrovka.Review;

namespace Ocyfrovka.Core.Tests;

public sealed class ReviewTableSessionTests
{
    [Fact]
    public void UserEdit_BecomesProtectedFromRecognitionRefresh()
    {
        var session = new ReviewTableSession(Layout(Cell(0, 0, "old", 60)));
        var cell = session.Find(new ReviewCellKey(0, 0))!;

        cell.ApplyUserEdit("manual");
        session.Merge(Layout(Cell(0, 0, "new OCR", 95)));

        Assert.Equal("manual", cell.CurrentText);
        Assert.Equal("new OCR", cell.SourceText);
        Assert.Equal(95, cell.Confidence);
        Assert.Equal(ReviewCellState.CorrectedByUser, cell.State);
    }

    [Fact]
    public void ConfirmedValue_SurvivesRecognitionRefresh()
    {
        var session = new ReviewTableSession(Layout(Cell(0, 0, "first", 50)));
        var cell = session.Find(new ReviewCellKey(0, 0))!;

        cell.ApplyUserEdit("confirmed value");
        cell.Confirm();

        session.Merge(Layout(Cell(0, 0, "replacement OCR", 99)));

        Assert.Equal("confirmed value", cell.CurrentText);
        Assert.Equal("replacement OCR", cell.SourceText);
        Assert.Equal(ReviewCellState.ConfirmedByUser, cell.State);
    }

    [Fact]
    public void UnreviewedRecognition_IsRefreshed()
    {
        var session = new ReviewTableSession(Layout(Cell(0, 0, "first", 50)));

        session.Merge(Layout(Cell(0, 0, "second", 88)));

        var cell = session.Find(new ReviewCellKey(0, 0))!;
        Assert.Equal("second", cell.CurrentText);
        Assert.Equal("second", cell.SourceText);
        Assert.Equal(88, cell.Confidence);
        Assert.Equal(ReviewCellState.Recognized, cell.State);
    }

    [Fact]
    public void AutomaticCorrection_CannotOverwriteConfirmedCell()
    {
        var session = new ReviewTableSession(Layout(Cell(0, 0, "ocr", 70)));
        var cell = session.Find(new ReviewCellKey(0, 0))!;

        cell.ApplyUserEdit("user");
        cell.Confirm();
        cell.ApplyAutomaticCorrection("automatic");

        Assert.Equal("user", cell.CurrentText);
        Assert.Equal(ReviewCellState.ConfirmedByUser, cell.State);
    }

    [Fact]
    public void Suggestion_DoesNotOverwriteCurrentTextUntilAccepted()
    {
        var session = new ReviewTableSession(Layout(Cell(0, 0, "ocr", 70)));
        var cell = session.Find(new ReviewCellKey(0, 0))!;

        cell.SetSuggestion("proposal");

        Assert.Equal("ocr", cell.CurrentText);
        Assert.Equal("proposal", cell.SuggestedText);
        Assert.Equal(ReviewCellState.Suggested, cell.State);

        cell.AcceptSuggestion();

        Assert.Equal("proposal", cell.CurrentText);
        Assert.Null(cell.SuggestedText);
        Assert.Equal(ReviewCellState.CorrectedByUser, cell.State);
    }

    [Fact]
    public void StaleConfirmedCell_IsRetained()
    {
        var session = new ReviewTableSession(Layout(
            Cell(0, 0, "A", 80),
            Cell(0, 1, "B", 80)));

        var protectedCell = session.Find(new ReviewCellKey(0, 1))!;
        protectedCell.Confirm();

        session.Merge(Layout(Cell(0, 0, "A2", 90)));

        Assert.NotNull(session.Find(new ReviewCellKey(0, 1)));
        Assert.Equal(ReviewCellState.ConfirmedByUser,
            session.Find(new ReviewCellKey(0, 1))!.State);
    }

    [Fact]
    public void StaleUnreviewedCell_IsRemoved()
    {
        var session = new ReviewTableSession(Layout(
            Cell(0, 0, "A", 80),
            Cell(0, 1, "B", 80)));

        session.Merge(Layout(Cell(0, 0, "A2", 90)));

        Assert.Null(session.Find(new ReviewCellKey(0, 1)));
    }

    private static TableLayout Layout(params TableCell[] cells)
    {
        var rows = cells
            .GroupBy(cell => cell.RowIndex)
            .OrderBy(group => group.Key)
            .Select(group => new TableRow(group.Key, group.ToArray()))
            .ToArray();

        var columnCount = cells.Length == 0
            ? 0
            : cells.Max(cell => cell.ColumnIndex) + 1;

        return new TableLayout([], rows, columnCount, columnCount >= 2, null);
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
