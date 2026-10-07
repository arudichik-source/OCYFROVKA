using Ocyfrovka.Review;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ocyfrovka.App.Review;

internal sealed class ReviewGridCellViewModel : INotifyPropertyChanged
{
    private readonly ReviewCell _cell;

    public ReviewGridCellViewModel(ReviewCell cell)
    {
        _cell = cell ?? throw new ArgumentNullException(nameof(cell));
    }

    public int RowIndex => _cell.Key.RowIndex;

    public int ColumnIndex => _cell.Key.ColumnIndex;

    public double Confidence => _cell.Confidence;

    public bool IsLowConfidence =>
        _cell.State != ReviewCellState.ConfirmedByUser &&
        _cell.Confidence >= 0 &&
        _cell.Confidence < 70;

    public bool IsConfirmed => _cell.State == ReviewCellState.ConfirmedByUser;

    public bool IsError => _cell.State == ReviewCellState.Error;

    public bool HasSuggestion =>
        _cell.State == ReviewCellState.Suggested &&
        !string.IsNullOrWhiteSpace(_cell.SuggestedText);

    public bool IsUserCorrected =>
        _cell.State == ReviewCellState.CorrectedByUser;

    public string? SuggestedText => _cell.SuggestedText;

    public string StateText => _cell.State switch
    {
        ReviewCellState.Recognized => "Розпізнано",
        ReviewCellState.CorrectedAutomatically => "Автовиправлено",
        ReviewCellState.Suggested => "Пропозиція",
        ReviewCellState.CorrectedByUser => "Виправлено користувачем",
        ReviewCellState.ConfirmedByUser => "Підтверджено",
        ReviewCellState.Error => "Помилка",
        _ => _cell.State.ToString()
    };

    public string Text
    {
        get => _cell.CurrentText;
        set
        {
            var normalized = (value ?? string.Empty).Trim();
            if (string.Equals(normalized, _cell.CurrentText, StringComparison.Ordinal))
            {
                return;
            }

            _cell.ApplyUserEdit(normalized);
            RaiseAll();
        }
    }

    public string Tooltip =>
        $"Confidence: {Confidence:0.#}% · {StateText}" +
        (string.Equals(_cell.SourceText, _cell.CurrentText, StringComparison.Ordinal)
            ? string.Empty
            : $" · OCR: {_cell.SourceText}");

    public void Confirm()
    {
        _cell.Confirm();
        RaiseAll();
    }

    public void AcceptSuggestion()
    {
        _cell.AcceptSuggestion();
        RaiseAll();
    }

    public void MarkError()
    {
        _cell.MarkError();
        RaiseAll();
    }

    public void ResetToRecognition()
    {
        _cell.ResetToRecognition();
        RaiseAll();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void RaiseAll()
    {
        OnPropertyChanged(nameof(Text));
        OnPropertyChanged(nameof(Confidence));
        OnPropertyChanged(nameof(IsLowConfidence));
        OnPropertyChanged(nameof(IsConfirmed));
        OnPropertyChanged(nameof(IsError));
        OnPropertyChanged(nameof(HasSuggestion));
        OnPropertyChanged(nameof(IsUserCorrected));
        OnPropertyChanged(nameof(SuggestedText));
        OnPropertyChanged(nameof(StateText));
        OnPropertyChanged(nameof(Tooltip));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
