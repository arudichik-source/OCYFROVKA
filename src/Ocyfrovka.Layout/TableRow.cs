namespace Ocyfrovka.Layout;

public sealed record TableRow(
    int Index,
    IReadOnlyList<TableCell> Cells);
