using Ocyfrovka.Layout;

namespace Ocyfrovka.Review;

public sealed class ReviewTableSession
{
    private readonly Dictionary<ReviewCellKey, ReviewCell> _cells = [];

    public ReviewTableSession(TableLayout layout)
    {
        Merge(layout);
    }

    public int ColumnCount { get; private set; }

    public int? HeaderRowIndex { get; private set; }

    public IReadOnlyList<ReviewRow> Rows =>
        _cells.Values
            .GroupBy(cell => cell.Key.RowIndex)
            .OrderBy(group => group.Key)
            .Select(group => new ReviewRow(
                group.Key,
                group.OrderBy(cell => cell.Key.ColumnIndex).ToArray()))
            .ToArray();

    public ReviewCell? Find(ReviewCellKey key)
        => _cells.GetValueOrDefault(key);

    public ReviewSummary GetSummary(double lowConfidenceThreshold = 70)
    {
        var reviewable = _cells.Values
            .Where(cell => cell.Key.RowIndex != HeaderRowIndex)
            .ToArray();

        if (reviewable.Length == 0)
        {
            return new ReviewSummary(
                ReviewSessionState.New,
                0,
                0,
                0,
                0,
                0);
        }

        var reviewed = reviewable.Count(cell =>
            cell.State != ReviewCellState.Recognized);
        var confirmed = reviewable.Count(cell =>
            cell.State == ReviewCellState.ConfirmedByUser);
        var errors = reviewable.Count(cell =>
            cell.State == ReviewCellState.Error);
        var lowConfidence = reviewable.Count(cell =>
            cell.State != ReviewCellState.ConfirmedByUser &&
            cell.Confidence >= 0 &&
            cell.Confidence < lowConfidenceThreshold);

        var state = errors > 0
            ? ReviewSessionState.Error
            : confirmed == reviewable.Length
                ? ReviewSessionState.Reviewed
                : ReviewSessionState.NeedsReview;

        return new ReviewSummary(
            state,
            reviewable.Length,
            reviewed,
            confirmed,
            errors,
            lowConfidence);
    }

    public void ConfirmAll()
    {
        foreach (var cell in _cells.Values
                     .Where(cell => cell.Key.RowIndex != HeaderRowIndex))
        {
            cell.Confirm();
        }
    }

    public void Merge(TableLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        ColumnCount = layout.ColumnCount;
        HeaderRowIndex = layout.HeaderRowIndex;

        var seen = new HashSet<ReviewCellKey>();

        foreach (var source in layout.Rows.SelectMany(row => row.Cells))
        {
            var key = new ReviewCellKey(source.RowIndex, source.ColumnIndex);
            seen.Add(key);

            if (_cells.TryGetValue(key, out var existing))
            {
                existing.RefreshRecognition(source);
            }
            else
            {
                _cells[key] = new ReviewCell(source);
            }
        }

        foreach (var stale in _cells.Keys.Where(key => !seen.Contains(key)).ToArray())
        {
            if (_cells[stale].State == ReviewCellState.ConfirmedByUser)
            {
                continue;
            }

            _cells.Remove(stale);
        }

        ColumnCount = Math.Max(
            layout.ColumnCount,
            _cells.Keys
                .Select(key => key.ColumnIndex + 1)
                .DefaultIfEmpty(0)
                .Max());
    }
}
