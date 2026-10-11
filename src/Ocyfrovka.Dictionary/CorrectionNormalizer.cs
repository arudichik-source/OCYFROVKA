using Ocyfrovka.Core.Text;
using System.Text;
using System.Text.RegularExpressions;

namespace Ocyfrovka.Dictionary;

public static partial class CorrectionNormalizer
{
    public static string NormalizeComparable(
        string? value,
        CorrectionFieldKind kind)
    {
        var normalized = TextNormalizer.NormalizeName(value);
        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        normalized = normalized
            .Normalize(NormalizationForm.FormKC)
            .Replace('–', '-')
            .Replace('—', '-')
            .Replace('−', '-')
            .ToUpperInvariant();

        normalized = kind == CorrectionFieldKind.Nomenclature
            ? NormalizeVisualLookalikes(normalized)
            : normalized;

        normalized = PunctuationSpacingRegex().Replace(normalized, "$1");
        normalized = WhitespaceRegex().Replace(normalized, " ").Trim();

        return normalized;
    }

    public static NumericCorrectionResult CorrectNumericContext(string? value)
    {
        var original = TextNormalizer.NormalizeName(value);
        if (original.Length == 0)
        {
            return new NumericCorrectionResult(
                original,
                original,
                false);
        }

        var builder = new StringBuilder(original.Length);

        foreach (var ch in original)
        {
            builder.Append(ch switch
            {
                'O' or 'o' or 'О' or 'о' => '0',
                'I' or 'i' or 'І' or 'і' or 'L' or 'l' or '|' => '1',
                'S' or 's' => '5',
                'B' or 'b' or 'В' or 'в' => '8',
                _ => ch
            });
        }

        var candidate = TextNormalizer.NormalizeName(builder.ToString());

        if (!NumericOnlyRegex().IsMatch(candidate))
        {
            return new NumericCorrectionResult(
                original,
                original,
                false);
        }

        return new NumericCorrectionResult(
            original,
            candidate,
            !string.Equals(original, candidate, StringComparison.Ordinal));
    }

    private static string NormalizeVisualLookalikes(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var ch in value)
        {
            builder.Append(ch switch
            {
                'А' => 'A',
                'В' => 'B',
                'Е' => 'E',
                'К' => 'K',
                'М' => 'M',
                'Н' => 'H',
                'О' => 'O',
                'Р' => 'P',
                'С' => 'C',
                'Т' => 'T',
                'Х' => 'X',
                'У' => 'Y',
                'І' => 'I',
                _ => ch
            });
        }

        return builder.ToString();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"\s*([\-/,;:()])\s*")]
    private static partial Regex PunctuationSpacingRegex();

    [GeneratedRegex(@"^[0-9\s,.+\-/%()]+$")]
    private static partial Regex NumericOnlyRegex();
}
