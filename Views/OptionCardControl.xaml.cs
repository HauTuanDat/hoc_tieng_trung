using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuizApp.Views;

public partial class OptionCardControl : UserControl
{
    public static readonly DependencyProperty SelectCommandProperty =
        DependencyProperty.Register(
            nameof(SelectCommand),
            typeof(ICommand),
            typeof(OptionCardControl),
            new PropertyMetadata(null));

    public ICommand? SelectCommand
    {
        get => (ICommand?)GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }

    public OptionCardControl()
    {
        InitializeComponent();
    }
}
