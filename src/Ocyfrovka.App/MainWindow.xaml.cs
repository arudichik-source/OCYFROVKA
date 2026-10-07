using Microsoft.Win32;
using Ocyfrovka.App.Services;
using Ocyfrovka.App.Review;
using Ocyfrovka.Core.Documents;
using Ocyfrovka.Core.Input;
using Ocyfrovka.Core.Ocr;
using Ocyfrovka.Imaging;
using Ocyfrovka.Layout;
using Ocyfrovka.Review;
using Ocyfrovka.Ocr.Tesseract;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Data;
using System.Windows.Media;

namespace Ocyfrovka.App;

public partial class MainWindow : Window
{
    private readonly DigitizationDocument _document = new();
    private readonly Dictionary<Guid, ProcessedPreview> _processedPreviews = [];
    private readonly Dictionary<Guid, ReviewTableSession> _reviewSessions = [];
    private readonly Dictionary<Guid, string> _ocrTextByPage = [];
    private ReviewGridCellViewModel? _selectedReviewCell;

    private bool _isImporting;
    private bool _isProcessing;
    private bool _isOcrRunning;
    private bool _showProcessed;

    public MainWindow()
    {
        InitializeComponent();
        RefreshPages();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshOcrLanguages();
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
        StatusText.Text = "Stage 6: імпорт файлів…";

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

            _showProcessed = false;
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
                ? $"Stage 6: у документі {_document.Count} стор."
                : "Stage 6: нових сторінок не додано.";

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
    {
        ClearOcrOutput();
        ShowSelectedPreview();

        if (GetSelectedPage() is not { } page)
        {
            return;
        }

        if (_ocrTextByPage.TryGetValue(page.Id, out var text))
        {
            OcrTextBox.Text = text;
        }

        if (_reviewSessions.TryGetValue(page.Id, out var session))
        {
            ShowReviewSession(session);
        }
    }

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
        InvalidateProcessed(page);
        ClearOcrOutput();
        RefreshPages(page);
    }

    private void RotateRight_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedPage() is not { } page)
        {
            return;
        }

        page.RotateClockwise();
        InvalidateProcessed(page);
        ClearOcrOutput();
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
        _processedPreviews.Remove(page.Id);
        _reviewSessions.Remove(page.Id);
        _ocrTextByPage.Remove(page.Id);

        DocumentPage? next = null;
        if (_document.Count > 0)
        {
            next = _document.Pages[Math.Min(oldIndex, _document.Count - 1)];
        }

        _showProcessed = false;
        ClearOcrOutput();
        RefreshPages(next);
        StatusText.Text = $"Stage 6: у документі {_document.Count} стор.";
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _document.Clear();
        _processedPreviews.Clear();
        _reviewSessions.Clear();
        _ocrTextByPage.Clear();
        _showProcessed = false;
        ClearOcrOutput();
        RefreshPages();
        StatusText.Text = "Stage 6: документ очищено.";
    }

    private void ShowOriginal_Click(object sender, RoutedEventArgs e)
    {
        _showProcessed = false;
        ShowSelectedPreview();
    }

    private void ShowProcessed_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedPage() is not { } page)
        {
            return;
        }

        if (!_processedPreviews.ContainsKey(page.Id))
        {
            _showProcessed = false;
            StatusText.Text = "Stage 6: спочатку натисніть «Обробити» для цієї сторінки.";
            ShowSelectedPreview();
            return;
        }

        _showProcessed = true;
        ShowSelectedPreview();
    }

    private void ApplyCrop_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedPage() is not { } page ||
            !_processedPreviews.TryGetValue(page.Id, out var processed))
        {
            StatusText.Text = "Stage 6: спочатку обробіть сторінку.";
            return;
        }

        var cropped = ImageProcessingService.ApplyDetectedCrop(processed);
        _processedPreviews[page.Id] = cropped;
        _showProcessed = true;
        ClearOcrOutput();
        ShowSelectedPreview();

        StatusText.Text = cropped.CropApplied
            ? $"Stage 6: preview обрізано до {cropped.Image.PixelWidth}×{cropped.Image.PixelHeight}."
            : "Stage 6: межі збігаються з повним кадром.";
    }

    private void ResetCrop_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedPage() is not { } page ||
            !_processedPreviews.TryGetValue(page.Id, out var processed))
        {
            return;
        }

        _processedPreviews[page.Id] =
            ImageProcessingService.ResetDetectedCrop(processed);

        _showProcessed = true;
        ClearOcrOutput();
        ShowSelectedPreview();
        StatusText.Text = "Stage 6: показано повний оброблений кадр.";
    }

    private async void ProcessSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_isProcessing || GetSelectedPage() is not { } page)
        {
            return;
        }

        _isProcessing = true;
        StatusText.Text = "Stage 6: обробка сторінки…";

        try
        {
            var original = ImagePreviewLoader.Load(page);
            var profile = GetSelectedProfile();

            var processed = await Task.Run(
                () => ImageProcessingService.Process(original, profile));

            _processedPreviews[page.Id] = processed;
            _showProcessed = true;
            ClearOcrOutput();
            ShowSelectedPreview();

            StatusText.Text =
                $"Stage 6: оброблено · якість {processed.Quality.OverallScore:0.#}/100";
        }
        catch (Exception ex)
        {
            _showProcessed = false;
            ShowSelectedPreview();

            MessageBox.Show(
                this,
                ex.Message,
                "ОЦИФРОВКА — preprocessing",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            StatusText.Text = "Stage 6: помилка обробки сторінки.";
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async void Digitize_Click(object sender, RoutedEventArgs e)
    {
        if (_isOcrRunning || GetSelectedPage() is not { } page)
        {
            if (_document.Count == 0)
            {
                MessageBox.Show(
                    this,
                    "Спочатку додайте фото, скан або PDF-документ.",
                    "ОЦИФРОВКА",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            return;
        }

        if (!TesseractRuntimeLocator.IsRuntimeAvailable(AppContext.BaseDirectory))
        {
            MessageBox.Show(
                this,
                "Локальний Tesseract runtime відсутній у portable-збірці. Перевірте папку Engine\\OCR.",
                "ОЦИФРОВКА — OCR",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var language = OcrLanguageComboBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(language))
        {
            language = "ukr";
        }

        _isOcrRunning = true;
        DigitizeButton.IsEnabled = false;
        StatusText.Text = $"Stage 6: OCR {language}…";

        try
        {
            BitmapSource inputBitmap =
                _showProcessed && _processedPreviews.TryGetValue(page.Id, out var processed)
                    ? processed.Image
                    : ImagePreviewLoader.Load(page);

            var inputPath = await OcrInputMaterializer.SavePngAsync(
                inputBitmap,
                page.Id,
                AppContext.BaseDirectory);

            var engine = new TesseractOcrEngine(AppContext.BaseDirectory);
            var result = await engine.RecognizeAsync(
                new OcrRequest(
                    FilePath: inputPath,
                    Language: language,
                    Profile: "auto"));

            _ocrTextByPage[page.Id] = result.Text;
            OcrTextBox.Text = result.Text;
            ShowStructuredLayout(page.Id, result.Words);

            StatusText.Text =
                $"Stage 6: OCR + структура завершені · confidence {result.Confidence:0.#}% · слів {result.Words.Count}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "ОЦИФРОВКА — OCR",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            StatusText.Text = "Stage 6: OCR завершився помилкою.";
        }
        finally
        {
            _isOcrRunning = false;
            DigitizeButton.IsEnabled = true;
        }
    }

    private void ClearOcrOutput()
    {
        OcrTextBox?.Clear();
        _selectedReviewCell = null;

        if (StructuredTableGrid is not null)
        {
            StructuredTableGrid.ItemsSource = null;
            StructuredTableGrid.Columns.Clear();
        }

        if (LayoutStatusText is not null)
        {
            LayoutStatusText.Text = "Структура ще не аналізувалась.";
        }
    }

    private void ShowStructuredLayout(
        Guid pageId,
        IReadOnlyList<OcrWord> words)
    {
        var layout = TableLayoutAnalyzer.Analyze(words);

        if (!layout.IsLikelyTable || layout.Rows.Count == 0 || layout.ColumnCount < 2)
        {
            StructuredTableGrid.ItemsSource = null;
            StructuredTableGrid.Columns.Clear();
            _selectedReviewCell = null;
            LayoutStatusText.Text =
                $"Таблицю не підтверджено · рядків тексту {layout.Lines.Count} · кандидатних колонок {layout.ColumnCount}.";
            return;
        }

        if (_reviewSessions.TryGetValue(pageId, out var session))
        {
            session.Merge(layout);
        }
        else
        {
            session = new ReviewTableSession(layout);
            _reviewSessions[pageId] = session;
        }

        ShowReviewSession(session);
    }

    private void ShowReviewSession(ReviewTableSession session)
    {
        var projection = ReviewGridProjection.Create(session);

        StructuredTableGrid.ItemsSource = null;
        StructuredTableGrid.Columns.Clear();
        _selectedReviewCell = null;

        for (var column = 0; column < projection.ColumnCount; column++)
        {
            StructuredTableGrid.Columns.Add(
                CreateReviewColumn(
                    projection.Headers[column],
                    column));
        }

        StructuredTableGrid.ItemsSource = projection.Rows;

        LayoutStatusText.Text =
            $"Перевірка: {projection.Rows.Count} ряд. × {projection.ColumnCount} кол. · " +
            $"підтверджено {projection.ConfirmedCount} · " +
            $"перевірено {projection.ReviewedCount} · " +
            $"низька confidence {projection.LowConfidenceCount}.";
    }

    private static DataGridTextColumn CreateReviewColumn(
        string header,
        int columnIndex)
    {
        var textPath = $"Cells[{columnIndex}].Text";
        var confidencePath = $"Cells[{columnIndex}].Confidence";
        var lowConfidencePath = $"Cells[{columnIndex}].IsLowConfidence";
        var confirmedPath = $"Cells[{columnIndex}].IsConfirmed";
        var errorPath = $"Cells[{columnIndex}].IsError";
        var tooltipPath = $"Cells[{columnIndex}].Tooltip";

        var elementStyle = new Style(typeof(TextBlock));
        elementStyle.Setters.Add(new Setter(
            TextBlock.PaddingProperty,
            new Thickness(6, 3, 6, 3)));
        elementStyle.Setters.Add(new Setter(
            FrameworkElement.ToolTipProperty,
            new Binding(tooltipPath)));

        var lowTrigger = new DataTrigger
        {
            Binding = new Binding(lowConfidencePath),
            Value = true
        };
        lowTrigger.Setters.Add(new Setter(
            TextBlock.BackgroundProperty,
            new SolidColorBrush(Color.FromRgb(92, 72, 18))));
        elementStyle.Triggers.Add(lowTrigger);

        var confirmedTrigger = new DataTrigger
        {
            Binding = new Binding(confirmedPath),
            Value = true
        };
        confirmedTrigger.Setters.Add(new Setter(
            TextBlock.BackgroundProperty,
            new SolidColorBrush(Color.FromRgb(24, 82, 54))));
        elementStyle.Triggers.Add(confirmedTrigger);

        var errorTrigger = new DataTrigger
        {
            Binding = new Binding(errorPath),
            Value = true
        };
        errorTrigger.Setters.Add(new Setter(
            TextBlock.BackgroundProperty,
            new SolidColorBrush(Color.FromRgb(105, 37, 37))));
        elementStyle.Triggers.Add(errorTrigger);

        var editingStyle = new Style(typeof(TextBox));
        editingStyle.Setters.Add(new Setter(
            TextBox.PaddingProperty,
            new Thickness(5, 2, 5, 2)));
        editingStyle.Setters.Add(new Setter(
            FrameworkElement.ToolTipProperty,
            new Binding(tooltipPath)));

        return new DataGridTextColumn
        {
            Header = header,
            Binding = new Binding(textPath)
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
            },
            ElementStyle = elementStyle,
            EditingElementStyle = editingStyle,
            MinWidth = 80
        };
    }

    private void StructuredTableGrid_SelectedCellsChanged(
        object sender,
        SelectedCellsChangedEventArgs e)
    {
        _selectedReviewCell = null;

        if (StructuredTableGrid.CurrentItem is not ReviewGridRowViewModel row ||
            StructuredTableGrid.CurrentColumn is null)
        {
            return;
        }

        var column = StructuredTableGrid.CurrentColumn.DisplayIndex;
        if ((uint)column >= (uint)row.Cells.Length)
        {
            return;
        }

        _selectedReviewCell = row.Cells[column];

        if (_selectedReviewCell is not null)
        {
            StatusText.Text =
                $"Stage 6: клітинка R{_selectedReviewCell.RowIndex + 1}C{_selectedReviewCell.ColumnIndex + 1} · " +
                $"{_selectedReviewCell.StateText} · confidence {_selectedReviewCell.Confidence:0.#}%";
        }
    }

    private void ConfirmReviewCell_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedReviewCell is null)
        {
            StatusText.Text = "Stage 6: виберіть клітинку для підтвердження.";
            return;
        }

        _selectedReviewCell.Confirm();
        StructuredTableGrid.Items.Refresh();
        RefreshReviewStatus();
    }

    private void MarkReviewCellError_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedReviewCell is null)
        {
            StatusText.Text = "Stage 6: виберіть проблемну клітинку.";
            return;
        }

        _selectedReviewCell.MarkError();
        StructuredTableGrid.Items.Refresh();
        RefreshReviewStatus();
    }

    private void ResetReviewCell_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedReviewCell is null)
        {
            StatusText.Text = "Stage 6: виберіть клітинку.";
            return;
        }

        _selectedReviewCell.ResetToRecognition();
        StructuredTableGrid.Items.Refresh();
        RefreshReviewStatus();
    }

    private void RefreshReviewStatus()
    {
        if (GetSelectedPage() is not { } page ||
            !_reviewSessions.TryGetValue(page.Id, out var session))
        {
            return;
        }

        var projection = ReviewGridProjection.Create(session);

        LayoutStatusText.Text =
            $"Перевірка: {projection.Rows.Count} ряд. × {projection.ColumnCount} кол. · " +
            $"підтверджено {projection.ConfirmedCount} · " +
            $"перевірено {projection.ReviewedCount} · " +
            $"низька confidence {projection.LowConfidenceCount}.";

        if (_selectedReviewCell is not null)
        {
            StatusText.Text =
                $"Stage 6: {_selectedReviewCell.StateText} · confidence {_selectedReviewCell.Confidence:0.#}%";
        }
    }

    private DocumentPage? GetSelectedPage() => InputFilesList.SelectedItem as DocumentPage;

    private PreprocessingProfile GetSelectedProfile() => ProfileComboBox.SelectedIndex switch
    {
        1 => PreprocessingProfile.Grayscale,
        2 => PreprocessingProfile.HighContrast,
        3 => PreprocessingProfile.Binary,
        4 => PreprocessingProfile.AdaptiveBinary,
        5 => PreprocessingProfile.ShadowCorrected,
        6 => PreprocessingProfile.Sharpened,
        _ => PreprocessingProfile.Auto
    };

    private void InvalidateProcessed(DocumentPage page)
    {
        _processedPreviews.Remove(page.Id);
        _showProcessed = false;
    }

    private void RefreshOcrLanguages()
    {
        var tessdata = TesseractRuntimeLocator.GetTessdataPath(AppContext.BaseDirectory);
        var installed = TesseractLanguageCatalog
            .GetInstalledLanguages(tessdata)
            .Select(language => language.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var options = new List<string>();

        foreach (var preferred in new[] { "ukr", "eng", "rus", "pol", "deu" })
        {
            if (installed.Contains(preferred))
            {
                options.Add(preferred);
            }
        }

        options.AddRange(
            installed
                .Where(code => !options.Contains(code, StringComparer.OrdinalIgnoreCase))
                .OrderBy(code => code, StringComparer.OrdinalIgnoreCase));

        AddCombination(options, installed, "ukr", "eng");
        AddCombination(options, installed, "ukr", "rus");
        AddCombination(options, installed, "ukr", "eng", "rus");

        OcrLanguageComboBox.ItemsSource = options;

        if (installed.Contains("ukr") && installed.Contains("eng"))
        {
            OcrLanguageComboBox.Text = "ukr+eng";
        }
        else if (installed.Contains("ukr"))
        {
            OcrLanguageComboBox.Text = "ukr";
        }
        else if (options.Count > 0)
        {
            OcrLanguageComboBox.Text = options[0];
        }
        else
        {
            OcrLanguageComboBox.Text = "ukr";
        }

        if (TesseractRuntimeLocator.IsRuntimeAvailable(AppContext.BaseDirectory))
        {
            StatusText.Text =
                $"Stage 6: OCR runtime готовий · мов {installed.Count}";
        }
        else
        {
            StatusText.Text =
                "Stage 6: OCR runtime буде доступний у portable-збірці.";
        }
    }

    private static void AddCombination(
        ICollection<string> options,
        ISet<string> installed,
        params string[] codes)
    {
        if (codes.All(installed.Contains))
        {
            options.Add(string.Join('+', codes));
        }
    }

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
            PreviewModeText.Text = "Оригінал";
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
            PreviewModeText.Text = "Оригінал";
            PreviewInfoText.Text = "Оригінал не змінюється.";
            return;
        }

        try
        {
            if (_showProcessed && _processedPreviews.TryGetValue(page.Id, out var processed))
            {
                PreviewImage.Source = processed.Image;
                PreviewHintText.Visibility = Visibility.Collapsed;
                PreviewModeText.Text = "Оброблено";

                var warnings = processed.Quality.Warnings.Count == 0
                    ? "попереджень немає"
                    : string.Join(" ", processed.Quality.Warnings);

                PreviewInfoText.Text =
                    $"Профіль: {GetProfileName(processed.Profile)} · " +
                    $"якість {processed.Quality.OverallScore:0.#}/100 · " +
                    $"яскравість {processed.Quality.Brightness:0.#} · " +
                    $"контраст {processed.Quality.Contrast:0.#} · " +
                    $"деталі {processed.Quality.EdgeScore:0.#} · " +
                    $"deskew {processed.DeskewAngle:+0.##;-0.##;0}° · " +
                    $"межі {processed.ContentBounds.Width}×{processed.ContentBounds.Height} · " +
                    $"crop {(processed.CropApplied ? "увімкнено" : "вимкнено")} · " +
                    $"Otsu {processed.OtsuThreshold}. {warnings}";
                return;
            }

            PreviewImage.Source = ImagePreviewLoader.Load(page);
            PreviewHintText.Visibility = Visibility.Collapsed;
            PreviewModeText.Text = "Оригінал";
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

    private static string GetProfileName(PreprocessingProfile profile) => profile switch
    {
        PreprocessingProfile.Grayscale => "Відтінки сірого",
        PreprocessingProfile.HighContrast => "Високий контраст",
        PreprocessingProfile.Binary => "Ч/Б (Otsu)",
        PreprocessingProfile.AdaptiveBinary => "Адаптивний Ч/Б",
        PreprocessingProfile.ShadowCorrected => "Корекція освітлення",
        PreprocessingProfile.Sharpened => "Різкість",
        _ => "Авто"
    };
}
