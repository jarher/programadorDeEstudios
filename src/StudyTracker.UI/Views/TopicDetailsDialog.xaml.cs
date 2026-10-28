using System.Windows;

namespace StudyTracker.UI.Views;

public partial class TopicDetailsDialog : Wpf.Ui.Controls.FluentWindow
{
    public TopicDetailsDialog()
    {
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
