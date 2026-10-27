using System.Windows;
using System.Windows.Automation;
using System.ComponentModel;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;
using StudyTracker.UI.ViewModels;
using StudyTracker.UI.Views;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace StudyTracker.UI;

public partial class MainWindow : FluentWindow
{
    private readonly Forms.NotifyIcon _trayIcon;
    private readonly System.Drawing.Icon _appIcon;
    private FloatingTimerWindow? _floatingTimer;
    private bool _exitRequested;

    public MainWindow(MainViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;
        UpdateThemeButton(ApplicationThemeManager.GetAppTheme());

        _appIcon = LoadApplicationIcon();
        var imageSource = Imaging.CreateBitmapSourceFromHIcon(
            _appIcon.Handle,
            System.Windows.Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());
        Icon = imageSource;
        _trayIcon = CreateTrayIcon();
        viewModel.Timer.JournalRequested += Timer_JournalRequested;
        TimerControl.FloatingModeRequested += TimerControl_FloatingModeRequested;
        StateChanged += MainWindow_StateChanged;
    }

    public MainWindow(DashboardViewModel dashboardViewModel)
        : this(new MainViewModel(
            dashboardViewModel,
            CreateTimerViewModel(),
            CreateStatsViewModel()))
    {
    }

    private static TimerViewModel CreateTimerViewModel()
    {
        var database = new StudyTracker.Core.Data.DatabaseInitializer();

        return new TimerViewModel(
            new StudyTracker.Core.Services.TimerService(),
            new StudyTracker.Core.Data.TopicRepository(database),
            new StudyTracker.Core.Data.SessionRepository(database));
    }

    private static StatsViewModel CreateStatsViewModel()
    {
        var database = new StudyTracker.Core.Data.DatabaseInitializer();
        var sessionRepository = new StudyTracker.Core.Data.SessionRepository(database);

        return new StatsViewModel(
            new StudyTracker.Core.Services.StatsService(sessionRepository),
            new StudyTracker.Core.Data.TopicRepository(database));
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_exitRequested)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        _floatingTimer?.Close();
        _trayIcon.Visible = false;
        _trayIcon.ContextMenuStrip?.Dispose();
        _trayIcon.Dispose();
        _appIcon.Dispose();
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.Timer.JournalRequested -= Timer_JournalRequested;
            viewModel.Timer.Dispose();
        }

        base.OnClosing(e);
    }

    private Forms.NotifyIcon CreateTrayIcon()
    {
        var icon = new Forms.NotifyIcon
        {
            Icon = _appIcon,
            Text = "StudyProgrammer — temporizador de estudio",
            Visible = true
        };
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Mostrar StudyProgrammer", null, (_, _) => RestoreMainWindow());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Salir", null, (_, _) => ExitApplication());
        icon.ContextMenuStrip = menu;
        icon.DoubleClick += (_, _) => RestoreMainWindow();

        return icon;
    }

    private static System.Drawing.Icon LoadApplicationIcon()
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new InvalidOperationException("No se pudo determinar la ruta del ejecutable.");
        }

        return System.Drawing.Icon.ExtractAssociatedIcon(executablePath)
            ?? throw new InvalidOperationException("El ejecutable no contiene el icono de StudyProgrammer.");
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState != WindowState.Minimized)
        {
            return;
        }

        Hide();
        WindowState = WindowState.Normal;
    }

    private void TimerControl_FloatingModeRequested(object? sender, EventArgs e)
    {
        if (DataContext is not MainViewModel viewModel || _floatingTimer is not null)
        {
            return;
        }

        _floatingTimer = new FloatingTimerWindow(viewModel.Timer);
        _floatingTimer.ReturnToMainRequested += FloatingTimer_ReturnToMainRequested;
        _floatingTimer.Closed += FloatingTimer_Closed;
        Hide();
        _floatingTimer.Show();
    }

    private void Timer_JournalRequested(object? sender, EventArgs e)
    {
        var floatingTimer = _floatingTimer;
        _floatingTimer = null;
        floatingTimer?.Close();
        RestoreMainWindow();
    }

    private void FloatingTimer_ReturnToMainRequested(object? sender, EventArgs e)
    {
        _floatingTimer?.Close();
        _floatingTimer = null;
        RestoreMainWindow();
    }

    private void FloatingTimer_Closed(object? sender, EventArgs e)
    {
        if (_exitRequested)
        {
            return;
        }

        _floatingTimer = null;
        RestoreMainWindow();
    }

    private void RestoreMainWindow()
    {
        var floatingTimer = _floatingTimer;
        _floatingTimer = null;
        if (floatingTimer is not null)
        {
            floatingTimer.ReturnToMainRequested -= FloatingTimer_ReturnToMainRequested;
            floatingTimer.Closed -= FloatingTimer_Closed;
            floatingTimer.Close();
        }

        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        _exitRequested = true;
        Close();
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
