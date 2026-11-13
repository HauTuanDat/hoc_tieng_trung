using System.Threading.Tasks;

namespace QuizApp.Services;

public class PronunciationResult
{
    public int Score { get; set; }
    public string ScoreGrade { get; set; } = "Chưa chấm";
    public string FeedbackMessage { get; set; } = "";
    public string RecognizedWord { get; set; } = "";
    public string RecognizedPinyin { get; set; } = "";
    public int StarCount { get; set; } = 5;
    public string ScoreHexColor { get; set; } = "#208B58";
    public bool IsAccurate { get; set; }
}

public interface IChinesePronunciationAiService
{
    Task<PronunciationResult> EvaluatePronunciationAsync(string wavFilePath, string targetHanzi, string targetPinyin, string targetMeaning);
}

