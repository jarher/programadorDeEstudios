using System.Windows;
using StudyTracker.UI.ViewModels;

namespace StudyTracker.UI.Views;

public partial class TimerView : System.Windows.Controls.UserControl
{
    private bool _isShowingJournal;

    public event EventHandler? FloatingModeRequested;

    public TimerView()
    {
        InitializeComponent();
        Loaded += TimerView_Loaded;
        Unloaded += TimerView_Unloaded;
    }

    private async void TimerView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not TimerViewModel viewModel)
        {
            return;
        }

        viewModel.JournalRequested -= ViewModel_JournalRequested;
        viewModel.JournalRequested += ViewModel_JournalRequested;

        if (viewModel.IsAwaitingJournal)
        {
            await ShowJournalDialog(viewModel);
        }
    }

    private void TimerView_Unloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is TimerViewModel viewModel)
        {
            viewModel.JournalRequested -= ViewModel_JournalRequested;
        }
    }

    private async void ViewModel_JournalRequested(object? sender, EventArgs e)
    {
        if (sender is TimerViewModel viewModel)
        {
            await ShowJournalDialog(viewModel);
        }
    }

    private async Task ShowJournalDialog(TimerViewModel viewModel)
    {
        if (_isShowingJournal || !viewModel.IsAwaitingJournal)
        {
            return;
        }

        _isShowingJournal = true;

        try
        {
            var dialog = new SessionJournalDialog(viewModel.PendingJournalNotes)
            {
                Owner = Window.GetWindow(this)
            };

            var result = dialog.ShowDialog();
            var notes = result == true && dialog.ShouldSave
                ? dialog.Notes
                : null;

            await viewModel.SavePendingSessionAsync(notes);
        }
        finally
        {
            _isShowingJournal = false;
        }
    }

    private async void RetryJournal_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is TimerViewModel viewModel)
        {
            await ShowJournalDialog(viewModel);
        }
    }

    private void FloatingMode_Click(object sender, RoutedEventArgs e)
    {
        FloatingModeRequested?.Invoke(this, EventArgs.Empty);
    }
}
