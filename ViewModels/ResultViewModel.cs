using System.Windows.Input;
using QuizApp.Models;

namespace QuizApp.ViewModels;

public class ResultViewModel : ViewModelBase
{
    private readonly QuizResult _result;
    private readonly Action _onRetryBatch;
    private readonly Action<string, string> _onContinueNextBatch;
    private readonly Action _onBackToHome;

    public ResultViewModel(
        QuizResult result,
        Action onRetryBatch,
        Action<string, string> onContinueNextBatch,
        Action onBackToHome)
    {
        _result = result;
        _onRetryBatch = onRetryBatch;
        _onContinueNextBatch = onContinueNextBatch;
        _onBackToHome = onBackToHome;

        RetryBatchCommand = new RelayCommand(_onRetryBatch);
        ContinueNextBatchCommand = new RelayCommand(
            () => _onContinueNextBatch(_result.Level, _result.Mode),
            () => IsPassing
        );
        BackToHomeCommand = new RelayCommand(_onBackToHome);
    }

    public int TotalQuestions => _result.TotalQuestions;
    public int CorrectCount => _result.CorrectCount;
    public int IncorrectCount => _result.IncorrectCount;
    public double ScorePercentage => Math.Round(_result.ScorePercentage, 1);
    public string DurationText => _result.Duration.ToString(@"mm\:ss");
    public string GradeEvaluation => _result.GradeEvaluation;
    public List<UserAnswer> UserAnswers => _result.UserAnswers;
    public string Level => _result.Level;

    public bool IsPassing => _result.IsPassing;
    public bool MustRetry => _result.MustRetry;

    public string ScoreSummaryText => $"{CorrectCount} / {TotalQuestions} câu đúng ({ScorePercentage}%)";

    public string ResultTitle => IsPassing
        ? "🎉 Đạt chuẩn thành công! Đã lưu 20 từ vào CSDL"
        : "⚠️ Chưa đạt 70%! Bắt buộc phải thi lại đợt này";

    public string RequirementNotice => IsPassing
        ? $"Bạn đã đạt {ScorePercentage}% (≥ 70%). Toàn bộ 20 từ này đã được lưu vào tiến độ học trên CSDL localhost. Bạn muốn tiếp tục 20 từ tiếp theo hay dừng lại?"
        : $"Điểm của bạn là {ScorePercentage}% (< 70%). Quy định yêu cầu tối thiểu đạt 70% (14/20 câu). Bạn bắt buộc phải thi lại 20 từ này để ghi nhớ chắc chắn trước khi chuyển sang từ mới!";

    public ICommand RetryBatchCommand { get; }
    public ICommand ContinueNextBatchCommand { get; }
    public ICommand BackToHomeCommand { get; }
}
