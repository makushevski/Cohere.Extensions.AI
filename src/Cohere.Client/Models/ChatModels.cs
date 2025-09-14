using System.Collections.Generic;

namespace Cohere.Client.Models;

/// <summary>
/// Request schema for Cohere API v2 Chat endpoint.
/// See https://docs.cohere.com/reference/chat (v2 section, streaming supported)
/// </summary>
public class ChatRequest
{
    public string Model { get; set; } = string.Empty;
    public IList<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

    // Optional tool/function calling definitions can be added by implementations
}

public class ChatMessage
{
    public string Role { get; set; } = "user"; // "user" | "system" | "assistant"
    public string Content { get; set; } = string.Empty;
}

public class ChatResponse
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class ChatStreamEvent
{
    public string? Type { get; set; }
    public string? Delta { get; set; }
}
