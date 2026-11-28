using System.Windows;
using System.Windows.Controls;

namespace QuizApp.Views;

public partial class CourseIllustrationControl : UserControl
{
    public static readonly DependencyProperty IllustrationKeyProperty =
        DependencyProperty.Register(
            nameof(IllustrationKey),
            typeof(string),
            typeof(CourseIllustrationControl),
            new PropertyMetadata(string.Empty, OnIllustrationKeyChanged));

    public string IllustrationKey
    {
        get => (string)GetValue(IllustrationKeyProperty);
        set => SetValue(IllustrationKeyProperty, value);
    }

    public CourseIllustrationControl()
    {
        InitializeComponent();
        UpdateVisibleArtwork();
    }

    private static void OnIllustrationKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CourseIllustrationControl control)
        {
            control.UpdateVisibleArtwork();
        }
    }

    private void UpdateVisibleArtwork()
    {
        string key = IllustrationKey ?? string.Empty;

        Hsk1Art.Visibility = key == "hsk1" ? Visibility.Visible : Visibility.Collapsed;
        Hsk2Art.Visibility = key == "hsk2" ? Visibility.Visible : Visibility.Collapsed;
        Hsk3Art.Visibility = key == "hsk3" ? Visibility.Visible : Visibility.Collapsed;
        Hsk4Art.Visibility = key == "hsk4" ? Visibility.Visible : Visibility.Collapsed;
        Hsk5Art.Visibility = key == "hsk5" ? Visibility.Visible : Visibility.Collapsed;
        Hsk6Art.Visibility = key == "hsk6" ? Visibility.Visible : Visibility.Collapsed;
        FactoryArt.Visibility = key == "factory" ? Visibility.Visible : Visibility.Collapsed;
        AccountingArt.Visibility = key == "accounting" ? Visibility.Visible : Visibility.Collapsed;
        ShippingArt.Visibility = key == "shipping" ? Visibility.Visible : Visibility.Collapsed;
    }
}
