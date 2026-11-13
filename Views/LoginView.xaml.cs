using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using QuizApp.ViewModels;

namespace QuizApp.Views;

public partial class LoginView : UserControl
{
    private bool _isUpdatingPassword = false;

    public LoginView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        TryLoadBackgroundImage();
    }

    private void TryLoadBackgroundImage()
    {
        if (BackgroundImage == null) return;

        // 1. Try application pack URI
        try
        {
            var packUri = new Uri("pack://application:,,,/Assets/mocngu_login_bg.jpg", UriKind.Absolute);
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = packUri;
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.EndInit();
            BackgroundImage.Source = bi;
            return;
        }
        catch
        {
            // fallback to local file
        }

        // 2. Try bin output folder
        try
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string localPath = Path.Combine(basePath, "Assets", "mocngu_login_bg.jpg");
            if (File.Exists(localPath))
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(localPath, UriKind.Absolute);
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                BackgroundImage.Source = bi;
                return;
            }
        }
        catch
        {
            // fallback
        }

        // 3. Try project directory relative path
        try
        {
            string relPath = Path.GetFullPath("Assets/mocngu_login_bg.jpg");
            if (File.Exists(relPath))
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(relPath, UriKind.Absolute);
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                BackgroundImage.Source = bi;
            }
        }
        catch
        {
            // Soft fail: vector WPF elements are fully visible
        }
    }

    private void LoginPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingPassword) return;

        if (DataContext is LoginViewModel vm && sender is PasswordBox pb)
        {
            _isUpdatingPassword = true;
            vm.LoginPassword = pb.Password;
            if (LoginPasswordTextBox != null && LoginPasswordTextBox.Text != pb.Password)
            {
                LoginPasswordTextBox.Text = pb.Password;
            }
            _isUpdatingPassword = false;
        }
    }

    private void LoginPasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingPassword) return;

        if (DataContext is LoginViewModel vm && sender is TextBox tb)
        {
            _isUpdatingPassword = true;
            vm.LoginPassword = tb.Text;
            if (LoginPasswordBox != null && LoginPasswordBox.Password != tb.Text)
            {
                LoginPasswordBox.Password = tb.Text;
            }
            _isUpdatingPassword = false;
        }
    }

    private void RegisterPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && sender is PasswordBox pb)
        {
            vm.RegisterPassword = pb.Password;
        }
    }

    private void RegisterConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && sender is PasswordBox pb)
        {
            vm.RegisterConfirmPassword = pb.Password;
        }
    }
}
