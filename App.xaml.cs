using System.Windows;

namespace QuizApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnExit(ExitEventArgs e)
    {
        Services.LocalPronunciationService.Instance.StopWorker(shuttingDown: true);
        base.OnExit(e);
    }
}
