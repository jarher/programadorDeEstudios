using System.Windows;
using System.Windows.Input;
using StudyTracker.UI.ViewModels;

namespace StudyTracker.UI.Views;

public partial class FloatingTimerWindow : Window
{
    public FloatingTimerWindow(TimerViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;
        PositionInWorkArea();
    }

    public event EventHandler? ReturnToMainRequested;

    private void PositionInWorkArea()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 24;
        Top = workArea.Bottom - Height - 24;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        var source = e.OriginalSource as DependencyObject;
        while (source is not null)
        {
            if (source is System.Windows.Controls.Button)
            {
                return;
            }

            source = System.Windows.Media.VisualTreeHelper.GetParent(source);
        }

        DragMove();
    }

    private void PauseResume_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not TimerViewModel viewModel)
        {
            return;
        }

        if (viewModel.IsRunning)
        {
            viewModel.PauseCommand.Execute(null);
        }
        else if (viewModel.IsPaused)
        {
            viewModel.ResumeCommand.Execute(null);
        }
    }

    private void ReturnToMain_Click(object sender, RoutedEventArgs e)
    {
        ReturnToMainRequested?.Invoke(this, EventArgs.Empty);
    }
}
