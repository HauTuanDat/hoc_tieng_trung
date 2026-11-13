using System.Windows;
using System.Windows.Input;
using QuizApp.ViewModels;

namespace QuizApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        // Avoid handling shortcuts when focused on text boxes
        if (e.OriginalSource is System.Windows.Controls.TextBox)
        {
            return;
        }

        if (DataContext is MainViewModel mainVm)
        {
            mainVm.HandleKeyDown(e.Key);
        }
    }
}