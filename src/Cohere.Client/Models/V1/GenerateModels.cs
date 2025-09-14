using System.Collections.Generic;

namespace Cohere.Client.Models.V1;

/// <summary>
/// Request schema for Cohere API v1 Generate endpoint (/v1/generate).
/// See https://docs.cohere.com/reference/generate
/// </summary>
public class GenerateRequestV1
{
    public string Model { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public int? MaxTokens { get; set; }
}

public class GenerationV1
{
    public string Text { get; set; } = string.Empty;
}

public class GenerateResponseV1
{
    public IList<GenerationV1> Generations { get; set; } = new List<GenerationV1>();
}