using System.Windows.Input;
using QuizApp.Services;

namespace QuizApp.ViewModels;

public class PracticeCenterViewModel : ViewModelBase
{
    private readonly IHskService _hskService;
    private readonly Action<string> _onNavigateToSkill;
    private readonly Action _onGoToCourses;

    public PracticeCenterViewModel(IHskService hskService, Action<string> onNavigateToSkill, Action onGoToCourses)
    {
        _hskService = hskService;
        _onNavigateToSkill = onNavigateToSkill;
        _onGoToCourses = onGoToCourses;

        OpenSkillCommand = new RelayCommand(param =>
        {
            string skill = param as string ?? "Vocab";
            _onNavigateToSkill(skill);
        });

        GoToCoursesCommand = new RelayCommand(_onGoToCourses);
    }

    public ICommand OpenSkillCommand { get; }
    public ICommand GoToCoursesCommand { get; }
}

