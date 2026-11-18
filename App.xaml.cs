using System.Configuration;
using System.Data;
using System.Windows;
using QuizApp.Services;

namespace QuizApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnExit(ExitEventArgs e)
    {
        ChinesePronunciationAiService.ShutdownAsrServer();
        base.OnExit(e);
    }
}
