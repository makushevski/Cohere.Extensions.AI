namespace Cohere.Client.Models;

public abstract class ChatMessageBase
{
    public string Role { get; set; } = "user"; // "user" | "system" | "assistant"
    public string Content { get; set; } = string.Empty;
}

public interface ITextDelta
{
    string? Delta { get; set; }
}
