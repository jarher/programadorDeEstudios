using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace StudyTracker.UI.Views;

public partial class SessionJournalDialog : FluentWindow
{
    public SessionJournalDialog(string? initialNotes = null)
    {
        InitializeComponent();
        NotesTextBox.Text = initialNotes ?? string.Empty;
    }

    public string Notes => NotesTextBox.Text;

    public bool ShouldSave { get; private set; }

    private void SessionJournalDialog_Loaded(object sender, RoutedEventArgs e)
    {
        NotesTextBox.Focus();
        Keyboard.Focus(NotesTextBox);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ShouldSave = true;
        DialogResult = true;
    }

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        ShouldSave = false;
        DialogResult = false;
    }
}
