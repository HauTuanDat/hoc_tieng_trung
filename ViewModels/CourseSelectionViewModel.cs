using System.Collections.ObjectModel;
using System.Windows.Input;
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

    private void LoadCourses()
    {
        HskCourses.Clear();
        HskCourses.Add(new CourseCardItem { LevelKey = "HSK 1", Title = "HSK 1", Subtitle = "Cơ bản", LessonCountText = "12 bài học", ProgressPercent = 0, AccentColor = "#208B58", BgTint = "#EBF7F0", Icon = "🏯" });
        HskCourses.Add(new CourseCardItem { LevelKey = "HSK 2", Title = "HSK 2", Subtitle = "Sơ cấp", LessonCountText = "18 bài học", ProgressPercent = 0, AccentColor = "#0D9488", BgTint = "#E8F6F4", Icon = "⛩️" });
        HskCourses.Add(new CourseCardItem { LevelKey = "HSK 3", Title = "HSK 3", Subtitle = "Trung cấp", LessonCountText = "20 bài học", ProgressPercent = 0, AccentColor = "#2563EB", BgTint = "#EFF6FF", Icon = "🏛️" });
        HskCourses.Add(new CourseCardItem { LevelKey = "HSK 4", Title = "HSK 4", Subtitle = "Trung cao cấp", LessonCountText = "24 bài học", ProgressPercent = 0, AccentColor = "#D97706", BgTint = "#FEF3C7", Icon = "🏯" });
        HskCourses.Add(new CourseCardItem { LevelKey = "HSK 5", Title = "HSK 5", Subtitle = "Nâng cao", LessonCountText = "28 bài học", ProgressPercent = 0, AccentColor = "#7C3AED", BgTint = "#F3EDFD", Icon = "⛩️" });
        HskCourses.Add(new CourseCardItem { LevelKey = "HSK 6", Title = "HSK 6", Subtitle = "Cao cấp", LessonCountText = "32 bài học", ProgressPercent = 0, AccentColor = "#C8372D", BgTint = "#FDF2F0", Icon = "🏛️" });

        SpecializedCourses.Clear();
        SpecializedCourses.Add(new CourseCardItem { LevelKey = "Công xưởng", Title = "Từ vựng công xưởng", Subtitle = "Sản xuất, kỹ thuật & QC", LessonCountText = "15 bài học", ProgressPercent = 0, AccentColor = "#1B7A4E", BgTint = "#EBF5EE", Icon = "🏭", IsSpecialized = true });
        SpecializedCourses.Add(new CourseCardItem { LevelKey = "Kế toán", Title = "Kế toán doanh nghiệp", Subtitle = "Hóa đơn, thuế & tài chính", LessonCountText = "12 bài học", ProgressPercent = 0, AccentColor = "#2563EB", BgTint = "#EFF6FF", Icon = "🧮", IsSpecialized = true });
        SpecializedCourses.Add(new CourseCardItem { LevelKey = "Xuất nhập khẩu", Title = "Xuất nhập khẩu (XNK)", Subtitle = "Logistics, hải quan & thanh toán", LessonCountText = "16 bài học", ProgressPercent = 0, AccentColor = "#D97706", BgTint = "#FFFBEB", Icon = "🚢", IsSpecialized = true });
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

