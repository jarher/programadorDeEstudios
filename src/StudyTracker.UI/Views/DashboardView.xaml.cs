using System.Windows;
using System.Windows.Controls;
using StudyTracker.UI.ViewModels;

namespace StudyTracker.UI.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    private async void AddTopic_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not DashboardViewModel viewModel)
        {
            return;
        }

        try
        {
            var dialog = new AddTopicDialog();
            var owner = Window.GetWindow(this) ?? Application.Current?.MainWindow;

            if (owner is not null)
            {
                dialog.Owner = owner;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            if (dialog.ShowDialog() == true && dialog.Request is { } request)
            {
                await viewModel.AddTopicAsync(request);
            }
        }
        catch (Exception)
        {
            viewModel.ErrorMessage = "No se pudo abrir el diálogo para crear la temática. Inténtalo de nuevo.";
        }
    }
}
