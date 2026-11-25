using System.Collections.ObjectModel;
using System.Windows.Input;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.ViewModels;

public class ChatViewModel : ViewModelBase
{
    private readonly ChatService _chatService = new();
    private string _inputText = string.Empty;
    private bool _isBusy;

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    public string InputText
    {
        get => _inputText;
        set => SetProperty(ref _inputText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public ICommand SendCommand { get; }
    public ICommand ClearChatCommand { get; }
    public ICommand SendSuggestionCommand { get; }

    public ChatViewModel()
    {
        SendCommand = new RelayCommand(async () => await SendMessageAsync(InputText));
        ClearChatCommand = new RelayCommand(ResetConversation);
        SendSuggestionCommand = new RelayCommand(async param =>
        {
            if (param is string text && !string.IsNullOrWhiteSpace(text))
            {
                await SendMessageAsync(text);
            }
        });

        ResetConversation();
    }

    private void ResetConversation()
    {
        Messages.Clear();
        Messages.Add(new ChatMessage
        {
            Sender = "AI",
            Content = "Xin chào! Mình là Trợ lý AI Mộc Ngữ 🐼\nBạn có thể hỏi mình nghĩa từ vựng HSK, cách đặt câu, phân biệt từ gần nghĩa hoặc luyện hội thoại tiếng Trung nhé!"
        });
    }

    private async Task SendMessageAsync(string text)
    {
        if (IsBusy || string.IsNullOrWhiteSpace(text)) return;

        string prompt = text.Trim();
        if (prompt == InputText.Trim())
        {
            InputText = string.Empty;
        }

        var historySnapshot = Messages.ToList();
        Messages.Add(new ChatMessage { Sender = "User", Content = prompt });

        IsBusy = true;
        var typingMsg = new ChatMessage { Sender = "AI", Content = "Đang suy nghĩ..." };
        Messages.Add(typingMsg);

        string reply = await _chatService.SendMessageAsync(historySnapshot, prompt);

        Messages.Remove(typingMsg);
        Messages.Add(new ChatMessage { Sender = "AI", Content = reply });
        IsBusy = false;
    }
}