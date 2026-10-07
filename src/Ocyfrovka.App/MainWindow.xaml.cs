using Microsoft.Win32;
using Ocyfrovka.App.Services;
using Ocyfrovka.Core.Documents;
using Ocyfrovka.Core.Input;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Ocyfrovka.App;

public partial class MainWindow : Window
{
    private readonly DigitizationDocument _document = new();
    private bool _isImporting;

    public MainWindow()
    {
        InitializeComponent();
        RefreshPages();
    }

    private async void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        if (_isImporting)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "Документи|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff;*.pdf|Усі файли|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            await AddFilesAsync(dialog.FileNames);
        }
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        if (_isImporting)
        {
            return;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
        {
            await AddFilesAsync(paths);
        }
    }

    private async Task AddFilesAsync(IEnumerable<string> paths)
    {
        _isImporting = true;
        StatusText.Text = "Stage 2: імпорт файлів…";

        try
        {
            DocumentPage? lastAdded = null;
            var added = 0;
            var unsupported = 0;
            var errors = new List<string>();

            foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    IReadOnlyList<DocumentPage> pages = InputFileClassifier.GetKind(path) switch
                    {
                        InputFileKind.Image => ImagePageLoader.Load(path),
                        InputFileKind.Pdf => await PdfPageLoader.LoadAsync(path, AppContext.BaseDirectory),
                        _ => []
                    };

                    if (pages.Count == 0 && InputFileClassifier.GetKind(path) == InputFileKind.Unsupported)
                    {
                        unsupported++;
                        continue;
                    }

                    foreach (var page in pages)
                    {
                        if (_document.AddPage(page))
                        {
                            added++;
                            lastAdded = page;
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{Path.GetFileName(path)}: {ex.Message}");
                }
            }

            RefreshPages(lastAdded);

            var notes = new List<string>();
            if (added > 0)
            {
                notes.Add($"Додано сторінок: {added}.");
            }

            if (unsupported > 0)
            {
                notes.Add($"Непідтримуваних файлів: {unsupported}.");
            }

            if (errors.Count > 0)
            {
                notes.Add("Помилки читання:");
                notes.AddRange(errors.Take(5));
                if (errors.Count > 5)
                {
                    notes.Add($"...ще {errors.Count - 5}.");
                }
            }

            StatusText.Text = added > 0
                ? $"Stage 2: у документі {_document.Count} стор."
                : "Stage 2: нових сторінок не додано.";

            if (unsupported > 0 || errors.Count > 0)
            {
                MessageBox.Show(
                    this,
                    string.Join(Environment.NewLine, notes),
                    "ОЦИФРОВКА — імпорт",
                    MessageBoxButton.OK,
                    errors.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            }
        }
        finally
        {
            _isImporting = false;
        }
    }

    private void InputFilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => ShowSelectedPreview();

    private void MoveUp_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedPage() is not { } page)
        {
            return;
        }

        if (_document.MoveUp(page.Id))
        {
            RefreshPages(page);
        }
    }

    private void MoveDown_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedPage() is not { } page)
        {
            return;
        }

        if (_document.MoveDown(page.Id))
        {
            RefreshPages(page);
        }
    }

    private void RotateLeft_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedPage() is not { } page)
        {
            return;
        }

        page.RotateCounterClockwise();
        RefreshPages(page);
    }

    private void RotateRight_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedPage() is not { } page)
        {
            return;
        }

        page.RotateClockwise();
        RefreshPages(page);
    }

    private void RemovePage_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedPage() is not { } page)
        {
            return;
        }

        var oldIndex = _document.Pages
            .Select((value, index) => new { value, index })
            .First(x => x.value.Id == page.Id)
            .index;

        _document.Remove(page.Id);

        DocumentPage? next = null;
        if (_document.Count > 0)
        {
            next = _document.Pages[Math.Min(oldIndex, _document.Count - 1)];
        }

        RefreshPages(next);
        StatusText.Text = $"Stage 2: у документі {_document.Count} стор.";
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _document.Clear();
        RefreshPages();
        StatusText.Text = "Stage 2: документ очищено.";
    }

    private void Digitize_Click(object sender, RoutedEventArgs e)
    {
        if (_document.Count == 0)
        {
            MessageBox.Show(
                this,
                "Спочатку додайте фото, скан або PDF-документ.",
                "ОЦИФРОВКА",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        MessageBox.Show(
            this,
            $"Документ підготовлено: {_document.Count} стор. Порядок і повороти збережені в моделі без зміни оригіналів. OCR буде підключено після завершення Stage 2/3.",
            "ОЦИФРОВКА — Stage 2",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private DocumentPage? GetSelectedPage() => InputFilesList.SelectedItem as DocumentPage;

    private void RefreshPages(DocumentPage? preferredSelection = null)
    {
        var selectedId = preferredSelection?.Id ?? GetSelectedPage()?.Id;

        InputFilesList.ItemsSource = null;
        InputFilesList.ItemsSource = _document.Pages.ToArray();
        PageCountText.Text = $"Сторінок: {_document.Count}";

        if (selectedId is not null)
        {
            InputFilesList.SelectedItem = _document.Pages.FirstOrDefault(page => page.Id == selectedId);
        }
        else if (_document.Count > 0)
        {
            InputFilesList.SelectedIndex = 0;
        }
        else
        {
            PreviewImage.Source = null;
            PreviewHintText.Visibility = Visibility.Visible;
            PreviewInfoText.Text = "Оригінал не змінюється.";
        }

        InputFilesList.Items.Refresh();
        ShowSelectedPreview();
    }

    private void ShowSelectedPreview()
    {
        if (GetSelectedPage() is not { } page)
        {
            PreviewImage.Source = null;
            PreviewHintText.Visibility = Visibility.Visible;
            PreviewInfoText.Text = "Оригінал не змінюється.";
            return;
        }

        try
        {
            PreviewImage.Source = ImagePreviewLoader.Load(page);
            PreviewHintText.Visibility = Visibility.Collapsed;
            PreviewInfoText.Text =
                $"{page.PixelWidth} × {page.PixelHeight} px · поворот {page.RotationDegrees}° · оригінал не змінено";
        }
        catch (Exception ex)
        {
            PreviewImage.Source = null;
            PreviewHintText.Visibility = Visibility.Visible;
            PreviewHintText.Text = "Не вдалося показати сторінку.";
            PreviewInfoText.Text = ex.Message;
        }
    }
}
