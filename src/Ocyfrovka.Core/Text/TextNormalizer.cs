using System.Text;
using System.Text.RegularExpressions;

namespace Ocyfrovka.Core.Text;

public static partial class TextNormalizer
{
    public static string NormalizeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormKC).Trim();
        normalized = WhitespaceRegex().Replace(normalized, " ");
        normalized = DecimalDotRegex().Replace(normalized, ",");
        normalized = CommaSpacingRegex().Replace(normalized, ",");
        return normalized;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"(?<=\d)\.(?=\d)")]
    private static partial Regex DecimalDotRegex();

    [GeneratedRegex(@"\s*,\s*")]
    private static partial Regex CommaSpacingRegex();
}