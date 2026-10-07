using System.Windows;
using Ocyfrovka.App.Services;

namespace Ocyfrovka.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            WorkspaceInitializer.EnsurePortableLayout();
            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Не вдалося підготувати робочі папки ОЦИФРОВКИ.\n\n{ex.Message}\n\nРозпакуйте програму в папку, доступну для запису.",
                "ОЦИФРОВКА — помилка запуску",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}