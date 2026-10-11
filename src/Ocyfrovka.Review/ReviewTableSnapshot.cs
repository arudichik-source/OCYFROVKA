namespace Ocyfrovka.Review;

public sealed record ReviewTableSnapshot(
    int ColumnCount,
    int? HeaderRowIndex,
    IReadOnlyList<ReviewCellSnapshot> Cells);
