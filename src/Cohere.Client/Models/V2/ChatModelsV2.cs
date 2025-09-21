using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cohere.Client.Models.V2;

public class ChatRequestV2
{
    public string Model { get; set; } = string.Empty;
    public IList<ChatMessageV2> Messages { get; set; } = new List<ChatMessageV2>();
    public bool Stream { get; set; }
}

public class ChatMessageV2 : ChatMessageBase
{
}

public class ChatResponseV2
{
    public ChatMessageResponseV2? Message { get; set; }

    [JsonIgnore]
    public string Text => Message?.Text ?? string.Empty;
}

public class ChatMessageResponseV2
{
    public string Role { get; set; } = "assistant";
    public IList<ChatContentBlockV2> Content { get; set; } = new List<ChatContentBlockV2>();

    [JsonIgnore]
    public string Text => string.Concat(Content
        .Where(b => b.IsTextLike)
        .Select(b => b.Text)
        .Where(s => !string.IsNullOrEmpty(s)));
}

public class ChatContentBlockV2
{
    public string Type { get; set; } = string.Empty; // e.g. "output_text", "input_text", etc.
    public string? Text { get; set; }

    [JsonIgnore]
    public bool IsTextLike =>
        Type.Contains("text", System.StringComparison.OrdinalIgnoreCase);
}

public class ChatStreamEventV2 : ITextDelta
{
    public string? Type { get; set; }
    public int? Index { get; set; }

    [JsonPropertyName("delta")]
    public JsonElement RawDelta { get; set; }

    [JsonIgnore]
    public string? Delta { get; set; }
}

