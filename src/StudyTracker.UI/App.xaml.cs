using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace StudyTracker.UI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var systemTheme = ApplicationThemeManager.GetSystemTheme();
        var initialTheme = systemTheme is SystemTheme.Light or SystemTheme.Sunrise or SystemTheme.Flow
            ? ApplicationTheme.Light
            : ApplicationTheme.Dark;

        ApplicationThemeManager.Apply(initialTheme, WindowBackdropType.Mica, true);

        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }
}
