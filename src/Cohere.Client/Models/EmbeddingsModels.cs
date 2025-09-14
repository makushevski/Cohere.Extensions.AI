using System.Collections.Generic;

namespace Cohere.Client.Models;

/// <summary>
/// Request schema for Cohere API v2 Embeddings endpoint.
/// See https://docs.cohere.com/reference/embeddings (v2)
/// </summary>
public class EmbeddingsRequest
{
    public string Model { get; set; } = string.Empty;
    public IList<string> Input { get; set; } = new List<string>();
}

public class Embedding
{
    public IList<float> Vector { get; set; } = new List<float>();
}

public class EmbeddingsResponse
{
    public IList<Embedding> Data { get; set; } = new List<Embedding>();
}
