namespace Ocyfrovka.Review;

public sealed record ReviewDocumentSummary(
    ReviewSessionState State,
    int SessionCount,
    int TotalCells,
    int ReviewedCells,
    int ConfirmedCells,
    int ErrorCells,
    int LowConfidenceCells)
{
    public static ReviewDocumentSummary Create(
        IEnumerable<ReviewTableSession> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        var summaries = sessions
            .Select(session => session.GetSummary())
            .ToArray();

        if (summaries.Length == 0)
        {
            return new ReviewDocumentSummary(
                ReviewSessionState.New,
                0,
                0,
                0,
                0,
                0,
                0);
        }

        var total = summaries.Sum(summary => summary.TotalCells);
        var reviewed = summaries.Sum(summary => summary.ReviewedCells);
        var confirmed = summaries.Sum(summary => summary.ConfirmedCells);
        var errors = summaries.Sum(summary => summary.ErrorCells);
        var lowConfidence = summaries.Sum(summary => summary.LowConfidenceCells);

        var state = errors > 0
            ? ReviewSessionState.Error
            : total == 0
                ? ReviewSessionState.New
                : confirmed == total
                    ? ReviewSessionState.Reviewed
                    : ReviewSessionState.NeedsReview;

        return new ReviewDocumentSummary(
            state,
            summaries.Length,
            total,
            reviewed,
            confirmed,
            errors,
            lowConfidence);
    }
}
