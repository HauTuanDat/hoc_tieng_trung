using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QuizApp.Views;

public partial class DashboardView : UserControl
{
    public static readonly DependencyProperty QuickGridColumnsProperty =
        DependencyProperty.Register(
            nameof(QuickGridColumns),
            typeof(int),
            typeof(DashboardView),
            new PropertyMetadata(4));

    public static readonly DependencyProperty FeaturedGridColumnsProperty =
        DependencyProperty.Register(
            nameof(FeaturedGridColumns),
            typeof(int),
            typeof(DashboardView),
            new PropertyMetadata(3));

    public static readonly DependencyProperty PagePaddingProperty =
        DependencyProperty.Register(
            nameof(PagePadding),
            typeof(Thickness),
            typeof(DashboardView),
            new PropertyMetadata(new Thickness(24, 20, 24, 26)));

    public static readonly DependencyProperty RoundedClipRadiusProperty =
        DependencyProperty.RegisterAttached(
            "RoundedClipRadius",
            typeof(double),
            typeof(DashboardView),
            new PropertyMetadata(0d, OnRoundedClipRadiusChanged));

    public static readonly DependencyProperty ProgressArcPercentProperty =
        DependencyProperty.RegisterAttached(
            "ProgressArcPercent",
            typeof(int),
            typeof(DashboardView),
            new PropertyMetadata(0, OnProgressArcPercentChanged));

    public int QuickGridColumns
    {
        get => (int)GetValue(QuickGridColumnsProperty);
        set => SetValue(QuickGridColumnsProperty, value);
    }

    public int FeaturedGridColumns
    {
        get => (int)GetValue(FeaturedGridColumnsProperty);
        set => SetValue(FeaturedGridColumnsProperty, value);
    }

    public Thickness PagePadding
    {
        get => (Thickness)GetValue(PagePaddingProperty);
        set => SetValue(PagePaddingProperty, value);
    }

    public DashboardView()
    {
        InitializeComponent();
        SizeChanged += OnViewSizeChanged;
    }

    public static double GetRoundedClipRadius(DependencyObject obj) =>
        (double)obj.GetValue(RoundedClipRadiusProperty);

    public static void SetRoundedClipRadius(DependencyObject obj, double value) =>
        obj.SetValue(RoundedClipRadiusProperty, value);

    public static int GetProgressArcPercent(DependencyObject obj) =>
        (int)obj.GetValue(ProgressArcPercentProperty);

    public static void SetProgressArcPercent(DependencyObject obj, int value) =>
        obj.SetValue(ProgressArcPercentProperty, value);

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

    private static void OnProgressArcPercentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Path path)
        {
            UpdateProgressArc(path, (int)e.NewValue);
        }
    }

    private static void UpdateProgressArc(Path path, int percent)
    {
        double clamped = Math.Clamp(percent, 0, 100);
        if (clamped <= 0)
        {
            path.Data = Geometry.Empty;
            return;
        }

        const double center = 70;
        const double radius = 61;
        double angle = clamped >= 100 ? 359.99 : clamped * 3.6;
        double radians = (angle - 90.0) * Math.PI / 180.0;

        var startPoint = new Point(center, center - radius);
        var endPoint = new Point(
            center + radius * Math.Cos(radians),
            center + radius * Math.Sin(radians));

        bool isLargeArc = angle > 180.0;

        var figure = new PathFigure { StartPoint = startPoint, IsClosed = false };
        figure.Segments.Add(new ArcSegment(
            endPoint,
            new Size(radius, radius),
            0,
            isLargeArc,
            SweepDirection.Clockwise,
            true));

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        geometry.Freeze();
        path.Data = geometry;
    }

    private void OnViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        double width = e.NewSize.Width;
        if (width <= 0) return;

        if (width < 600)
        {
            QuickGridColumns = 1;
            FeaturedGridColumns = 1;
            PagePadding = new Thickness(14, 14, 14, 20);
        }
        else if (width < 940)
        {
            QuickGridColumns = 2;
            FeaturedGridColumns = width < 760 ? 1 : 3;
            PagePadding = new Thickness(18, 16, 18, 22);
        }
        else
        {
            QuickGridColumns = 4;
            FeaturedGridColumns = 3;
            PagePadding = new Thickness(24, 20, 24, 26);
        }

        if (MainColumnsGrid == null || SecondaryColumnPanel == null || ColGap == null || ColRight == null)
        {
            return;
        }

        if (width < 940)
        {
            ColGap.Width = new GridLength(0);
            ColRight.Width = new GridLength(0);
            ColRight.MinWidth = 0;
            Grid.SetColumn(SecondaryColumnPanel, 0);
            Grid.SetRow(SecondaryColumnPanel, 1);
            SecondaryColumnPanel.Margin = new Thickness(0, 16, 0, 0);
        }
        else
        {
            ColGap.Width = new GridLength(18);
            ColRight.Width = new GridLength(0.95, GridUnitType.Star);
            ColRight.MinWidth = 300;
            Grid.SetColumn(SecondaryColumnPanel, 2);
            Grid.SetRow(SecondaryColumnPanel, 0);
            SecondaryColumnPanel.Margin = new Thickness(0);
        }
    }
}
