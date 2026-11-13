using System;

namespace QuizApp.Services;

public interface IAudioRecorderService
{
    bool IsRecording { get; }
    void StartRecording();
    string StopRecording();
    void PlayRecording(string wavFilePath);
    void StopPlayback();
    double CalculateRms(string wavFilePath);
}

