using Microsoft.Data.Sqlite;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using StudyTracker.Core.Data;
using StudyTracker.Core.Models;
using StudyTracker.UI.ViewModels;
using StudyTracker.UI.Views;
using Wpf.Ui.Controls;

namespace StudyTracker.UI.Tests;

public sealed class DashboardTopicDetailsTests
{
    [Fact]
    public async Task SelectingTopic_LoadsPersistedSessionHistoryAndNotes()
    {
        await using var database = await DashboardTestDatabase.CreateAsync();
        var topic = new Topic
        {
            Title = "Álgebra",
            Description = "Ecuaciones y funciones",
            TargetHours = 4,
            CreatedAt = new DateTime(2026, 10, 1)
        };
        await database.Topics.CreateAsync(topic);

        await database.Sessions.RecordSessionAsync(new StudySession
        {
            TopicId = topic.Id,
            StartedAt = new DateTime(2026, 10, 27, 16, 0, 0),
            EndedAt = new DateTime(2026, 10, 27, 16, 45, 0),
            PlannedMinutes = 50,
            ActualMinutes = 45,
            SessionType = StudySession.TypePomodoro,
            Notes = "Repasar ecuaciones cuadráticas"
        });

        var viewModel = new DashboardViewModel(database.Topics, database.Sessions);
        await viewModel.LoadAsync();
        var listedTopic = Assert.Single(viewModel.Topics);

        await viewModel.SelectTopicAsync(listedTopic);

        var session = Assert.Single(viewModel.SelectedSessions);
        Assert.Same(listedTopic, viewModel.SelectedTopic);
        Assert.True(listedTopic.IsSelected);
        Assert.Equal("Ecuaciones y funciones", listedTopic.Description);
        Assert.Equal(45, listedTopic.TotalMinutesStudied);
        Assert.Equal("Repasar ecuaciones cuadráticas", session.Notes);
        Assert.Equal(StudySession.TypePomodoro, session.SessionType);
        Assert.False(viewModel.HasNoSelectedSessions);
        Assert.False(viewModel.HasSelectedTopicError);

        viewModel.ClearSelectedTopic();

        Assert.Null(viewModel.SelectedTopic);
        Assert.Empty(viewModel.SelectedSessions);
        Assert.False(listedTopic.IsSelected);
        Assert.False(viewModel.HasSelectedTopic);
    }

    [Fact]
    public async Task SelectingTopicWithoutSessions_ShowsEmptyHistoryState()
    {
        await using var database = await DashboardTestDatabase.CreateAsync();
        await database.Topics.CreateAsync(new Topic { Title = "Sin sesiones" });

        var viewModel = new DashboardViewModel(database.Topics, database.Sessions);
        await viewModel.LoadAsync();

        await viewModel.SelectTopicAsync(Assert.Single(viewModel.Topics));

        Assert.Empty(viewModel.SelectedSessions);
        Assert.True(viewModel.HasNoSelectedSessions);
        Assert.Null(viewModel.SelectedTopicErrorMessage);
    }

    [Fact]
    public Task ClickingTopicInDashboard_OpensModalWithSessionNotes()
    {
        return RunOnStaAsync(async () =>
        {
            var application = Application.Current ?? new Application();
            application.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            await using var database = await DashboardTestDatabase.CreateAsync();
            var topic = new Topic { Title = "Biología" };
            await database.Topics.CreateAsync(topic);
            await database.Sessions.RecordSessionAsync(new StudySession
            {
                TopicId = topic.Id,
                StartedAt = new DateTime(2026, 10, 28, 9, 0, 0),
                EndedAt = new DateTime(2026, 10, 28, 9, 30, 0),
                PlannedMinutes = 30,
                ActualMinutes = 30,
                Notes = "Repasar la célula"
            });

            var viewModel = new DashboardViewModel(database.Topics, database.Sessions);
            await viewModel.LoadAsync();
            var dashboard = new DashboardView { DataContext = viewModel };
            var window = new Window
            {
                Content = dashboard,
                Width = 1000,
                Height = 800,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            try
            {
                window.Show();
                dashboard.UpdateLayout();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

                var listedTopic = Assert.Single(viewModel.Topics);
                var completionCheckbox = FindVisualChild<System.Windows.Controls.CheckBox>(
                    dashboard,
                    checkbox => AutomationProperties.GetName(checkbox) == "Marcar Biología como completada");
                Assert.NotNull(completionCheckbox);
                Assert.NotSame(viewModel.SelectTopicCommand, completionCheckbox.Command);
                completionCheckbox.Command!.Execute(completionCheckbox.CommandParameter);

                while (viewModel.IsBusy)
                {
                    await Task.Delay(1);
                }

                Assert.True(listedTopic.IsCompleted);
                Assert.Null(viewModel.SelectedTopic);

                var topicButton = FindVisualChild<ButtonBase>(
                    dashboard,
                    button => AutomationProperties.GetName(button) == "Ver detalles de Biología");
                Assert.NotNull(topicButton);
                var topicList = Assert.IsType<System.Windows.Controls.ScrollViewer>(
                    dashboard.FindName("TopicListScrollViewer"));
                Assert.True(
                    topicList.ViewportWidth >= dashboard.ActualWidth * 0.9,
                    $"La lista ocupa {topicList.ViewportWidth:0} px de los {dashboard.ActualWidth:0} px disponibles.");
                Assert.True(
                    topicButton.ActualWidth >= topicList.ViewportWidth * 0.9,
                    $"La tarjeta ocupa {topicButton.ActualWidth:0} px, pero la lista dispone de {topicList.ViewportWidth:0} px.");
                var buttonPosition = TopicBounds(topicButton, dashboard);
                var checkboxPosition = TopicBounds(completionCheckbox, dashboard);
                Assert.True(buttonPosition.Contains(checkboxPosition),
                    "El checkbox debe quedar dentro de la tarjeta de la temática.");
                _ = dashboard.Dispatcher.BeginInvoke(
                    new Action(() => topicButton.RaiseEvent(
                        new RoutedEventArgs(ButtonBase.ClickEvent, topicButton))),
                    DispatcherPriority.Background);

                var dialog = await WaitForDialogAsync(application);
                dialog.UpdateLayout();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                Assert.True(dialog.IsVisible);
                Assert.Same(window, dialog.Owner);
                Assert.Same(viewModel, dialog.DataContext);
                Assert.Equal("Biología", viewModel.SelectedTopic?.Title);
                var session = Assert.Single(viewModel.SelectedSessions);
                Assert.Equal("Repasar la célula", session.Notes);
                Assert.True(viewModel.HasSelectedSessions);
                var sessionHistory = Assert.IsType<System.Windows.Controls.ItemsControl>(
                    dialog.FindName("SessionHistory"));
                Assert.Single(sessionHistory.Items);
                var sessionTemplate = Assert.IsAssignableFrom<FrameworkElement>(
                    sessionHistory.ItemTemplate.LoadContent());
                sessionTemplate.DataContext = session;
                sessionTemplate.Measure(new Size(500, 220));
                sessionTemplate.Arrange(new Rect(0, 0, 500, 220));
                await Dispatcher.Yield(DispatcherPriority.DataBind);
                var displayedText = FindVisualChildren<System.Windows.Controls.TextBlock>(sessionTemplate)
                    .Select(textBlock => textBlock.Text)
                    .ToList();
                Assert.Contains("Repasar la célula", displayedText);

                dialog.Close();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                Assert.Null(viewModel.SelectedTopic);
                Assert.Empty(viewModel.SelectedSessions);
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static async Task<TopicDetailsDialog> WaitForDialogAsync(Application application)
    {
        for (var attempt = 0; attempt < 1000; attempt++)
        {
            var dialog = application.Windows
                .OfType<TopicDetailsDialog>()
                .FirstOrDefault(window => window.IsVisible);
            if (dialog is not null)
            {
                return dialog;
            }

            await Task.Delay(1);
        }

        throw new TimeoutException("La ventana modal de detalle no se abrió.");
    }

    private static Rect TopicBounds(FrameworkElement element, FrameworkElement relativeTo)
    {
        var topLeft = element.TranslatePoint(new Point(0, 0), relativeTo);
        return new Rect(topLeft, element.RenderSize);
    }

    private static Task RunOnStaAsync(Func<Task> action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            dispatcher.BeginInvoke(new Action(async () =>
            {
                try
                {
                    await action();
                    completion.SetResult();
                }
                catch (Exception exception)
                {
                    completion.SetException(exception);
                }
                finally
                {
                    dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                }
            }));
            Dispatcher.Run();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }

    private static T? FindVisualChild<T>(DependencyObject parent, Func<T, bool> predicate)
        where T : DependencyObject =>
        FindVisualChildren<T>(parent).FirstOrDefault(predicate);

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
        where T : DependencyObject
    {
        var visited = new HashSet<DependencyObject>();
        return FindChildren<T>(parent, visited);
    }

    private static IEnumerable<T> FindChildren<T>(
        DependencyObject parent,
        ISet<DependencyObject> visited)
        where T : DependencyObject
    {
        if (!visited.Add(parent))
        {
            yield break;
        }

        if (parent is T matchingParent)
        {
            yield return matchingParent;
        }

        var children = new List<DependencyObject>();
        if (parent is Visual or System.Windows.Media.Media3D.Visual3D)
        {
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            {
                children.Add(VisualTreeHelper.GetChild(parent, index));
            }
        }

        foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
        {
            if (!children.Contains(child))
            {
                children.Add(child);
            }
        }

        foreach (var child in children)
        {
            foreach (var descendant in FindChildren<T>(child, visited))
            {
                yield return descendant;
            }
        }
    }

    private sealed class DashboardTestDatabase : IAsyncDisposable
    {
        private DashboardTestDatabase(string databasePath)
        {
            DatabasePath = databasePath;
            Initializer = new DatabaseInitializer(databasePath);
            Topics = new TopicRepository(Initializer);
            Sessions = new SessionRepository(Initializer);
        }

        private string DatabasePath { get; }

        public DatabaseInitializer Initializer { get; }

        public TopicRepository Topics { get; }

        public SessionRepository Sessions { get; }

        public static async Task<DashboardTestDatabase> CreateAsync()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "StudyTracker.UI.Tests",
                Guid.NewGuid().ToString("N"));
            var database = new DashboardTestDatabase(Path.Combine(directory, "study_tracker.db"));
            await database.Initializer.InitializeAsync();
            return database;
        }

        public ValueTask DisposeAsync()
        {
            SqliteConnection.ClearAllPools();

            var directory = Path.GetDirectoryName(DatabasePath);
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }

            return ValueTask.CompletedTask;
        }
    }
}
