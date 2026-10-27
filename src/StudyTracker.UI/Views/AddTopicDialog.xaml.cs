using System.Windows;
using System.Windows.Input;
using StudyTracker.UI.ViewModels;
using Wpf.Ui.Controls;

namespace StudyTracker.UI.Views;

public partial class AddTopicDialog : FluentWindow
{
    public AddTopicDialog()
    {
        InitializeComponent();
        DataContext = new AddTopicDialogViewModel();
    }

    public NewTopicRequest? Request { get; private set; }

    private void AddTopicDialog_Loaded(object sender, RoutedEventArgs e)
    {
        TitleTextBox.Focus();
        Keyboard.Focus(TitleTextBox);
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not AddTopicDialogViewModel viewModel)
        {
            return;
        }

        var request = viewModel.TryBuildRequest();

        if (request is null)
        {
            return;
        }

        Request = request;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
