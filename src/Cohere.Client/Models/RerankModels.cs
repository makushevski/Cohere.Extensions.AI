using System.Collections.Generic;

namespace Cohere.Client.Models;

/// <summary>
/// Request schema for Cohere API v2 Rerank endpoint.
/// See https://docs.cohere.com/reference/rerank (v2)
/// </summary>
public class RerankRequest
{
    public string Model { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public IList<string> Documents { get; set; } = new List<string>();
    public int? TopN { get; set; }
}

public class RerankResult
{
    public int Index { get; set; }
    public double RelevanceScore { get; set; }
    public string? Document { get; set; }
}

public class RerankResponse
{
    public IList<RerankResult> Results { get; set; } = new List<RerankResult>();
}
