namespace Ocyfrovka.Export;

public sealed record ExportValue(
    ExportValueKind Kind,
    string Text,
    double? Number)
{
    public static ExportValue Empty { get; } =
        new(ExportValueKind.Empty, string.Empty, null);

    public static ExportValue FromText(string value)
        => new(ExportValueKind.Text, value, null);

    public static ExportValue FromNumber(string original, double value)
        => new(ExportValueKind.Number, original, value);
}
