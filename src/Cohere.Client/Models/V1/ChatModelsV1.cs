using System.Collections.Generic;

namespace Cohere.Client.Models.V1;

public class ChatRequestV1
{
    public string Model { get; set; } = string.Empty;

    public string? Message { get; set; }
    public IList<ChatMessageV1>? Messages { get; set; }

    public bool? Stream { get; set; }
}

public class ChatMessageV1 : ChatMessageBase
{

}

public class ChatResponseV1
{
    public string Text { get; set; } = string.Empty;
}

public class ChatStreamEventV1 : ITextDelta
{
    public string? Delta { get; set; }
}
