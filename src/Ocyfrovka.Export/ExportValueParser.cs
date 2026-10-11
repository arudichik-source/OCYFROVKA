using System.Globalization;
using System.Text.RegularExpressions;

namespace Ocyfrovka.Export;

public static partial class ExportValueParser
{
    public static ExportValue Parse(string? value)
    {
        var text = (value ?? string.Empty).Trim();

        if (text.Length == 0)
        {
            return ExportValue.Empty;
        }

        if (!StrictNumberRegex().IsMatch(text))
        {
            return ExportValue.FromText(text);
        }

        var unsigned = text.TrimStart('+', '-');
        var integerPart = unsigned
            .Split(['.', ','], 2)[0];

        if (integerPart.Length > 1 &&
            integerPart.StartsWith('0'))
        {
            return ExportValue.FromText(text);
        }

        var digitCount = text.Count(char.IsDigit);
        if (digitCount > 15)
        {
            return ExportValue.FromText(text);
        }

        var normalized = text.Replace(',', '.');

        if (!double.TryParse(
                normalized,
                NumberStyles.AllowLeadingSign |
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var number) ||
            double.IsNaN(number) ||
            double.IsInfinity(number))
        {
            return ExportValue.FromText(text);
        }

        return ExportValue.FromNumber(text, number);
    }

    [GeneratedRegex(@"^[+-]?(?:0|[1-9]\d*)(?:[.,]\d+)?$")]
    private static partial Regex StrictNumberRegex();
}
