using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.ViewModels;

public class CourseCardItem : ViewModelBase
{
    private int _progressPercent = 0;

    public string LevelKey { get; set; } = "";
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string LessonCountText { get; set; } = "";
    public int ProgressPercent
    {
        get => _progressPercent;
        set => SetProperty(ref _progressPercent, value);
    }
    public string AccentColor { get; set; } = "#208B58";
    public string BgTint { get; set; } = "#FDF2F0";
    public string Icon { get; set; } = "🏯";
    public bool IsSpecialized { get; set; }

    // Redesigned UI properties
    public string BadgeBg { get; set; } = "#DCFCE7";
    public string BadgeFg { get; set; } = "#16A34A";
    public string IconBg { get; set; } = "#F0FDF4";
    public string IconBorder { get; set; } = "#DCFCE7";
    public string IconColor { get; set; } = "#16A34A";
    public string IconPathData { get; set; } = "";
    public Brush ButtonBrush { get; set; } = Brushes.Transparent;
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

    private static LinearGradientBrush CreateHorizontalBrush(string startHex, string endHex)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint = new System.Windows.Point(1, 0)
        };
        brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(startHex), 0.0));
        brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(endHex), 1.0));
        brush.Freeze();
        return brush;
    }

    private void LoadCourses()
    {
        HskCourses.Clear();
        HskCourses.Add(new CourseCardItem
        {
            LevelKey = "HSK 1",
            Title = "HSK 1",
            Subtitle = "Cơ bản",
            LessonCountText = "12 bài học",
            ProgressPercent = 0,
            AccentColor = "#16A34A",
            BgTint = "#DCFCE7",
            Icon = "⛺",
            BadgeBg = "#DCFCE7",
            BadgeFg = "#16A34A",
            IconBg = "#F0FDF4",
            IconBorder = "#BBF7D0",
            IconColor = "#16A34A",
            IconPathData = "M 7,32 L 20,8 L 33,32 Z M 18,5 L 20,8 L 22,5 M 12,23 L 28,23 M 16,32 L 20,24 L 24,32",
            ButtonBrush = CreateHorizontalBrush("#16A34A", "#22C55E")
        });

        HskCourses.Add(new CourseCardItem
        {
            LevelKey = "HSK 2",
            Title = "HSK 2",
            Subtitle = "Sơ cấp",
            LessonCountText = "16 bài học",
            ProgressPercent = 0,
            AccentColor = "#0284C7",
            BgTint = "#E0F2FE",
            Icon = "🗺️",
            BadgeBg = "#E0F2FE",
            BadgeFg = "#0284C7",
            IconBg = "#F0F9FF",
            IconBorder = "#BAE6FD",
            IconColor = "#0284C7",
            IconPathData = "M 7,10 L 16,7 L 24,10 L 33,7 L 33,30 L 24,33 L 16,30 L 7,33 Z M 16,7 L 16,30 M 24,10 L 24,33",
            ButtonBrush = CreateHorizontalBrush("#0284C7", "#06B6D4")
        });

        HskCourses.Add(new CourseCardItem
        {
            LevelKey = "HSK 3",
            Title = "HSK 3",
            Subtitle = "Trung cấp",
            LessonCountText = "20 bài học",
            ProgressPercent = 0,
            AccentColor = "#2563EB",
            BgTint = "#DBEAFE",
            Icon = "🏛️",
            BadgeBg = "#DBEAFE",
            BadgeFg = "#2563EB",
            IconBg = "#EFF6FF",
            IconBorder = "#BFDBFE",
            IconColor = "#2563EB",
            IconPathData = "M 6,15 L 20,8 L 34,15 Z M 8,17 L 32,17 M 11,17 V 29 M 17,17 V 29 M 23,17 V 29 M 29,17 V 29 M 8,29 H 32 M 5,33 H 35",
            ButtonBrush = CreateHorizontalBrush("#2563EB", "#3B82F6")
        });

        HskCourses.Add(new CourseCardItem
        {
            LevelKey = "HSK 4",
            Title = "HSK 4",
            Subtitle = "Trung cao cấp",
            LessonCountText = "24 bài học",
            ProgressPercent = 0,
            AccentColor = "#7C3AED",
            BgTint = "#F3E8FF",
            Icon = "🏰",
            BadgeBg = "#F3E8FF",
            BadgeFg = "#7C3AED",
            IconBg = "#FAF5FF",
            IconBorder = "#E9D5FF",
            IconColor = "#7C3AED",
            IconPathData = "M 8,33 V 13 H 13 V 17 H 17 V 13 H 23 V 17 H 27 V 13 H 32 V 33 Z M 16,33 V 26 A 4,4 0 0 1 24,26 V 33",
            ButtonBrush = CreateHorizontalBrush("#7C3AED", "#8B5CF6")
        });

        HskCourses.Add(new CourseCardItem
        {
            LevelKey = "HSK 5",
            Title = "HSK 5",
            Subtitle = "Nâng cao",
            LessonCountText = "28 bài học",
            ProgressPercent = 0,
            AccentColor = "#C026D3",
            BgTint = "#FCE7F3",
            Icon = "🏔️",
            BadgeBg = "#FCE7F3",
            BadgeFg = "#C026D3",
            IconBg = "#FDF2F8",
            IconBorder = "#FBCFE8",
            IconColor = "#C026D3",
            IconPathData = "M 6,32 L 15,16 L 22,23 L 30,12 L 35,32 Z",
            ButtonBrush = CreateHorizontalBrush("#C026D3", "#E879F9")
        });

        HskCourses.Add(new CourseCardItem
        {
            LevelKey = "HSK 6",
            Title = "HSK 6",
            Subtitle = "Cao cấp",
            LessonCountText = "32 bài học",
            ProgressPercent = 0,
            AccentColor = "#E11D48",
            BgTint = "#FFE4E6",
            Icon = "🏢",
            BadgeBg = "#FFE4E6",
            BadgeFg = "#E11D48",
            IconBg = "#FFF1F2",
            IconBorder = "#FECDD3",
            IconColor = "#E11D48",
            IconPathData = "M 11,33 V 10 C 11,8 13,7 15,7 H 25 C 27,7 29,8 29,10 V 33 Z M 16,13 H 18 M 22,13 H 24 M 16,18 H 18 M 22,18 H 24 M 16,23 H 18 M 22,23 H 24 M 18,33 V 28 H 22 V 33",
            ButtonBrush = CreateHorizontalBrush("#E11D48", "#FB7185")
        });

        SpecializedCourses.Clear();
        SpecializedCourses.Add(new CourseCardItem
        {
            LevelKey = "Công xưởng",
            Title = "Từ vựng công xưởng",
            Subtitle = "Sản xuất, kỹ thuật & QC",
            LessonCountText = "15 bài học",
            ProgressPercent = 0,
            AccentColor = "#0284C7",
            BgTint = "#E0F2FE",
            Icon = "⚙️",
            BadgeBg = "#EFF6FF",
            BadgeFg = "#0284C7",
            IconBg = "#E0F2FE",
            IconBorder = "#BAE6FD",
            IconColor = "#0284C7",
            IconPathData = "M 19.4,13 C 19.5,12.7 19.5,12.3 19.5,12 C 19.5,11.7 19.5,11.3 19.4,11 L 21.5,9.3 C 21.7,9.1 21.8,8.8 21.6,8.6 L 19.6,5.1 C 19.5,4.9 19.2,4.8 19,4.9 L 16.5,5.9 C 16,5.5 15.4,5.2 14.8,5 L 14.4,2.3 C 14.4,2.1 14.2,2 14,2 L 10,2 C 9.8,2 9.6,2.1 9.6,2.3 L 9.2,5 C 8.6,5.2 8,5.6 7.5,5.9 L 5,4.9 C 4.8,4.8 4.5,4.9 4.4,5.1 L 2.4,8.6 C 2.2,8.8 2.3,9.1 2.5,9.3 L 4.6,11 C 4.5,11.3 4.5,11.7 4.5,12 C 4.5,12.3 4.5,12.7 4.6,13 L 2.5,14.7 C 2.3,14.9 2.2,15.2 2.4,15.4 L 4.4,18.9 C 4.5,19.1 4.8,19.2 5,19.1 L 7.5,18.1 C 8,18.5 8.6,18.8 9.2,19 L 9.6,21.7 C 9.6,21.9 9.8,22 10,22 L 14,22 C 14.2,22 14.4,21.9 14.4,21.7 L 14.8,19 C 15.4,18.8 16,18.4 16.5,18.1 L 19,19.1 C 19.2,19.2 19.5,19.1 19.6,18.9 L 21.6,15.4 C 21.8,15.2 21.7,14.9 21.5,14.7 L 19.4,13 Z M 12,15.5 C 10.1,15.5 8.5,13.9 8.5,12 C 8.5,10.1 10.1,8.5 12,8.5 C 13.9,8.5 15.5,10.1 15.5,12 C 15.5,13.9 13.9,15.5 12,15.5 Z",
            IsSpecialized = true
        });

        SpecializedCourses.Add(new CourseCardItem
        {
            LevelKey = "Kế toán",
            Title = "Kế toán doanh nghiệp",
            Subtitle = "Hóa đơn, thuế & tài chính",
            LessonCountText = "12 bài học",
            ProgressPercent = 0,
            AccentColor = "#7C3AED",
            BgTint = "#F3E8FF",
            Icon = "📈",
            BadgeBg = "#FAF5FF",
            BadgeFg = "#7C3AED",
            IconBg = "#F3E8FF",
            IconBorder = "#E9D5FF",
            IconColor = "#7C3AED",
            IconPathData = "M 3.5,18.5 L 8.5,13.5 L 12.5,17.5 L 20.5,9.5 M 15,9.5 H 20.5 V 15 M 4,20 V 16 M 9,20 V 12 M 14,20 V 14 M 19,20 V 8",
            IsSpecialized = true
        });

        SpecializedCourses.Add(new CourseCardItem
        {
            LevelKey = "Xuất nhập khẩu",
            Title = "Xuất nhập khẩu (XNK)",
            Subtitle = "Logistics, hải quan & thanh toán",
            LessonCountText = "16 bài học",
            ProgressPercent = 0,
            AccentColor = "#16A34A",
            BgTint = "#DCFCE7",
            Icon = "📦",
            BadgeBg = "#F0FDF4",
            BadgeFg = "#16A34A",
            IconBg = "#DCFCE7",
            IconBorder = "#BBF7D0",
            IconColor = "#16A34A",
            IconPathData = "M 3,9 L 12,3 L 21,9 V 21 H 3 Z M 8,21 V 13 H 16 V 21",
            IsSpecialized = true
        });
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

