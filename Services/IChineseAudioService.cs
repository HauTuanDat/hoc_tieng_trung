namespace QuizApp.Services;

public interface IChineseAudioService
{
    Task PlayAsync(string text, string? voice = null, string? rate = null);
    void Stop();
    bool IsMuted { get; set; }
    string SelectedVoice { get; set; }
    string SelectedSpeed { get; set; }
}
