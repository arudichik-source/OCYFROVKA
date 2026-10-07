using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace Ocyfrovka.App;

public partial class MainWindow : Window
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff", ".pdf"
    };

    private readonly ObservableCollection<string> _files = [];

    public MainWindow()
    {
        InitializeComponent();
        InputFilesList.ItemsSource = _files;
    }

    private void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "Документи|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff;*.pdf|Усі файли|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            AddFiles(dialog.FileNames);
        }
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
        {
            AddFiles(paths);
        }
    }

    private void AddFiles(IEnumerable<string> paths)
    {
        foreach (var path in paths.Where(File.Exists))
        {
            if (!SupportedExtensions.Contains(Path.GetExtension(path))) continue;
            if (!_files.Any(x => string.Equals(x, path, StringComparison.OrdinalIgnoreCase))) _files.Add(path);
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e) => _files.Clear();

    private void Digitize_Click(object sender, RoutedEventArgs e)
    {
        if (_files.Count == 0)
        {
            MessageBox.Show(this, "Спочатку додайте фото або PDF.", "ОЦИФРОВКА", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        MessageBox.Show(this,
            "Імпорт уже працює. Локальний OCR буде підключено після preprocessing та моделі сторінок.",
            "ОЦИФРОВКА — Stage 1",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}