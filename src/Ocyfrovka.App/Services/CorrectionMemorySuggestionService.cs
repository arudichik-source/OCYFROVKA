using Ocyfrovka.Dictionary;
using Ocyfrovka.Review;

namespace Ocyfrovka.App.Services;

internal sealed record CorrectionMemoryApplyResult(
    int Suggested,
    int Ambiguous,
    int Protected,
    int NoMatch);

internal static class CorrectionMemorySuggestionService
{
    public static CorrectionMemoryApplyResult Apply(
        ReviewTableSession session,
        ReviewPersistenceService persistence)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(persistence);

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

            var normalized = CorrectionNormalizer.NormalizeComparable(
                cell.CurrentText,
                CorrectionFieldKind.Nomenclature);

            if (normalized.Length == 0)
            {
                noMatch++;
                continue;
            }

            var matches = persistence.FindCorrections(
                normalized,
                CorrectionFieldKind.Nomenclature.ToString(),
                limit: 3);

            if (matches.Count == 0)
            {
                noMatch++;
                continue;
            }

            var top = matches[0];

            var tied = matches
                .Skip(1)
                .Any(other =>
                    other.HitCount == top.HitCount &&
                    !string.Equals(
                        other.Canonical,
                        top.Canonical,
                        StringComparison.OrdinalIgnoreCase));

            if (tied)
            {
                ambiguous++;
                continue;
            }

            if (string.Equals(
                    cell.CurrentText.Trim(),
                    top.Canonical,
                    StringComparison.Ordinal))
            {
                noMatch++;
                continue;
            }

            cell.SetSuggestion(top.Canonical);
            suggested++;
        }

        return new CorrectionMemoryApplyResult(
            suggested,
            ambiguous,
            protectedCells,
            noMatch);
    }
}
