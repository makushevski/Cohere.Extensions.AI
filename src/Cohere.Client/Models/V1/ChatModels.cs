using System.Collections.Generic;

namespace Cohere.Client.Models.V1;

/// <summary>
/// Request schema for Cohere API v1 Chat endpoint (/v1/chat).
/// See https://docs.cohere.com/reference/chat
/// </summary>
public class ChatRequestV1
{
    public string Model { get; set; } = string.Empty;

    // For v1 chat, top-level 'message' is required; 'messages'/'chat_history' are optional and provider-specific.
    public string? Message { get; set; }

    public IList<ChatMessageV1>? Messages { get; set; }

    public bool? Stream { get; set; }
}

/// <summary>
/// Chat message for v1 chat API.
/// </summary>
public class ChatMessageV1
{
    public string Role { get; set; } = "user"; // "user" | "system" | "assistant"
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Response for v1 chat.
/// </summary>
public class ChatResponseV1
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Stream event for v1 chat streaming.
/// Carries incremental text deltas; implementations may add event types.
/// </summary>
public class ChatStreamEventV1
{
    public string? Type { get; set; } // e.g., "text-delta", "message-end"
    public string? Delta { get; set; }
}
