using System;
using System.IO;
using System.Windows;

namespace QuizApp.Services;

public enum AppTheme
{
    Light,
    [Obsolete("Chế độ tối đã được loại bỏ hoàn toàn.")]
    Dark
}

public class ThemeService
{
    private static ThemeService? _instance;
    public static ThemeService Instance => _instance ??= new ThemeService();

    private readonly string _settingsFilePath;

    public AppTheme CurrentTheme => AppTheme.Light;
    public bool IsDarkMode => false;
    public bool IsLightMode => true;

    public event Action<AppTheme>? ThemeChanged;

    private ThemeService()
    {
        try
        {
            var appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MocNgu"
            );
            Directory.CreateDirectory(appDataDir);
            _settingsFilePath = Path.Combine(appDataDir, "theme_setting.txt");
            File.WriteAllText(_settingsFilePath, "Light");
        }
        catch
        {
            _settingsFilePath = "theme_setting.txt";
        }
    }

    public void Initialize()
    {
        ApplyTheme(AppTheme.Light);
    }

    public void SetTheme(AppTheme theme)
    {
        ApplyTheme(AppTheme.Light);
    }

    public void ToggleTheme()
    {
        // Chế độ tối đã bị loại bỏ, ứng dụng luôn ở chế độ sáng
        ApplyTheme(AppTheme.Light);
    }

    public void ApplyTheme(AppTheme theme = AppTheme.Light)
    {
        try
        {
            var themeUri = new Uri("Styles/Themes/LightTheme.xaml", UriKind.Relative);
            var themeDict = new ResourceDictionary { Source = themeUri };

            if (Application.Current != null)
            {
                var appResources = Application.Current.Resources;
                // Swap in MergedDictionaries if present
                for (int i = 0; i < appResources.MergedDictionaries.Count; i++)
                {
                    var dict = appResources.MergedDictionaries[i];
                    for (int j = 0; j < dict.MergedDictionaries.Count; j++)
                    {
                        var child = dict.MergedDictionaries[j];
                        if (child.Source != null && child.Source.OriginalString.Contains("Theme.xaml"))
                        {
                            dict.MergedDictionaries[j] = themeDict;
                        }
                    }
                }

                foreach (var key in themeDict.Keys)
                {
                    appResources[key] = themeDict[key];
                }
            }

            try
            {
                File.WriteAllText(_settingsFilePath, "Light");
            }
            catch { }

            ThemeChanged?.Invoke(AppTheme.Light);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to apply theme: {ex.Message}");
        }
    }
}
