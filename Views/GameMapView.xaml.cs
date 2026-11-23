using System.Windows;
using System.Windows.Controls;

namespace QuizApp.Views;

public partial class GameMapView : UserControl
{
    public GameMapView()
    {
        InitializeComponent();
    }

    private void ScrollLeft_Click(object sender, RoutedEventArgs e)
    {
        RealmScrollViewer.ScrollToHorizontalOffset(RealmScrollViewer.HorizontalOffset - 220);
    }

    private void ScrollRight_Click(object sender, RoutedEventArgs e)
    {
        RealmScrollViewer.ScrollToHorizontalOffset(RealmScrollViewer.HorizontalOffset + 220);
    }
}
