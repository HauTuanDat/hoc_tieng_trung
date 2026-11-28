using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using QuizApp.Services;

namespace QuizApp.ViewModels;

public class CourseCardItem : ViewModelBase
{
    private int _progressPercent = 0;

    public string LevelKey { get; set; } = "";
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string LessonCountText { get; set; } = "";
    public string IllustrationKey { get; set; } = "";

    public int ProgressPercent
    {
        get => _progressPercent;
        set => SetProperty(ref _progressPercent, value);
    }

    public string AccentColor { get; set; } = "#16A765";
    public string SoftColor { get; set; } = "#E2F9EC";
    public bool IsSpecialized { get; set; }

    public string IconPathData { get; set; } = "";

    public Brush AccentBrush { get; set; } = Brushes.Transparent;
    public Brush SoftBrush { get; set; } = Brushes.Transparent;
    public Brush IconBgBrush { get; set; } = Brushes.Transparent;
    public Brush IconBorderBrush { get; set; } = Brushes.Transparent;
    public Brush ButtonBrush { get; set; } = Brushes.Transparent;
    public Brush ButtonHoverBrush { get; set; } = Brushes.Transparent;
    public Brush IllustrationGlowBrush { get; set; } = Brushes.Transparent;
}

public class CourseSelectionViewModel : ViewModelBase
{
    private readonly IHskService _hskService;
    private readonly Action<string> _onSelectCourse;
    private readonly Action _onBack;

    public CourseSelectionViewModel(IHskService hskService, Action<string> onSelectCourse, Action onBack)
    {
        _hskService = hskService;
        _onSelectCourse = onSelectCourse;
        _onBack = onBack;

        SelectCourseCommand = new RelayCommand(param =>
        {
            string lvl = param as string ?? "HSK 1";
            _onSelectCourse(lvl);
        });

        BackCommand = new RelayCommand(_onBack);
        LoadCourses();
        _ = RefreshProgressFromDbAsync();
    }

    public ObservableCollection<CourseCardItem> HskCourses { get; } = new();
    public ObservableCollection<CourseCardItem> SpecializedCourses { get; } = new();

    public ICommand SelectCourseCommand { get; }
    public ICommand BackCommand { get; }

    private static Color ParseColor(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    private static SolidColorBrush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static Color MixWithWhite(Color color, double whiteRatio)
    {
        byte r = (byte)Math.Round(color.R + (255 - color.R) * whiteRatio);
        byte g = (byte)Math.Round(color.G + (255 - color.G) * whiteRatio);
        byte b = (byte)Math.Round(color.B + (255 - color.B) * whiteRatio);
        return Color.FromRgb(r, g, b);
    }

    private static LinearGradientBrush CreateGradientBrush(string startHex, string endHex, double angleRatio = 0.25)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint = new System.Windows.Point(1, angleRatio)
        };
        brush.GradientStops.Add(new GradientStop(ParseColor(startHex), 0.0));
        brush.GradientStops.Add(new GradientStop(ParseColor(endHex), 1.0));
        brush.Freeze();
        return brush;
    }

    private static LinearGradientBrush CreateHoverGradientBrush(string startHex, string endHex)
    {
        var c1 = MixWithWhite(ParseColor(startHex), 0.12);
        var c2 = MixWithWhite(ParseColor(endHex), 0.12);
        var brush = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint = new System.Windows.Point(1, 0.25)
        };
        brush.GradientStops.Add(new GradientStop(c1, 0.0));
        brush.GradientStops.Add(new GradientStop(c2, 1.0));
        brush.Freeze();
        return brush;
    }

    private static RadialGradientBrush CreateSoftGlowBrush(string softHex)
    {
        var soft = ParseColor(softHex);
        var brush = new RadialGradientBrush
        {
            Center = new System.Windows.Point(0.5, 0.88),
            GradientOrigin = new System.Windows.Point(0.5, 0.88),
            RadiusX = 0.65,
            RadiusY = 0.78
        };
        brush.GradientStops.Add(new GradientStop(soft, 0.0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, soft.R, soft.G, soft.B), 1.0));
        brush.Freeze();
        return brush;
    }

    private static CourseCardItem CreateCourseCard(
        string levelKey,
        string title,
        string subtitle,
        string lessonCountText,
        string illustrationKey,
        string accentHex,
        string softHex,
        string gradStartHex,
        string gradEndHex,
        string iconPathData,
        bool isSpecialized = false)
    {
        var accentColor = ParseColor(accentHex);
        var softColor = ParseColor(softHex);

        return new CourseCardItem
        {
            LevelKey = levelKey,
            Title = title,
            Subtitle = subtitle,
            LessonCountText = lessonCountText,
            IllustrationKey = illustrationKey,
            ProgressPercent = 0,
            AccentColor = accentHex,
            SoftColor = softHex,
            IsSpecialized = isSpecialized,
            IconPathData = iconPathData,
            AccentBrush = CreateFrozenBrush(accentColor),
            SoftBrush = CreateFrozenBrush(softColor),
            IconBgBrush = CreateFrozenBrush(MixWithWhite(softColor, 0.35)),
            IconBorderBrush = CreateFrozenBrush(MixWithWhite(accentColor, 0.65)),
            ButtonBrush = CreateGradientBrush(gradStartHex, gradEndHex),
            ButtonHoverBrush = CreateHoverGradientBrush(gradStartHex, gradEndHex),
            IllustrationGlowBrush = CreateSoftGlowBrush(softHex)
        };
    }

    private void LoadCourses()
    {
        // 6 distinct HSK colors matching the learning dashboard palette
        HskCourses.Clear();
        HskCourses.Add(CreateCourseCard(
            levelKey: "HSK 1",
            title: "HSK 1",
            subtitle: "Cơ bản",
            lessonCountText: "12 bài học",
            illustrationKey: "hsk1",
            accentHex: "#16A765",
            softHex: "#E2F9EC",
            gradStartHex: "#16A765",
            gradEndHex: "#58D98B",
            // Lucide Triangle
            iconPathData: "M 13.73,4 A 2,2 0 0 0 10.27,4 L 2.27,18 A 2,2 0 0 0 4,21 H 20 A 2,2 0 0 0 21.73,18 Z"));

        HskCourses.Add(CreateCourseCard(
            levelKey: "HSK 2",
            title: "HSK 2",
            subtitle: "Sơ cấp",
            lessonCountText: "16 bài học",
            illustrationKey: "hsk2",
            accentHex: "#168DE5",
            softHex: "#E2F2FF",
            gradStartHex: "#168DE5",
            gradEndHex: "#42C4EE",
            // Lucide Map
            iconPathData: "M 3,6 L 9,3 L 15,6 L 21,3 V 18 L 15,21 L 9,18 L 3,21 Z M 9,3 V 18 M 15,6 V 21"));

        HskCourses.Add(CreateCourseCard(
            levelKey: "HSK 3",
            title: "HSK 3",
            subtitle: "Trung cấp",
            lessonCountText: "20 bài học",
            illustrationKey: "hsk3",
            accentHex: "#F07824",
            softHex: "#FFF0E3",
            gradStartHex: "#F07824",
            gradEndHex: "#FFB04A",
            // Lucide Landmark
            iconPathData: "M 3,22 H 21 M 6,18 V 11 M 10,18 V 11 M 14,18 V 11 M 18,18 V 11 M 12,2 L 20,7 H 4 Z"));

        HskCourses.Add(CreateCourseCard(
            levelKey: "HSK 4",
            title: "HSK 4",
            subtitle: "Trung cao cấp",
            lessonCountText: "24 bài học",
            illustrationKey: "hsk4",
            accentHex: "#883FE5",
            softHex: "#F1E5FF",
            gradStartHex: "#883FE5",
            gradEndHex: "#B77BFA",
            // Lucide Castle
            iconPathData: "M 22,20 V 9 L 19,11 L 17,9 V 4 H 14 V 6 H 10 V 4 H 7 V 9 L 5,11 L 2,9 V 20 Z M 10,20 V 15 A 2,2 0 0 1 14,15 V 20"));

        HskCourses.Add(CreateCourseCard(
            levelKey: "HSK 5",
            title: "HSK 5",
            subtitle: "Nâng cao",
            lessonCountText: "28 bài học",
            illustrationKey: "hsk5",
            accentHex: "#079EAA",
            softHex: "#DDF8F7",
            gradStartHex: "#079EAA",
            gradEndHex: "#40D4C7",
            // Lucide Mountain
            iconPathData: "M 8,3 L 12,11 L 17,6 L 22,21 H 2 Z"));

        HskCourses.Add(CreateCourseCard(
            levelKey: "HSK 6",
            title: "HSK 6",
            subtitle: "Cao cấp",
            lessonCountText: "32 bài học",
            illustrationKey: "hsk6",
            accentHex: "#E83D61",
            softHex: "#FFE5EB",
            gradStartHex: "#E83D61",
            gradEndHex: "#FF8494",
            // Lucide Building2
            iconPathData: "M 6,22 V 4 A 2,2 0 0 1 8,2 H 16 A 2,2 0 0 1 18,4 V 22 Z M 6,12 H 4 A 2,2 0 0 0 2,14 V 20 A 2,2 0 0 0 4,22 H 6 M 18,9 H 20 A 2,2 0 0 1 22,11 V 20 A 2,2 0 0 1 20,22 H 18 M 10,6 H 14 M 10,10 H 14 M 10,14 H 14 M 10,18 H 14"));

        // 3 distinct specialty colors (9 unique colors total across all courses)
        SpecializedCourses.Clear();
        SpecializedCourses.Add(CreateCourseCard(
            levelKey: "Công xưởng",
            title: "Từ vựng công xưởng",
            subtitle: "Sản xuất, kỹ thuật & QC",
            lessonCountText: "15 bài học",
            illustrationKey: "factory",
            accentHex: "#2457C5",
            softHex: "#E3EDFF",
            gradStartHex: "#2457C5",
            gradEndHex: "#559BFF",
            // Lucide Factory
            iconPathData: "M 2,20 A 2,2 0 0 0 4,22 H 20 A 2,2 0 0 0 22,20 V 8 L 15,13 V 8 L 8,13 V 4 A 2,2 0 0 0 6,2 H 4 A 2,2 0 0 0 2,4 Z M 17,18 H 18 M 12,18 H 13 M 7,18 H 8",
            isSpecialized: true));

        SpecializedCourses.Add(CreateCourseCard(
            levelKey: "Kế toán",
            title: "Kế toán doanh nghiệp",
            subtitle: "Hóa đơn, thuế & tài chính",
            lessonCountText: "12 bài học",
            illustrationKey: "accounting",
            accentHex: "#C026A8",
            softHex: "#FBE4F6",
            gradStartHex: "#C026A8",
            gradEndHex: "#EA79D5",
            // Lucide Calculator
            iconPathData: "M 6,2 H 18 A 2,2 0 0 1 20,4 V 20 A 2,2 0 0 1 18,22 H 6 A 2,2 0 0 1 4,20 V 4 A 2,2 0 0 1 6,2 Z M 8,6 H 16 M 16,14 V 18 M 16,10 H 16.01 M 12,10 H 12.01 M 8,10 H 8.01 M 12,14 H 12.01 M 8,14 H 8.01 M 12,18 H 12.01 M 8,18 H 8.01",
            isSpecialized: true));

        SpecializedCourses.Add(CreateCourseCard(
            levelKey: "Xuất nhập khẩu",
            title: "Xuất nhập khẩu (XNK)",
            subtitle: "Logistics, hải quan & thanh toán",
            lessonCountText: "16 bài học",
            illustrationKey: "shipping",
            accentHex: "#70A91C",
            softHex: "#EDF8D9",
            gradStartHex: "#70A91C",
            gradEndHex: "#A6D84B",
            // Lucide Package
            iconPathData: "M 11,21.73 A 2,2 0 0 0 13,21.73 L 20,17.73 A 2,2 0 0 0 21,16 V 8 A 2,2 0 0 0 20,6.27 L 13,2.27 A 2,2 0 0 0 11,2.27 L 4,6.27 A 2,2 0 0 0 3,8 V 16 A 2,2 0 0 0 4,17.73 Z M 12,22 V 12 M 3.3,7 L 12,12 L 20.7,7 M 7.5,4.27 L 16.5,9.42",
            isSpecialized: true));
    }

    private async Task RefreshProgressFromDbAsync()
    {
        try
        {
            var list = await _hskService.GetLevelProgressListAsync();
            var dict = list.ToDictionary(x => x.LevelName, x => (int)x.ProgressPercentage, StringComparer.OrdinalIgnoreCase);

            foreach (var c in HskCourses)
            {
                if (dict.TryGetValue(c.LevelKey, out int p))
                {
                    c.ProgressPercent = p;
                }
            }

            foreach (var c in SpecializedCourses)
            {
                if (dict.TryGetValue(c.LevelKey, out int p))
                {
                    c.ProgressPercent = p;
                }
            }
        }
        catch
        {
            // Keep default 0%
        }
    }
}
