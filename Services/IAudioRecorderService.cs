namespace QuizApp.Services;

public interface IAudioRecorderService
{
    bool IsRecording { get; }
    double InputLevel { get; }
    void StartRecording();
    Task<string> StopRecordingAsync();
    void CancelRecording();
    void PlayRecording(string wavFilePath);
    void StopPlayback();
}

