using Ocyfrovka.Dictionary;
using Ocyfrovka.Review;

namespace Ocyfrovka.App.Services;

internal sealed record SmartCorrectionBatchResult(
    int AutoCorrected,
    int Suggested,
    int Ambiguous,
    int Protected,
    int NoMatch);

internal static class SmartCorrectionService
{
    public static SmartCorrectionBatchResult ApplyNomenclatureDictionary(
        ReviewTableSession session,
        DictionaryCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(catalog);

        var autoCorrected = 0;
        var suggested = 0;
        var ambiguous = 0;
        var protectedCells = 0;
        var noMatch = 0;

        foreach (var cell in session.Rows
                     .Where(row => row.Index != session.HeaderRowIndex)
                     .SelectMany(row => row.Cells))
        {
            if (cell.IsProtectedFromRecognitionRefresh)
            {
                protectedCells++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(cell.CurrentText) ||
                !cell.CurrentText.Any(char.IsLetter))
            {
                noMatch++;
                continue;
            }

            var candidates = SmartCorrector.Suggest(
                cell.CurrentText,
                CorrectionFieldKind.Nomenclature,
                catalog,
                maxSuggestions: 3);

            if (candidates.Count == 0)
            {
                noMatch++;
                continue;
            }

            var top = candidates[0];

            if (top.MatchKind == CorrectionMatchKind.ExactCanonical)
            {
                noMatch++;
                continue;
            }

            if (top.IsAutoCorrectionSafe)
            {
                cell.ApplyAutomaticCorrection(top.Canonical);
                autoCorrected++;
                continue;
            }

            var hasEqualAlternative = candidates
                .Skip(1)
                .Any(candidate =>
                    Math.Abs(candidate.Score - top.Score) < 0.0001 &&
                    !string.Equals(
                        candidate.Canonical,
                        top.Canonical,
                        StringComparison.OrdinalIgnoreCase));

            if (hasEqualAlternative)
            {
                ambiguous++;
                continue;
            }

            cell.SetSuggestion(top.Canonical);
            suggested++;
        }

        return new SmartCorrectionBatchResult(
            autoCorrected,
            suggested,
            ambiguous,
            protectedCells,
            noMatch);
    }
}
