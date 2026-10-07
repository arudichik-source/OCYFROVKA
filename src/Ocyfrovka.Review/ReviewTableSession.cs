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
    }
}
