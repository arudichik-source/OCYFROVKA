namespace Ocyfrovka.App.Review;

internal sealed class ReviewGridRowViewModel
{
    public ReviewGridRowViewModel(
        int index,
        ReviewGridCellViewModel?[] cells)
    {
        Index = index;
        Cells = cells;
    }

    public int Index { get; }

    public ReviewGridCellViewModel?[] Cells { get; }
}
