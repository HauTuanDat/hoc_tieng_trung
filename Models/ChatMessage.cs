namespace QuizApp.Models;

public class ChatMessage
{
    public string Sender { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsUser => Sender == "User";
    public string TimeFormatted => Timestamp.ToString("HH:mm");
}