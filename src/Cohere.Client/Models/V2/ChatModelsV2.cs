using System.Collections.Generic;

namespace Cohere.Client.Models.V2;

public class ChatRequestV2
{
    public string Model { get; set; } = string.Empty;
    public IList<ChatMessageV2> Messages { get; set; } = new List<ChatMessageV2>();
}

public class ChatMessageV2 : ChatMessageBase
{
}

public class ChatResponseV2
{
    public string Text { get; set; } = string.Empty;      // MEAI читает только Text
}

public class ChatStreamEventV2 : ITextDelta
{
    public string? Delta { get; set; }
}
