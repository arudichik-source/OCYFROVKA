using Ocyfrovka.App.Services;
using Ocyfrovka.Core.Documents;
using Ocyfrovka.Core.Ocr;
using Ocyfrovka.Layout;
using Ocyfrovka.Review;

namespace Ocyfrovka.App.Tests;

public sealed class ReviewPersistenceServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "ocyfrovka-review-persistence-tests",
        Guid.NewGuid().ToString("N"));

    public ReviewPersistenceServiceTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void SaveAndRestore_PreservesOcrAndProtectedReviewState()
    {
        var source = Path.Combine(_root, "source.png");
        File.WriteAllText(source, "synthetic");

        var page = new DocumentPage(
            source,
            0,
            100,
            100,
            id: SourcePageIdentity.CreatePageId(source, 0));

        page.RotateClockwise();

        var session = new ReviewTableSession(
            new TableLayout(
                [],
                [
                    new TableRow(
                        0,
                        [
                            Cell(0, 0, "OCR value", 62)
                        ])
                ],
                1,
                false,
                null));

        var cell = session.Find(new ReviewCellKey(0, 0))!;
        cell.ApplyUserEdit("manual value");
        cell.Confirm();

        var service = new ReviewPersistenceService(_root);
        service.Save(page, "raw OCR", session);

        var restored = service.Restore(page);

        Assert.NotNull(restored);
        Assert.Equal("raw OCR", restored!.OcrText);
        Assert.Equal(90, restored.RotationDegrees);
        Assert.NotNull(restored.ReviewSession);

        var restoredCell = restored.ReviewSession!
            .Find(new ReviewCellKey(0, 0))!;

        Assert.Equal("OCR value", restoredCell.SourceText);
        Assert.Equal("manual value", restoredCell.CurrentText);
        Assert.Equal(ReviewCellState.ConfirmedByUser, restoredCell.State);
    }

    [Fact]
    public void CorrectionMemory_RoundTripsThroughService()
    {
        var service = new ReviewPersistenceService(_root);

        service.RememberCorrection(
            "OCR ALPHA",
            "Alpha",
            "Nomenclature");

        service.RememberCorrection(
            "OCR ALPHA",
            "Alpha",
            "Nomenclature");

        var matches = service.FindCorrections(
            "OCR ALPHA",
            "Nomenclature");

        var match = Assert.Single(matches);
        Assert.Equal("Alpha", match.Canonical);
        Assert.Equal(2, match.HitCount);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
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
            10,
            10,
            60,
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
