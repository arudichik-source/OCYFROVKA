using Ocyfrovka.Core.Ocr;
using Ocyfrovka.Layout;
using Ocyfrovka.Persistence;
using Ocyfrovka.Review;

namespace Ocyfrovka.Core.Tests;

public sealed class OcyfrovkaDatabaseTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "ocyfrovka-db-tests",
        Guid.NewGuid().ToString("N"));

    public OcyfrovkaDatabaseTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void Initialize_CreatesPortableDatabase()
    {
        var path = Path.Combine(_root, "Data", "digitizer.db");
        var database = new OcyfrovkaDatabase(path);

        database.Initialize();

        Assert.True(File.Exists(path));
        Assert.Equal(Path.GetFullPath(path), database.DatabasePath);
    }

    [Fact]
    public void PageReview_RoundTripsManualAndConfirmedStateWithProvenance()
    {
        var database = CreateDatabase();
        var documentId = Guid.NewGuid();
        var pageId = Guid.NewGuid();

        var session = new ReviewTableSession(
            Layout(
                Cell(0, 0, "Name", 95),
                Cell(0, 1, "Qty", 92),
                Cell(1, 0, "OCR alpha", 61),
                Cell(1, 1, "12", 88),
                headerRowIndex: 0));

        var edited = session.Find(new ReviewCellKey(1, 0))!;
        edited.ApplyUserEdit("Alpha corrected");
        edited.Confirm();

        var quantity = session.Find(new ReviewCellKey(1, 1))!;
        quantity.SetSuggestion("12.0");

        var updatedUtc = new DateTimeOffset(
            2026, 10, 11, 8, 30, 0, TimeSpan.Zero);

        database.SavePageReview(
            new PersistedPageReview(
                documentId,
                pageId,
                @"C:\Input\sample.pdf",
                2,
                90,
                "raw OCR text",
                session.ToSnapshot(),
                updatedUtc));

        var restored = database.LoadPageReview(pageId);

        Assert.NotNull(restored);
        Assert.Equal(documentId, restored!.DocumentId);
        Assert.Equal(pageId, restored.PageId);
        Assert.Equal(2, restored.SourceFrameIndex);
        Assert.Equal(90, restored.RotationDegrees);
        Assert.Equal("raw OCR text", restored.OcrText);
        Assert.Equal(updatedUtc, restored.UpdatedUtc);
        Assert.NotNull(restored.Review);

        var restoredSession = ReviewTableSession.FromSnapshot(restored.Review!);
        var restoredEdited = restoredSession.Find(new ReviewCellKey(1, 0))!;
        var restoredQuantity = restoredSession.Find(new ReviewCellKey(1, 1))!;

        Assert.Equal("OCR alpha", restoredEdited.SourceText);
        Assert.Equal("Alpha corrected", restoredEdited.CurrentText);
        Assert.Equal(ReviewCellState.ConfirmedByUser, restoredEdited.State);
        Assert.Single(restoredEdited.SourceWords);
        Assert.Equal(61, restoredEdited.SourceWords[0].Confidence);

        Assert.Equal("12", restoredQuantity.CurrentText);
        Assert.Equal("12.0", restoredQuantity.SuggestedText);
        Assert.Equal(ReviewCellState.Suggested, restoredQuantity.State);
    }

    [Fact]
    public void SavingSamePage_ReplacesReviewCellsTransactionally()
    {
        var database = CreateDatabase();
        var documentId = Guid.NewGuid();
        var pageId = Guid.NewGuid();

        var first = new ReviewTableSession(
            Layout(
                Cell(0, 0, "A", 80),
                Cell(0, 1, "B", 80)));

        database.SavePageReview(
            Page(documentId, pageId, "first", first));

        var second = new ReviewTableSession(
            Layout(Cell(0, 0, "A2", 91)));

        database.SavePageReview(
            Page(documentId, pageId, "second", second));

        var restored = database.LoadPageReview(pageId)!;
        var session = ReviewTableSession.FromSnapshot(restored.Review!);

        Assert.Equal("second", restored.OcrText);
        Assert.NotNull(session.Find(new ReviewCellKey(0, 0)));
        Assert.Null(session.Find(new ReviewCellKey(0, 1)));
    }

    [Fact]
    public void CorrectionMemory_IncrementsHitsAndOrdersResults()
    {
        var database = CreateDatabase();

        database.RecordCorrection(
            "ocr-alpha",
            "Alpha",
            "Nomenclature",
            new DateTimeOffset(2026, 10, 10, 8, 0, 0, TimeSpan.Zero));

        database.RecordCorrection(
            "ocr-alpha",
            "Alpha",
            "Nomenclature",
            new DateTimeOffset(2026, 10, 11, 8, 0, 0, TimeSpan.Zero));

        database.RecordCorrection(
            "ocr-alpha",
            "Alternative",
            "Nomenclature",
            new DateTimeOffset(2026, 10, 11, 9, 0, 0, TimeSpan.Zero));

        var results = database.FindCorrections(
            "ocr-alpha",
            "Nomenclature");

        Assert.Equal(2, results.Count);
        Assert.Equal("Alpha", results[0].Canonical);
        Assert.Equal(2, results[0].HitCount);
        Assert.Equal("Alternative", results[1].Canonical);
        Assert.Equal(1, results[1].HitCount);
    }

    [Fact]
    public void LoadDocumentPages_ReturnsAllPersistedPages()
    {
        var database = CreateDatabase();
        var documentId = Guid.NewGuid();

        database.SavePageReview(
            Page(
                documentId,
                Guid.NewGuid(),
                "page one",
                new ReviewTableSession(Layout(Cell(0, 0, "A", 90))),
                sourceFrameIndex: 0));

        database.SavePageReview(
            Page(
                documentId,
                Guid.NewGuid(),
                "page two",
                new ReviewTableSession(Layout(Cell(0, 0, "B", 90))),
                sourceFrameIndex: 1));

        var pages = database.LoadDocumentPages(documentId);

        Assert.Equal(2, pages.Count);
        Assert.Equal(0, pages[0].SourceFrameIndex);
        Assert.Equal(1, pages[1].SourceFrameIndex);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private OcyfrovkaDatabase CreateDatabase()
    {
        var database = new OcyfrovkaDatabase(
            Path.Combine(_root, Guid.NewGuid().ToString("N"), "digitizer.db"));

        database.Initialize();
        return database;
    }

    private static PersistedPageReview Page(
        Guid documentId,
        Guid pageId,
        string ocrText,
        ReviewTableSession session,
        int sourceFrameIndex = 0)
        => new(
            documentId,
            pageId,
            @"C:\Input\document.pdf",
            sourceFrameIndex,
            0,
            ocrText,
            session.ToSnapshot(),
            DateTimeOffset.UtcNow);

    private static TableLayout Layout(
        TableCell first,
        params TableCell[] rest)
        => Layout([first, .. rest], null);

    private static TableLayout Layout(
        TableCell first,
        TableCell second,
        TableCell third,
        TableCell fourth,
        int? headerRowIndex)
        => Layout(
            [first, second, third, fourth],
            headerRowIndex);

    private static TableLayout Layout(
        IReadOnlyList<TableCell> cells,
        int? headerRowIndex)
    {
        var rows = cells
            .GroupBy(cell => cell.RowIndex)
            .OrderBy(group => group.Key)
            .Select(group => new TableRow(group.Key, group.ToArray()))
            .ToArray();

        var columnCount = cells.Count == 0
            ? 0
            : cells.Max(cell => cell.ColumnIndex) + 1;

        return new TableLayout(
            [],
            rows,
            columnCount,
            columnCount >= 2,
            headerRowIndex);
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
            column * 120,
            row * 30,
            80,
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
