namespace Cohere.Extensions.AI.Chat;

/// <summary>
///     Options for <see cref="CohereChatClient" />.
/// </summary>
public sealed class CohereChatClientOptions
{
    /// <summary>
    ///     Default model identifier for Cohere, e.g. "command-r-plus".
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    ///     Use Cohere API v1 Chat endpoints instead of v2. Default: false (v2).
    /// </summary>
    public bool UseV1 { get; set; }
}
