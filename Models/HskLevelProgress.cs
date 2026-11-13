using System.Windows;
using System.Windows.Media;

namespace QuizApp.Models;

public class HskLevelProgress
{
    public string LevelName { get; set; } = "HSK 1";
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "🌱";
    public string AccentColor { get; set; } = "#5850EC";
    public string LightBgColor { get; set; } = "#EEF2FF";
    public string GradientStart { get; set; } = "#FF7A00";
    public string GradientEnd { get; set; } = "#EE3124";
    public int TotalWords { get; set; }
    public int MasteredWords { get; set; }

    public Brush BackgroundBrush
    {
        get
        {
            try
            {
                var c1 = (Color)ColorConverter.ConvertFromString(GradientStart);
                var c2 = (Color)ColorConverter.ConvertFromString(GradientEnd);
                var brush = new LinearGradientBrush(c1, c2, new Point(0, 0), new Point(0, 1));
                brush.Freeze();
                return brush;
            }
            catch
            {
                return new SolidColorBrush(Colors.Indigo);
            }
        }
    }

    public double ProgressPercentage => TotalWords > 0 ? ((double)MasteredWords / TotalWords) * 100.0 : 0;
    public string ProgressText => $"{MasteredWords} / {TotalWords} từ";
    public string SummaryText => $"{TotalWords} từ vựng";
}
