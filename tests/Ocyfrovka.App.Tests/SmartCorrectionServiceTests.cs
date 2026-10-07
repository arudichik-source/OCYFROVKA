using Ocyfrovka.App.Services;
using Ocyfrovka.Core.Ocr;
using Ocyfrovka.Dictionary;
using Ocyfrovka.Layout;
using Ocyfrovka.Review;

namespace Ocyfrovka.App.Tests;

public sealed class SmartCorrectionServiceTests
{
    [Fact]
    public void Service_AutoCorrectsExactAlias_SuggestsFuzzy_AndProtectsUserEdit()
    {
        var layout = new TableLayout(
            [],
            [
                new TableRow(0,
                [
                    Cell(0, 0, "Name", 95),
                    Cell(0, 1, "Description", 95),
                    Cell(0, 2, "Protected", 95)
                ]),
                new TableRow(1,
                [
                    Cell(1, 0, "Widget A", 70),
                    Cell(1, 1, "Portable scannr", 65),
                    Cell(1, 2, "Manual src", 80)
                ])
            ],
            3,
            true,
            0);

        var session = new ReviewTableSession(layout);
        session.Find(new ReviewCellKey(1, 2))!.ApplyUserEdit("User value");

        var catalog = new DictionaryCatalog(
        [
            new DictionaryEntry("Widget Alpha", ["Widget A"], "sample"),
            new DictionaryEntry("Portable scanner", [], "sample"),
            new DictionaryEntry("Manual source", ["Manual src"], "sample")
        ]);

        var result = SmartCorrectionService.ApplyNomenclatureDictionary(
            session,
            catalog);

        var exact = session.Find(new ReviewCellKey(1, 0))!;
        var fuzzy = session.Find(new ReviewCellKey(1, 1))!;
        var protectedCell = session.Find(new ReviewCellKey(1, 2))!;

        Assert.Equal(1, result.AutoCorrected);
        Assert.Equal(1, result.Suggested);
        Assert.Equal(1, result.Protected);

        Assert.Equal("Widget Alpha", exact.CurrentText);
        Assert.Equal(ReviewCellState.CorrectedAutomatically, exact.State);

        Assert.Equal("Portable scannr", fuzzy.CurrentText);
        Assert.Equal("Portable scanner", fuzzy.SuggestedText);
        Assert.Equal(ReviewCellState.Suggested, fuzzy.State);

        Assert.Equal("User value", protectedCell.CurrentText);
        Assert.Equal(ReviewCellState.CorrectedByUser, protectedCell.State);
    }

    [Fact]
    public void Service_DoesNotChooseBetweenEqualAmbiguousCandidates()
    {
        var layout = new TableLayout(
            [],
            [new TableRow(0, [Cell(0, 0, "shared", 60)])],
            1,
            true,
            null);

        var session = new ReviewTableSession(layout);
        var catalog = new DictionaryCatalog(
        [
            new DictionaryEntry("Alpha One", ["shared"]),
            new DictionaryEntry("Alpha Two", ["shared"])
        ]);

        var result = SmartCorrectionService.ApplyNomenclatureDictionary(
            session,
            catalog);

        var cell = session.Find(new ReviewCellKey(0, 0))!;

        Assert.Equal(1, result.Ambiguous);
        Assert.Equal("shared", cell.CurrentText);
        Assert.Equal(ReviewCellState.Recognized, cell.State);
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
            new LayoutBounds(word.X, word.Y, word.Width, word.Height),
            confidence,
            [word]);
    }
}
