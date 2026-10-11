using Ocyfrovka.Dictionary;

namespace Ocyfrovka.Core.Tests;

public sealed class SmartCorrectorTests
{
    [Fact]
    public void NomenclatureNormalization_UnifiesCyrillicLatinLookalikes()
    {
        var latin = CorrectionNormalizer.NormalizeComparable(
            "ABC-M795",
            CorrectionFieldKind.Nomenclature);

        var mixed = CorrectionNormalizer.NormalizeComparable(
            "АВС-М795",
            CorrectionFieldKind.Nomenclature);

        Assert.Equal(latin, mixed);
    }

    [Fact]
    public void NumericCorrection_IsAppliedOnlyInExplicitNumericContext()
    {
        var numeric = SmartCorrector.Suggest(
            "O1B,5",
            CorrectionFieldKind.Numeric,
            DictionaryCatalog.Empty);

        var text = SmartCorrector.Suggest(
            "O1B,5",
            CorrectionFieldKind.Text,
            DictionaryCatalog.Empty);

        var result = Assert.Single(numeric);
        Assert.Equal("018,5", result.Canonical);
        Assert.True(result.IsAutoCorrectionSafe);
        Assert.Equal(CorrectionMatchKind.ContextualNumeric, result.MatchKind);
        Assert.Empty(text);
    }

    [Fact]
    public void NumericCorrection_DoesNotMutateArbitraryLetters()
    {
        var result = CorrectionNormalizer.CorrectNumericContext("ABC");

        Assert.False(result.Changed);
        Assert.Equal("ABC", result.Corrected);
    }

    [Fact]
    public void ExactAlias_IsSafeAutoCorrectionWhenUnambiguous()
    {
        var catalog = new DictionaryCatalog(
        [
            new DictionaryEntry(
                "Widget 12,5",
                ["Widget 12.5", "WIDGET-12,5"],
                "sample")
        ]);

        var suggestions = SmartCorrector.Suggest(
            "WIDGET-12,5",
            CorrectionFieldKind.Nomenclature,
            catalog);

        var suggestion = Assert.Single(suggestions);
        Assert.Equal("Widget 12,5", suggestion.Canonical);
        Assert.Equal(CorrectionMatchKind.ExactAlias, suggestion.MatchKind);
        Assert.True(suggestion.IsAutoCorrectionSafe);
    }

    [Fact]
    public void ExactCanonical_IsRecognizedButNotPresentedAsAutoChange()
    {
        var catalog = new DictionaryCatalog(
        [
            new DictionaryEntry("Widget A", [], "sample")
        ]);

        var suggestion = Assert.Single(
            SmartCorrector.Suggest(
                "Widget A",
                CorrectionFieldKind.Nomenclature,
                catalog));

        Assert.Equal(CorrectionMatchKind.ExactCanonical, suggestion.MatchKind);
        Assert.False(suggestion.IsAutoCorrectionSafe);
    }

    [Fact]
    public void FuzzyMatch_ReturnsSuggestionButNeverSilentCorrection()
    {
        var catalog = new DictionaryCatalog(
        [
            new DictionaryEntry("Portable scanner", ["Portable scan"], "sample")
        ]);

        var suggestions = SmartCorrector.Suggest(
            "Portable scannr",
            CorrectionFieldKind.Nomenclature,
            catalog);

        var suggestion = Assert.Single(suggestions);
        Assert.Equal("Portable scanner", suggestion.Canonical);
        Assert.Equal(CorrectionMatchKind.Fuzzy, suggestion.MatchKind);
        Assert.False(suggestion.IsAutoCorrectionSafe);
        Assert.True(suggestion.Score >= 0.72);
    }

    [Fact]
    public void NumericTokenMismatch_IsStronglyPenalized()
    {
        var catalog = new DictionaryCatalog(
        [
            new DictionaryEntry("Widget 100", [], "sample"),
            new DictionaryEntry("Widget 200", [], "sample")
        ]);

        var suggestions = SmartCorrector.Suggest(
            "Widgt 100",
            CorrectionFieldKind.Nomenclature,
            catalog);

        Assert.NotEmpty(suggestions);
        Assert.Equal("Widget 100", suggestions[0].Canonical);
        Assert.DoesNotContain(
            suggestions,
            item => item.Canonical == "Widget 200");
    }

    [Fact]
    public void AmbiguousExactAlias_IsNeverAutoCorrected()
    {
        var catalog = new DictionaryCatalog(
        [
            new DictionaryEntry("Alpha One", ["shared"], null),
            new DictionaryEntry("Alpha Two", ["shared"], null)
        ]);

        var suggestions = SmartCorrector.Suggest(
            "shared",
            CorrectionFieldKind.Nomenclature,
            catalog);

        Assert.Equal(2, suggestions.Count);
        Assert.All(suggestions, suggestion =>
            Assert.False(suggestion.IsAutoCorrectionSafe));
    }
}
