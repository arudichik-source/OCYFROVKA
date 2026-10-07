namespace Ocyfrovka.Layout;

public sealed record TableLayout(
    IReadOnlyList<TextLine> Lines,
    IReadOnlyList<TableRow> Rows,
    int ColumnCount,
    bool IsLikelyTable,
    int? HeaderRowIndex)
{
    public static TableLayout Empty { get; } =
        new([], [], 0, false, null);
}
