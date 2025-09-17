using System.Collections.Generic;

namespace Cohere.Client.Models.V1;

public class ChatRequestV1
{
    public string Model { get; set; } = string.Empty;

    // Для MEAI достаточно одного из этих полей:
    public string? Message { get; set; }                  // последний user-текст
    public IList<ChatMessageV1>? Messages { get; set; }   // необязательная история

    public bool? Stream { get; set; }                     // true => SSE
}

public class ChatMessageV1 : ChatMessageBase
{

}

public class ChatResponseV1
{
    public string Text { get; set; } = string.Empty;      // MEAI читает только Text
}

// Минимальное событие стриминга для fallback в SseStreamReader/CohereChatClient
public class ChatStreamEventV1 : ITextDelta
{
    public string? Delta { get; set; }                    // инкрементальный чанк текста
}
