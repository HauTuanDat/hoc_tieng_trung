using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QuizApp.Views;

public partial class CourseSelectionView : UserControl
{
    public static readonly DependencyProperty GridColumnsProperty =
        DependencyProperty.Register(
            nameof(GridColumns),
            typeof(int),
            typeof(CourseSelectionView),
            new PropertyMetadata(3));

    public static readonly DependencyProperty PagePaddingProperty =
        DependencyProperty.Register(
            nameof(PagePadding),
            typeof(Thickness),
            typeof(CourseSelectionView),
            new PropertyMetadata(new Thickness(28, 24, 28, 30)));

    public static readonly DependencyProperty RoundedClipRadiusProperty =
        DependencyProperty.RegisterAttached(
            "RoundedClipRadius",
            typeof(double),
            typeof(CourseSelectionView),
            new PropertyMetadata(0d, OnRoundedClipRadiusChanged));

    public int GridColumns
    {
        get => (int)GetValue(GridColumnsProperty);
        set => SetValue(GridColumnsProperty, value);
    }

    public Thickness PagePadding
    {
        get => (Thickness)GetValue(PagePaddingProperty);
        set => SetValue(PagePaddingProperty, value);
    }

    public CourseSelectionView()
    {
        InitializeComponent();
        SizeChanged += OnViewSizeChanged;
    }

    public static double GetRoundedClipRadius(DependencyObject obj) =>
        (double)obj.GetValue(RoundedClipRadiusProperty);

    public static void SetRoundedClipRadius(DependencyObject obj, double value) =>
        obj.SetValue(RoundedClipRadiusProperty, value);

    private static void OnRoundedClipRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement fe)
        {
            fe.SizeChanged -= OnClippedElementSizeChanged;
            fe.Loaded -= OnClippedElementLoaded;

            if ((double)e.NewValue > 0)
            {
                fe.SizeChanged += OnClippedElementSizeChanged;
                fe.Loaded += OnClippedElementLoaded;
                UpdateElementClip(fe);
            }
            else
            {
                fe.Clip = null;
            }
        }
    }

    private static void OnClippedElementLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            UpdateElementClip(fe);
        }
    }

    private static void OnClippedElementSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            UpdateElementClip(fe);
        }
    }

    private static void UpdateElementClip(FrameworkElement fe)
    {
        if (fe.ActualWidth <= 0 || fe.ActualHeight <= 0) return;

        double radius = GetRoundedClipRadius(fe);
        var geometry = new RectangleGeometry(new Rect(0, 0, fe.ActualWidth, fe.ActualHeight), radius, radius);
        geometry.Freeze();
        fe.Clip = geometry;
    }

    private void OnViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        double width = e.NewSize.Width;
        if (width <= 0) return;

        if (width < 640)
        {
            GridColumns = 1;
            PagePadding = new Thickness(14, 16, 14, 22);
        }
        else if (width < 980)
        {
            GridColumns = 2;
            PagePadding = new Thickness(20, 20, 20, 26);
        }
        else
        {
            GridColumns = 3;
            PagePadding = new Thickness(28, 24, 28, 30);
        }
    }
}
