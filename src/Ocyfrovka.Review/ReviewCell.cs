using Ocyfrovka.Core.Ocr;
using Ocyfrovka.Layout;

namespace Ocyfrovka.Review;

public sealed class ReviewCell
{
    private ReviewCell(
        ReviewCellKey key,
        string sourceText,
        string currentText,
        string? suggestedText,
        double confidence,
        LayoutBounds bounds,
        IReadOnlyList<OcrWord> sourceWords,
        ReviewCellState state)
    {
        Key = key;
        SourceText = sourceText;
        CurrentText = currentText;
        SuggestedText = suggestedText;
        Confidence = confidence;
        Bounds = bounds;
        SourceWords = sourceWords.ToArray();
        State = state;
    }

    public ReviewCell(TableCell source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Key = new ReviewCellKey(source.RowIndex, source.ColumnIndex);
        SourceText = source.Text;
        CurrentText = source.Text;
        Confidence = source.Confidence;
        Bounds = source.Bounds;
        SourceWords = source.SourceWords.ToArray();
        State = ReviewCellState.Recognized;
    }

    public ReviewCellKey Key { get; }

    public string SourceText { get; private set; }

    public string CurrentText { get; private set; }

    public string? SuggestedText { get; private set; }

    public double Confidence { get; private set; }

    public LayoutBounds Bounds { get; private set; }

    public IReadOnlyList<OcrWord> SourceWords { get; private set; }

    public ReviewCellState State { get; private set; }

    public bool IsProtectedFromRecognitionRefresh =>
        State is ReviewCellState.CorrectedByUser or ReviewCellState.ConfirmedByUser;

    public ReviewCellSnapshot ToSnapshot()
        => new(
            Key.RowIndex,
            Key.ColumnIndex,
            SourceText,
            CurrentText,
            SuggestedText,
            Confidence,
            Bounds,
            SourceWords.ToArray(),
            State);

    public static ReviewCell FromSnapshot(ReviewCellSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new ReviewCell(
            new ReviewCellKey(snapshot.RowIndex, snapshot.ColumnIndex),
            snapshot.SourceText ?? string.Empty,
            snapshot.CurrentText ?? string.Empty,
            snapshot.SuggestedText,
            snapshot.Confidence,
            snapshot.Bounds,
            snapshot.SourceWords ?? [],
            snapshot.State);
    }

    public void RefreshRecognition(TableCell source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.RowIndex != Key.RowIndex || source.ColumnIndex != Key.ColumnIndex)
        {
            throw new InvalidOperationException("Recognition cell identity does not match review cell identity.");
        }

        SourceText = source.Text;
        Confidence = source.Confidence;
        Bounds = source.Bounds;
        SourceWords = source.SourceWords.ToArray();

        if (IsProtectedFromRecognitionRefresh)
        {
            return;
        }

        CurrentText = source.Text;
        SuggestedText = null;
        State = ReviewCellState.Recognized;
    }

    public void ApplyAutomaticCorrection(string text)
    {
        if (IsProtectedFromRecognitionRefresh)
        {
            return;
        }

        CurrentText = Normalize(text);
        SuggestedText = null;
        State = ReviewCellState.CorrectedAutomatically;
    }

    public void SetSuggestion(string text)
    {
        if (IsProtectedFromRecognitionRefresh)
        {
            return;
        }

        SuggestedText = Normalize(text);
        State = ReviewCellState.Suggested;
    }

    public void AcceptSuggestion()
    {
        if (string.IsNullOrWhiteSpace(SuggestedText))
        {
            return;
        }

        CurrentText = SuggestedText;
        SuggestedText = null;
        State = ReviewCellState.CorrectedByUser;
    }

    public void ApplyUserEdit(string text)
    {
        CurrentText = Normalize(text);
        SuggestedText = null;
        State = ReviewCellState.CorrectedByUser;
    }

    public void Confirm()
    {
        SuggestedText = null;
        State = ReviewCellState.ConfirmedByUser;
    }

    public void MarkError()
    {
        SuggestedText = null;
        State = ReviewCellState.Error;
    }

    public void ResetToRecognition()
    {
        CurrentText = SourceText;
        SuggestedText = null;
        State = ReviewCellState.Recognized;
    }

    private static string Normalize(string? text)
        => (text ?? string.Empty).Trim();
}
