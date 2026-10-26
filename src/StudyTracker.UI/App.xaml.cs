using System.Diagnostics;
using System.Windows;
using StudyTracker.Core.Data;
using StudyTracker.UI.ViewModels;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace StudyTracker.UI;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DashboardViewModel? viewModel = null;
        MainWindow? window = null;

        try
        {
            ApplyInitialTheme();

            var database = new DatabaseInitializer();
            var repository = new TopicRepository(database);
            viewModel = new DashboardViewModel(repository);

            var databaseReady = false;

            try
            {
                await database.InitializeAsync();
                databaseReady = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"No se pudo inicializar la base de datos: {ex}");
                viewModel.ErrorMessage =
                    "No se pudo preparar la base de datos. Revisa los permisos de %LOCALAPPDATA%.";
            }

            window = new MainWindow(viewModel);
            MainWindow = window;
            window.Show();

            if (!databaseReady)
            {
                return;
            }

            try
            {
                await viewModel.LoadAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"No se pudieron cargar las temáticas: {ex}");
                viewModel.ErrorMessage = "No se pudieron cargar las temáticas. Inténtalo de nuevo.";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"No se pudo iniciar el panel de estudio: {ex}");

            if (viewModel is not null)
            {
                viewModel.ErrorMessage = "No se pudo iniciar el panel de estudio. Inténtalo de nuevo.";
            }

            if (window is not null && !window.IsVisible)
            {
                window.Show();
            }
        }
    }

    private static void ApplyInitialTheme()
    {
        var systemTheme = ApplicationThemeManager.GetSystemTheme();
        var initialTheme = systemTheme is SystemTheme.Light or SystemTheme.Sunrise or SystemTheme.Flow
            ? ApplicationTheme.Light
            : ApplicationTheme.Dark;

        ApplicationThemeManager.Apply(initialTheme, WindowBackdropType.Mica, true);
    }
}
