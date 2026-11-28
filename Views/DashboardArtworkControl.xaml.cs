using System.Windows;
using System.Windows.Controls;

namespace QuizApp.Views;

public partial class DashboardArtworkControl : UserControl
{
    public static readonly DependencyProperty ArtworkKeyProperty =
        DependencyProperty.Register(
            nameof(ArtworkKey),
            typeof(string),
            typeof(DashboardArtworkControl),
            new PropertyMetadata(string.Empty, OnArtworkKeyChanged));

    public string ArtworkKey
    {
        get => (string)GetValue(ArtworkKeyProperty);
        set => SetValue(ArtworkKeyProperty, value);
    }

    public DashboardArtworkControl()
    {
        InitializeComponent();
        UpdateVisibleArtwork();
    }

    private static void OnArtworkKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DashboardArtworkControl control)
        {
            control.UpdateVisibleArtwork();
        }
    }

    private void UpdateVisibleArtwork()
    {
        string key = ArtworkKey ?? string.Empty;

        HeroArt.Visibility = key == "hero" ? Visibility.Visible : Visibility.Collapsed;
        LessonArt.Visibility = key == "lesson" ? Visibility.Visible : Visibility.Collapsed;
        FeaturedHsk1Art.Visibility = key == "featured-hsk1" ? Visibility.Visible : Visibility.Collapsed;
        FeaturedHsk4Art.Visibility = key == "featured-hsk4" ? Visibility.Visible : Visibility.Collapsed;
        FeaturedXnkArt.Visibility = key == "featured-xnk" ? Visibility.Visible : Visibility.Collapsed;
        RewardMedalArt.Visibility = key == "reward-medal" ? Visibility.Visible : Visibility.Collapsed;
        AvatarPandaArt.Visibility = key == "panda" ? Visibility.Visible : Visibility.Collapsed;
        AvatarGirlArt.Visibility = key == "girl" ? Visibility.Visible : Visibility.Collapsed;
        AvatarBoyArt.Visibility = key == "boy" ? Visibility.Visible : Visibility.Collapsed;
    }
}
