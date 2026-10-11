using Ocyfrovka.Review;
using System.Text;

namespace Ocyfrovka.Export;

public static class DelimitedExporter
{
    public static void ExportCsv(
        ReviewTableSession session,
        string outputPath,
        ExportOptions? options = null,
        char delimiter = ';')
        => ExportDelimited(
            session,
            outputPath,
            options,
            delimiter,
            csvMode: true);

    public static void ExportText(
        ReviewTableSession session,
        string outputPath,
        ExportOptions? options = null)
        => ExportDelimited(
            session,
            outputPath,
            options,
            '\t',
            csvMode: false);

    private static void ExportDelimited(
        ReviewTableSession session,
        string outputPath,
        ExportOptions? options,
        char delimiter,
        bool csvMode)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException(
                "Output path is required.",
                nameof(outputPath));
        }

        options ??= new ExportOptions();
        var table = ReviewExportProjection.Create(
            session,
            options);

        var fullPath = Path.GetFullPath(outputPath);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var builder = new StringBuilder();

        builder.AppendLine(
            string.Join(
                delimiter,
                table.Headers.Select(value =>
                    Escape(
                        value,
                        delimiter,
                        csvMode))));

        foreach (var row in table.Rows)
        {
            var values = new string[
                table.Headers.Count];

            for (var column = 0;
                 column < values.Length;
                 column++)
            {
                var value = column < row.Values.Count
                    ? row.Values[column]
                    : string.Empty;

                values[column] = Escape(
                    value,
                    delimiter,
                    csvMode);
            }

            builder.AppendLine(
                string.Join(
                    delimiter,
                    values));
        }

        File.WriteAllText(
            fullPath,
            builder.ToString(),
            new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: true));
    }

    private static string Escape(
        string? value,
        char delimiter,
        bool csvMode)
    {
        var text = (value ?? string.Empty)
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');

        if (!csvMode)
        {
            return text
                .Replace('\t', ' ')
                .Replace('\n', ' ');
        }

        var parsed = ExportValueParser.Parse(text);

        if (parsed.Kind == ExportValueKind.Text &&
            IsFormulaLike(text))
        {
            text = "'" + text;
        }

        var mustQuote =
            text.Contains(delimiter) ||
            text.Contains('"') ||
            text.Contains('\n');

        if (!mustQuote)
        {
            return text;
        }

        const char quote = '"';

        return quote +
               text.Replace(
                   quote.ToString(),
                   new string(quote, 2)) +
               quote;
    }

    private static bool IsFormulaLike(string value)
    {
        var trimmed = value.TrimStart();

        if (trimmed.Length == 0)
        {
            return false;
        }

        return trimmed[0] is '=' or '+' or '-' or '@';
    }
}
