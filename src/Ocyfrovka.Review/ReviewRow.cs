namespace Ocyfrovka.Review;

public sealed record ReviewRow(
    int Index,
    IReadOnlyList<ReviewCell> Cells);
