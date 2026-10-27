using System.Windows;
using StudyTracker.UI.ViewModels;

namespace StudyTracker.UI.Views;

public partial class StatsView : System.Windows.Controls.UserControl
{
    public StatsView()
    {
        InitializeComponent();
        Loaded += StatsView_Loaded;
    }

    private async void StatsView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is StatsViewModel viewModel)
        {
            await viewModel.LoadCommand.ExecuteAsync(null);
        }
    }
}
