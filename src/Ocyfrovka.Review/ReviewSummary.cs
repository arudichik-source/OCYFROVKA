namespace Ocyfrovka.Review;

public sealed record ReviewSummary(
    ReviewSessionState State,
    int TotalCells,
    int ReviewedCells,
    int ConfirmedCells,
    int ErrorCells,
    int LowConfidenceCells);
