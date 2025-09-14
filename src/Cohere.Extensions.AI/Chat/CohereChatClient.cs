using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cohere.Client;
using Cohere.Client.Models;
using Microsoft.Extensions.AI;
using CohereChatRequest = Cohere.Client.Models.ChatRequest;
using CohereChatMessage = Cohere.Client.Models.ChatMessage;
using CohereChatRequestV1 = Cohere.Client.Models.V1.ChatRequestV1;
using CohereChatMessageV1 = Cohere.Client.Models.V1.ChatMessageV1;

namespace Cohere.Extensions.AI.Chat;

/// <summary>
/// Microsoft.Extensions.AI chat client backed by Cohere Chat API (v2 by default).
/// </summary>
public sealed class CohereChatClient : IChatClient, IAsyncDisposable
{
    private readonly ICohereClient _cohere;
    private readonly CohereChatClientOptions _options;

    public CohereChatClient(ICohereClient cohereClient, CohereChatClientOptions? options = null)
    {
        _cohere = cohereClient ?? throw new ArgumentNullException(nameof(cohereClient));
        _options = options ?? new CohereChatClientOptions();
    }

    public async Task<Microsoft.Extensions.AI.ChatResponse> GetResponseAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ChatOptions? options, CancellationToken cancellationToken)
    {
        options ??= new ChatOptions();
        var modelId = options.ModelId ?? _options.ModelId ?? throw new InvalidOperationException("ModelId must be specified either in ChatOptions.ModelId or CohereChatClientOptions.ModelId.");
        if (_options.UseV1)
        {
            var reqV1 = new CohereChatRequestV1
            {
                Model = modelId,
                Stream = false
            };

            reqV1.Message = GetLastUserText(messages) ?? GetAllText(messages);

            var respV1 = await _cohere.ChatV1Async(reqV1, cancellationToken).ConfigureAwait(false);

            var msgV1 = new Microsoft.Extensions.AI.ChatMessage(
                ChatRole.Assistant,
                new List<AIContent> { new TextContent(respV1.Text) })
            {
                RawRepresentation = respV1
            };

            return new Microsoft.Extensions.AI.ChatResponse(new List<Microsoft.Extensions.AI.ChatMessage> { msgV1 })
            {
                ModelId = modelId,
                RawRepresentation = respV1
            };
        }
        else
        {
            var req = new CohereChatRequest
            {
                Model = modelId,
                Messages = Map(messages)
            };

            var resp = await _cohere.ChatAsync(req, cancellationToken).ConfigureAwait(false);

            var msg = new Microsoft.Extensions.AI.ChatMessage(
                ChatRole.Assistant,
                new List<AIContent> { new TextContent(resp.Text) })
            {
                RawRepresentation = resp
            };

            return new Microsoft.Extensions.AI.ChatResponse(new List<Microsoft.Extensions.AI.ChatMessage> { msg })
            {
                ModelId = modelId,
                RawRepresentation = resp
            };
        }
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ChatOptions? options, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        options ??= new ChatOptions();
        var modelId = options.ModelId ?? _options.ModelId ?? throw new InvalidOperationException("ModelId must be specified either in ChatOptions.ModelId or CohereChatClientOptions.ModelId.");
        if (_options.UseV1)
        {
            var reqV1 = new CohereChatRequestV1
            {
                Model = modelId,
                Stream = true
            };

            reqV1.Message = GetLastUserText(messages) ?? GetAllText(messages);

            var emitted = false;
            await foreach (var evt in _cohere.ChatStreamV1Async(reqV1, cancellationToken))
            {
                if (string.IsNullOrEmpty(evt.Delta))
                {
                    continue;
                }
                emitted = true;
                yield return new ChatResponseUpdate(ChatRole.Assistant, evt.Delta);
            }

            if (!emitted)
            {
                // Fallback to non-streaming if no events
                reqV1.Stream = false;
                var respV1 = await _cohere.ChatV1Async(reqV1, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(respV1.Text))
                {
                    yield return new ChatResponseUpdate(ChatRole.Assistant, respV1.Text);
                }
            }
        }
        else
        {
            var req = new CohereChatRequest
            {
                Model = modelId,
                Messages = Map(messages)
            };

            var emitted = false;
            await foreach (var evt in _cohere.ChatStreamAsync(req, cancellationToken))
            {
                if (string.IsNullOrEmpty(evt.Delta))
                {
                    continue;
                }
                emitted = true;
                yield return new ChatResponseUpdate(ChatRole.Assistant, evt.Delta);
            }

            if (!emitted)
            {
                var resp = await _cohere.ChatAsync(req, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(resp.Text))
                {
                    yield return new ChatResponseUpdate(ChatRole.Assistant, resp.Text);
                }
            }
        }
    }

    public object? GetService(Type serviceType, object? serviceKey) => serviceType == typeof(ICohereClient) ? _cohere : null;

    private static IList<CohereChatMessage> Map(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages)
    {
        var list = new List<CohereChatMessage>();
        foreach (var item in messages)
        {
            if (item.Role == ChatRole.Tool)
            {
                continue; // tool calling can be added in a future iteration
            }

            var content = item.Text;
            var role = item.Role == ChatRole.System ? "system"
                      : item.Role == ChatRole.Assistant ? "assistant" : "user";

            list.Add(new CohereChatMessage { Role = role, Content = content });
        }
        return list;
    }

    private static IList<CohereChatMessageV1> MapV1(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages)
    {
        var list = new List<CohereChatMessageV1>();
        foreach (var item in messages)
        {
            if (item.Role == ChatRole.Tool)
            {
                continue;
            }

            var content = item.Text;
            var role = item.Role == ChatRole.System ? "system"
                      : item.Role == ChatRole.Assistant ? "assistant" : "user";

            list.Add(new CohereChatMessageV1 { Role = role, Content = content });
        }
        return list;
    }

    private static string? GetLastUserText(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages)
        => messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text;

    private static string GetAllText(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages)
        => string.Concat(messages.Select(m => m.Text));

    public ValueTask DisposeAsync() => _cohere.DisposeAsync();

    public void Dispose()
    {
        // Ensure synchronous dispose path for consumers using IDisposable
        _cohere.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}

/// <summary>
/// Options for <see cref="CohereChatClient"/>.
/// </summary>
public sealed class CohereChatClientOptions
{
    /// <summary>
    /// Default model identifier for Cohere, e.g. "command-r-plus".
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// Use Cohere API v1 Chat endpoints instead of v2. Default: false (v2).
    /// </summary>
    public bool UseV1 { get; set; }
}
