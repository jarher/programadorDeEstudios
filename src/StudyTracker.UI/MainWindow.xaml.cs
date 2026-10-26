using System.Windows;
using System.Windows.Automation;
using StudyTracker.UI.ViewModels;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace StudyTracker.UI;

public partial class MainWindow : FluentWindow
{
    public MainWindow(DashboardViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;
        UpdateThemeButton(ApplicationThemeManager.GetAppTheme());
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        var currentTheme = ApplicationThemeManager.GetAppTheme();
        var nextTheme = currentTheme == ApplicationTheme.Dark
            ? ApplicationTheme.Light
            : ApplicationTheme.Dark;

        ApplicationThemeManager.Apply(nextTheme, WindowBackdropType.Mica, true);
        UpdateThemeButton(nextTheme);
    }

    private void UpdateThemeButton(ApplicationTheme theme)
    {
        var isDark = theme == ApplicationTheme.Dark;
        ThemeIcon.Symbol = isDark ? SymbolRegular.WeatherSunny24 : SymbolRegular.WeatherMoon24;
        ThemeButton.ToolTip = isDark ? "Cambiar a tema claro" : "Cambiar a tema oscuro";
        AutomationProperties.SetName(ThemeButton, ThemeButton.ToolTip.ToString());
    }
}
