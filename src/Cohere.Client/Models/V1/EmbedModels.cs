using System.Collections.Generic;

namespace Cohere.Client.Models.V1;

/// <summary>
/// Request schema for Cohere API v1 Embed endpoint (/v1/embed).
/// See https://docs.cohere.com/reference/embed
/// </summary>
public class EmbedRequestV1
{
    public string Model { get; set; } = string.Empty;
    public IList<string> Inputs { get; set; } = new List<string>();
}

public class EmbeddingV1
{
    public IList<float> Values { get; set; } = new List<float>();
}

public class EmbedResponseV1
{
    public IList<EmbeddingV1> Embeddings { get; set; } = new List<EmbeddingV1>();
}

