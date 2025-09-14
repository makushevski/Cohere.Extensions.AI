using System.Collections.Generic;

namespace Cohere.Client.Models.V1;

/// <summary>
/// Request schema for Cohere API v1 Rerank endpoint (/v1/rerank).
/// See https://docs.cohere.com/reference/rerank
/// </summary>
public class RerankRequestV1
{
    public string Model { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public IList<string> Documents { get; set; } = new List<string>();
    public int? TopN { get; set; }
}

public class RerankResultV1
{
    public int Index { get; set; }
    public double RelevanceScore { get; set; }
    public string? Document { get; set; }
}

public class RerankResponseV1
{
    public IList<RerankResultV1> Results { get; set; } = new List<RerankResultV1>();
}

