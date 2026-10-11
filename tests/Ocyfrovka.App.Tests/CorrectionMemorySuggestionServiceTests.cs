using Ocyfrovka.App.Services;
using Ocyfrovka.Core.Ocr;
using Ocyfrovka.Dictionary;
using Ocyfrovka.Layout;
using Ocyfrovka.Review;

namespace Ocyfrovka.App.Tests;

public sealed class CorrectionMemorySuggestionServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "ocyfrovka-memory-suggestion-tests",
        Guid.NewGuid().ToString("N"));

    public CorrectionMemorySuggestionServiceTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void MostFrequentMemory_BecomesSuggestionWithoutChangingText()
    {
        var persistence = new ReviewPersistenceService(_root);
        var normalized = CorrectionNormalizer.NormalizeComparable(
            "ocr alpha",
            CorrectionFieldKind.Nomenclature);

        persistence.RememberCorrection(
            normalized,
            "Alpha corrected",
            CorrectionFieldKind.Nomenclature.ToString());
        persistence.RememberCorrection(
            normalized,
            "Alpha corrected",
            CorrectionFieldKind.Nomenclature.ToString());
        persistence.RememberCorrection(
            normalized,
            "Alternative",
            CorrectionFieldKind.Nomenclature.ToString());

        var session = Session(Cell(0, 0, "ocr alpha", 55));

        var result = CorrectionMemorySuggestionService.Apply(
            session,
            persistence);

        var cell = session.Find(new ReviewCellKey(0, 0))!;

        Assert.Equal(1, result.Suggested);
        Assert.Equal("ocr alpha", cell.CurrentText);
        Assert.Equal("Alpha corrected", cell.SuggestedText);
        Assert.Equal(ReviewCellState.Suggested, cell.State);
    }

    [Fact]
    public void EqualHitCounts_AreLeftAmbiguous()
    {
        var persistence = new ReviewPersistenceService(_root);
        var normalized = CorrectionNormalizer.NormalizeComparable(
            "ocr beta",
            CorrectionFieldKind.Nomenclature);

        persistence.RememberCorrection(
            normalized,
            "Beta One",
            CorrectionFieldKind.Nomenclature.ToString());
        persistence.RememberCorrection(
            normalized,
            "Beta Two",
            CorrectionFieldKind.Nomenclature.ToString());

        var session = Session(Cell(0, 0, "ocr beta", 50));

        var result = CorrectionMemorySuggestionService.Apply(
            session,
            persistence);

        var cell = session.Find(new ReviewCellKey(0, 0))!;

        Assert.Equal(1, result.Ambiguous);
        Assert.Null(cell.SuggestedText);
        Assert.Equal(ReviewCellState.Recognized, cell.State);
    }

    [Fact]
    public void ConfirmedCell_IsNeverTouchedByCorrectionMemory()
    {
        var persistence = new ReviewPersistenceService(_root);
        var normalized = CorrectionNormalizer.NormalizeComparable(
            "ocr gamma",
            CorrectionFieldKind.Nomenclature);

        persistence.RememberCorrection(
            normalized,
            "Gamma corrected",
            CorrectionFieldKind.Nomenclature.ToString());

        var session = Session(Cell(0, 0, "ocr gamma", 40));
        var cell = session.Find(new ReviewCellKey(0, 0))!;
        cell.ApplyUserEdit("User final");
        cell.Confirm();

        var result = CorrectionMemorySuggestionService.Apply(
            session,
            persistence);

        Assert.Equal(1, result.Protected);
        Assert.Equal("User final", cell.CurrentText);
        Assert.Null(cell.SuggestedText);
        Assert.Equal(ReviewCellState.ConfirmedByUser, cell.State);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static ReviewTableSession Session(TableCell cell)
        => new(
            new TableLayout(
                [],
                [new TableRow(cell.RowIndex, [cell])],
                1,
                false,
                null));

    private static TableCell Cell(
        int row,
        int column,
        string text,
        double confidence)
    {
        var word = new OcrWord(
            text,
            confidence,
            10,
            10,
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
