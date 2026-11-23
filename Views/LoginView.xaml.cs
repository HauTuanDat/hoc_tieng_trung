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
        TryLoadBackgroundImages();
        UpdateResponsiveMode(ActualWidth, ActualHeight);
    }

    private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateResponsiveMode(e.NewSize.Width, e.NewSize.Height);
    }

    private void UpdateResponsiveMode(double width, double height)
    {
        if (width <= 0 || height <= 0) return;

        if (DataContext is LoginViewModel vm)
        {
            // Automatic responsive switching:
            // Switch to Mobile when width is less than 940px (not suitable for 2-column desktop)
            // or when aspect ratio is portrait (height > width * 1.05)
            bool shouldBeMobile = width < 940 || (height > width * 1.05);

            if (vm.IsMobileMode != shouldBeMobile)
            {
                vm.IsMobileMode = shouldBeMobile;
            }
        }
    }

    private void TryLoadBackgroundImages()
    {
        LoadImage(BackgroundImage, "mocngu_login_bg.jpg");
        LoadImage(MobileBackgroundImage, "mocngu_login_mobile_bg.jpg");
    }

    private void LoadImage(Image? targetImage, string fileName)
    {
        if (targetImage == null) return;

        // 1. Try application pack URI
        try
        {
            var packUri = new Uri($"pack://application:,,,/Assets/{fileName}", UriKind.Absolute);
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = packUri;
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.EndInit();
            targetImage.Source = bi;
            return;
        }
        catch
        {
            // fallback
        }

        // 2. Try bin output folder
        try
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string localPath = Path.Combine(basePath, "Assets", fileName);
            if (File.Exists(localPath))
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(localPath, UriKind.Absolute);
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                targetImage.Source = bi;
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
            string relPath = Path.GetFullPath($"Assets/{fileName}");
            if (File.Exists(relPath))
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(relPath, UriKind.Absolute);
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                targetImage.Source = bi;
            }
        }
        catch
        {
            // Soft fail
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
            if (MobileLoginPasswordTextBox != null && MobileLoginPasswordTextBox.Text != pb.Password)
            {
                MobileLoginPasswordTextBox.Text = pb.Password;
            }
            if (LoginPasswordBox != null && LoginPasswordBox != pb && LoginPasswordBox.Password != pb.Password)
            {
                LoginPasswordBox.Password = pb.Password;
            }
            if (MobileLoginPasswordBox != null && MobileLoginPasswordBox != pb && MobileLoginPasswordBox.Password != pb.Password)
            {
                MobileLoginPasswordBox.Password = pb.Password;
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
            if (MobileLoginPasswordBox != null && MobileLoginPasswordBox.Password != tb.Text)
            {
                MobileLoginPasswordBox.Password = tb.Text;
            }
            if (LoginPasswordTextBox != null && LoginPasswordTextBox != tb && LoginPasswordTextBox.Text != tb.Text)
            {
                LoginPasswordTextBox.Text = tb.Text;
            }
            if (MobileLoginPasswordTextBox != null && MobileLoginPasswordTextBox != tb && MobileLoginPasswordTextBox.Text != tb.Text)
            {
                MobileLoginPasswordTextBox.Text = tb.Text;
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
