using System.Collections.Generic;

namespace Cohere.Client.Models;

/// <summary>
///     Request schema for Cohere API v2 Chat endpoint.
///     See https://docs.cohere.com/reference/chat (v2 section, streaming supported)
/// </summary>
public class ChatRequestV2
{
    public string Model { get; set; } = string.Empty;
    public IList<ChatMessageV2> Messages { get; set; } = new List<ChatMessageV2>();
}

public class ChatMessageV2
{
    public string Role { get; set; } = "user"; // "user" | "system" | "assistant"
    public string Content { get; set; } = string.Empty;
}

public class ChatResponseV2
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class ChatStreamEventV2
{
    public string? Type { get; set; }
    public string? Delta { get; set; }
}
