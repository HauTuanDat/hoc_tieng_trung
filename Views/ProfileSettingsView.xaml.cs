using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QuizApp.Views;

public partial class ProfileSettingsView : UserControl
{
    public static readonly DependencyProperty PagePaddingProperty =
        DependencyProperty.Register(
            nameof(PagePadding),
            typeof(Thickness),
            typeof(ProfileSettingsView),
            new PropertyMetadata(new Thickness(32, 24, 32, 32)));

    public static readonly DependencyProperty IsCircularClipProperty =
        DependencyProperty.RegisterAttached(
            "IsCircularClip",
            typeof(bool),
            typeof(ProfileSettingsView),
            new PropertyMetadata(false, OnIsCircularClipChanged));

    public Thickness PagePadding
    {
        get => (Thickness)GetValue(PagePaddingProperty);
        set => SetValue(PagePaddingProperty, value);
    }

    public ProfileSettingsView()
    {
        InitializeComponent();
        SizeChanged += OnViewSizeChanged;
    }

    public static bool GetIsCircularClip(DependencyObject obj) =>
        (bool)obj.GetValue(IsCircularClipProperty);

    public static void SetIsCircularClip(DependencyObject obj, bool value) =>
        obj.SetValue(IsCircularClipProperty, value);

    private static void OnIsCircularClipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement fe)
        {
            fe.SizeChanged -= OnCircularElementSizeChanged;
            fe.Loaded -= OnCircularElementLoaded;
            if ((bool)e.NewValue)
            {
                fe.SizeChanged += OnCircularElementSizeChanged;
                fe.Loaded += OnCircularElementLoaded;
                UpdateCircularClip(fe);
            }
            else
            {
                fe.Clip = null;
            }
        }
    }

    private static void OnCircularElementLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe) UpdateCircularClip(fe);
    }

    private static void OnCircularElementSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is FrameworkElement fe) UpdateCircularClip(fe);
    }

    private static void UpdateCircularClip(FrameworkElement fe)
    {
        if (fe.ActualWidth <= 0 || fe.ActualHeight <= 0) return;
        double r = Math.Min(fe.ActualWidth, fe.ActualHeight) / 2.0;
        var geo = new EllipseGeometry(new Point(fe.ActualWidth / 2.0, fe.ActualHeight / 2.0), r, r);
        geo.Freeze();
        fe.Clip = geo;
    }

    private void OnViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        double width = e.NewSize.Width;
        if (width <= 0) return;

        PagePadding = width < 640
            ? new Thickness(16, 16, 16, 24)
            : new Thickness(32, 24, 32, 32);

        if (SettingsColumnsGrid == null || RightColumnPanel == null || SettingsColGap == null || SettingsColRight == null)
        {
            return;
        }

        if (width < 880)
        {
            SettingsColGap.Width = new GridLength(0);
            SettingsColRight.Width = new GridLength(0);
            Grid.SetColumn(RightColumnPanel, 0);
            Grid.SetRow(RightColumnPanel, 1);
            RightColumnPanel.Margin = new Thickness(0, 18, 0, 0);
        }
        else
        {
            SettingsColGap.Width = new GridLength(24);
            SettingsColRight.Width = new GridLength(1.0, GridUnitType.Star);
            Grid.SetColumn(RightColumnPanel, 2);
            Grid.SetRow(RightColumnPanel, 0);
            RightColumnPanel.Margin = new Thickness(0);
        }
    }
}
