using System.Text.RegularExpressions;

namespace Ocyfrovka.Dictionary;

public static partial class SmartCorrector
{
    public static IReadOnlyList<CorrectionSuggestion> Suggest(
        string? input,
        CorrectionFieldKind fieldKind,
        DictionaryCatalog catalog,
        int maxSuggestions = 3)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        if (maxSuggestions <= 0 || string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        var original = input.Trim();

        if (fieldKind == CorrectionFieldKind.Numeric)
        {
            var numeric = CorrectionNormalizer.CorrectNumericContext(original);

            return numeric.Changed
                ? [
                    new CorrectionSuggestion(
                        original,
                        numeric.Corrected,
                        1.0,
                        CorrectionMatchKind.ContextualNumeric,
                        true,
                        "numeric")
                  ]
                : [];
        }

        if (fieldKind != CorrectionFieldKind.Nomenclature ||
            catalog.Entries.Count == 0)
        {
            return [];
        }

        var normalized = CorrectionNormalizer.NormalizeComparable(
            original,
            CorrectionFieldKind.Nomenclature);

        if (normalized.Length == 0)
        {
            return [];
        }

        var exact = catalog.FindExact(normalized);
        if (exact.Count > 0)
        {
            var ambiguous = exact.Count > 1;

            return exact
                .Select(entry =>
                {
                    var canonicalNormalized =
                        CorrectionNormalizer.NormalizeComparable(
                            entry.Canonical,
                            CorrectionFieldKind.Nomenclature);

                    var kind = string.Equals(
                        canonicalNormalized,
                        normalized,
                        StringComparison.Ordinal)
                            ? CorrectionMatchKind.ExactCanonical
                            : CorrectionMatchKind.ExactAlias;

                    return new CorrectionSuggestion(
                        original,
                        entry.Canonical,
                        1.0,
                        kind,
                        !ambiguous &&
                        kind == CorrectionMatchKind.ExactAlias,
                        entry.Category);
                })
                .Take(maxSuggestions)
                .ToArray();
        }

        var candidates = catalog.Entries
            .Select(entry =>
            {
                var score = catalog
                    .EnumerateForms()
                    .Where(form => ReferenceEquals(form.Entry, entry) ||
                                   string.Equals(
                                       form.Entry.Canonical,
                                       entry.Canonical,
                                       StringComparison.Ordinal))
                    .Select(form => Score(normalized, form.Form))
                    .DefaultIfEmpty(0)
                    .Max();

                return new CorrectionSuggestion(
                    original,
                    entry.Canonical,
                    Math.Round(score, 4),
                    CorrectionMatchKind.Fuzzy,
                    false,
                    entry.Category);
            })
            .GroupBy(
                suggestion => CorrectionNormalizer.NormalizeComparable(
                    suggestion.Canonical,
                    CorrectionFieldKind.Nomenclature),
                StringComparer.Ordinal)
            .Select(group => group
                .OrderByDescending(item => item.Score)
                .First())
            .Where(suggestion =>
                suggestion.Score >= FuzzyThreshold(normalized.Length))
            .OrderByDescending(suggestion => suggestion.Score)
            .ThenBy(
                suggestion => suggestion.Canonical,
                StringComparer.OrdinalIgnoreCase)
            .Take(maxSuggestions)
            .ToArray();

        return candidates;
    }

    private static double Score(string input, string candidate)
    {
        var edit = Levenshtein.Similarity(input, candidate);
        var token = TokenSimilarity(input, candidate);
        var score = edit * 0.75 + token * 0.25;

        var inputNumbers = NumericTokenRegex()
            .Matches(input)
            .Cast<Match>()
            .Select(match => match.Value)
            .ToArray();

        var candidateNumbers = NumericTokenRegex()
            .Matches(candidate)
            .Cast<Match>()
            .Select(match => match.Value)
            .ToArray();

        if (inputNumbers.Length > 0 || candidateNumbers.Length > 0)
        {
            if (inputNumbers.SequenceEqual(
                    candidateNumbers,
                    StringComparer.Ordinal))
            {
                score = Math.Min(1.0, score + 0.08);
            }
            else
            {
                score *= 0.55;
            }
        }

        return Math.Clamp(score, 0, 1);
    }

    private static double TokenSimilarity(
        string input,
        string candidate)
    {
        var left = TokenRegex()
            .Matches(input)
            .Select(match => match.Value)
            .ToHashSet(StringComparer.Ordinal);

        var right = TokenRegex()
            .Matches(candidate)
            .Select(match => match.Value)
            .ToHashSet(StringComparer.Ordinal);

        if (left.Count == 0 && right.Count == 0)
        {
            return 1;
        }

        var union = left.Union(right, StringComparer.Ordinal).Count();
        if (union == 0)
        {
            return 0;
        }

        var intersection = left.Intersect(right, StringComparer.Ordinal).Count();
        return intersection / (double)union;
    }

    private static double FuzzyThreshold(int normalizedLength)
        => normalizedLength <= 4 ? 0.90 : 0.72;

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex TokenRegex();

    [GeneratedRegex(@"\d+(?:[,.]\d+)?")]
    private static partial Regex NumericTokenRegex();
}
