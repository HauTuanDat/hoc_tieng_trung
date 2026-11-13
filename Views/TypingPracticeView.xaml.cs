using System.Windows.Controls;
using System.Windows.Input;
using QuizApp.ViewModels;

namespace QuizApp.Views;

public partial class TypingPracticeView : UserControl
{
    public TypingPracticeView()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            WordInputBox.Focus();
        };
    }

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is TypingPracticeViewModel vm)
            {
                vm.ExecuteCheckOrNext();
                if (vm.HasError)
                {
                    WordInputBox.SelectAll();
                    WordInputBox.Focus();
                }
                else if (vm.IsCorrect)
                {
                    // Focus word input box for next word
                    WordInputBox.Focus();
                }
            }
            e.Handled = true;
        }
    }

    private void JumpBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is TypingPracticeViewModel vm)
            {
                vm.ExecuteJumpToWord();
                WordInputBox.Focus();
            }
            e.Handled = true;
        }
    }
}
