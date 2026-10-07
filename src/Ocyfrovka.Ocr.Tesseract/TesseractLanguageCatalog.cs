namespace Ocyfrovka.Ocr.Tesseract;

public sealed record TesseractLanguage(string Code, string DisplayName);

public static class TesseractLanguageCatalog
{
    private static readonly IReadOnlyDictionary<string, string> KnownNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ukr"] = "Українська",
            ["eng"] = "English",
            ["rus"] = "Русский",
            ["pol"] = "Polski",
            ["deu"] = "Deutsch",
            ["fra"] = "Français",
            ["spa"] = "Español",
            ["ita"] = "Italiano",
            ["ces"] = "Čeština",
            ["slk"] = "Slovenčina",
            ["ron"] = "Română",
            ["hun"] = "Magyar"
        };

    public static IReadOnlyList<TesseractLanguage> GetInstalledLanguages(string tessdataDirectory)
    {
        if (!Directory.Exists(tessdataDirectory))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(tessdataDirectory, "*.traineddata", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Where(code => !string.Equals(code, "osd", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .Select(code => new TesseractLanguage(code!, GetDisplayName(code!)))
            .ToArray();
    }

    public static IReadOnlyList<string> GetMissingLanguages(
        string tessdataDirectory,
        string languageExpression)
    {
        var requested = ParseExpression(languageExpression);
        var installed = GetInstalledLanguages(tessdataDirectory)
            .Select(x => x.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return requested.Where(code => !installed.Contains(code)).ToArray();
    }

    public static IReadOnlyList<string> ParseExpression(string languageExpression)
    {
        if (string.IsNullOrWhiteSpace(languageExpression))
        {
            return ["ukr"];
        }

        return languageExpression
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string GetDisplayName(string code)
        => KnownNames.TryGetValue(code, out var name) ? name : code;
}
