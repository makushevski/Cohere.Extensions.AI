using System.Collections.Generic;

namespace Cohere.Client.Models.V1;

/// <summary>
/// Request schema for Cohere API v1 Tokenize endpoint (/v1/tokenize).
/// See https://docs.cohere.com/reference/tokenize
/// </summary>
public class TokenizeRequestV1
{
    public string Text { get; set; } = string.Empty;
}

public class TokenizeResponseV1
{
    public IList<int> Tokens { get; set; } = new List<int>();
}

/// <summary>
/// Request schema for Cohere API v1 Detokenize endpoint (/v1/detokenize).
/// See https://docs.cohere.com/reference/detokenize
/// </summary>
public class DetokenizeRequestV1
{
    public IList<int> Tokens { get; set; } = new List<int>();
}

public class DetokenizeResponseV1
{
    public string Text { get; set; } = string.Empty;
}

