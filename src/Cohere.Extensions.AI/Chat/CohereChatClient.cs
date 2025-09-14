using System.Runtime.CompilerServices;
using Cohere.Client;
using Cohere.Client.Abstractions;
using Cohere.Client.Models;
using Microsoft.Extensions.AI;
using CohereChatRequestV1 = Cohere.Client.Models.V1.ChatRequestV1;
using CohereChatMessageV1 = Cohere.Client.Models.V1.ChatMessageV1;

namespace Cohere.Extensions.AI.Chat;

/// <summary>
///     Microsoft.Extensions.AI chat client backed by Cohere Chat API (v2 by default).
/// </summary>
public sealed class CohereChatClient : IChatClient
{
    private readonly ICohereClient cohere;
    private readonly CohereChatClientOptions options;

    public CohereChatClient(ICohereClient cohereClient, CohereChatClientOptions? options = null)
    {
        cohere = cohereClient ?? throw new ArgumentNullException(nameof(cohereClient));
        this.options = options ?? new CohereChatClientOptions();
    }


    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options,
        CancellationToken cancellationToken)
    {
        options ??= new ChatOptions();
        var modelId = options.ModelId ?? this.options.ModelId ??
            throw new InvalidOperationException(
                "ModelId must be specified either in ChatOptions.ModelId or CohereChatClientOptions.ModelId.");
        if (this.options.UseV1)
        {
            var reqV1 = new CohereChatRequestV1
            {
                Model = modelId,
                Stream = false
            };

            reqV1.Message = GetLastUserText(messages) ?? GetAllText(messages);

            var respV1 = await cohere.ChatV1Async(reqV1, cancellationToken).ConfigureAwait(false);

            var msgV1 = new ChatMessage(
                ChatRole.Assistant,
                new List<AIContent> { new TextContent(respV1.Text) })
            {
                RawRepresentation = respV1
            };

            return new ChatResponse(new List<ChatMessage> { msgV1 })
            {
                ModelId = modelId,
                RawRepresentation = respV1
            };
        }

        var req = new ChatRequestV2
        {
            Model = modelId,
            Messages = Map(messages)
        };

        var resp = await cohere.ChatV2Async(req, cancellationToken).ConfigureAwait(false);

        var msg = new ChatMessage(
            ChatRole.Assistant,
            new List<AIContent> { new TextContent(resp.Text) })
        {
            RawRepresentation = resp
        };

        return new ChatResponse(new List<ChatMessage> { msg })
        {
            ModelId = modelId,
            RawRepresentation = resp
        };
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        options ??= new ChatOptions();
        var modelId = options.ModelId ?? this.options.ModelId ??
            throw new InvalidOperationException(
                "ModelId must be specified either in ChatOptions.ModelId or CohereChatClientOptions.ModelId.");
        if (this.options.UseV1)
        {
            var reqV1 = new CohereChatRequestV1
            {
                Model = modelId,
                Stream = true
            };

            reqV1.Message = GetLastUserText(messages) ?? GetAllText(messages);

            var emitted = false;
            await foreach (var evt in cohere.ChatStreamV1Async(reqV1, cancellationToken))
            {
                if (string.IsNullOrEmpty(evt.Delta)) continue;
                emitted = true;
                yield return new ChatResponseUpdate(ChatRole.Assistant, evt.Delta);
            }

            if (!emitted)
            {
                // Fallback to non-streaming if no events
                reqV1.Stream = false;
                var respV1 = await cohere.ChatV1Async(reqV1, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(respV1.Text))
                    yield return new ChatResponseUpdate(ChatRole.Assistant, respV1.Text);
            }
        }
        else
        {
            var req = new ChatRequestV2
            {
                Model = modelId,
                Messages = Map(messages)
            };

            var emitted = false;
            await foreach (var evt in cohere.ChatStreamV2Async(req, cancellationToken))
            {
                if (string.IsNullOrEmpty(evt.Delta)) continue;
                emitted = true;
                yield return new ChatResponseUpdate(ChatRole.Assistant, evt.Delta);
            }

            if (!emitted)
            {
                var resp = await cohere.ChatV2Async(req, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(resp.Text))
                    yield return new ChatResponseUpdate(ChatRole.Assistant, resp.Text);
            }
        }
    }

    public object? GetService(Type serviceType, object? serviceKey) =>
        serviceType == typeof(ICohereClient) ? cohere : null;

    public void Dispose() => cohere.Dispose();

    private static IList<ChatMessageV2> Map(IEnumerable<ChatMessage> messages)
    {
        var list = new List<ChatMessageV2>();
        foreach (var item in messages)
        {
            if (item.Role == ChatRole.Tool) continue; // tool calling can be added in a future iteration

            var content = item.Text;
            var role = item.Role == ChatRole.System ? "system"
                : item.Role == ChatRole.Assistant ? "assistant" : "user";

            list.Add(new ChatMessageV2 { Role = role, Content = content });
        }

        return list;
    }

    private static IList<CohereChatMessageV1> MapV1(IEnumerable<ChatMessage> messages)
    {
        var list = new List<CohereChatMessageV1>();
        foreach (var item in messages)
        {
            if (item.Role == ChatRole.Tool) continue;

            var content = item.Text;
            var role = item.Role == ChatRole.System ? "system"
                : item.Role == ChatRole.Assistant ? "assistant" : "user";

            list.Add(new CohereChatMessageV1 { Role = role, Content = content });
        }

        return list;
    }

    private static string? GetLastUserText(IEnumerable<ChatMessage> messages)
        => messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text;

    private static string GetAllText(IEnumerable<ChatMessage> messages)
        => string.Concat(messages.Select(m => m.Text));
}
