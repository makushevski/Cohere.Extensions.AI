using Cohere.Client;
using Cohere.Client.Abstractions;
using Cohere.Client.Models;
using Cohere.Client.Models.V1;

namespace Cohere.Extensions.AI.Tests.Fakes;

internal sealed class FakeCohereClient : ICohereClient
{
    public Func<ChatRequestV2, Task<ChatResponseV2>>? OnChatAsync { get; set; }
    public Func<ChatRequestV2, IAsyncEnumerable<ChatStreamEventV2>>? OnChatStreamAsync { get; set; }

    public Func<ChatRequestV1, Task<ChatResponseV1>>? OnChatV1Async { get; set; }
    public Func<ChatRequestV1, IAsyncEnumerable<ChatStreamEventV1>>? OnChatStreamV1Async { get; set; }


    // V1 chat
    public Task<ChatResponseV1> ChatV1Async(ChatRequestV1 request, CancellationToken cancellationToken = default)
        => OnChatV1Async is null ? throw new NotImplementedException() : OnChatV1Async(request);

    public IAsyncEnumerable<ChatStreamEventV1> ChatStreamV1Async(ChatRequestV1 request,
        CancellationToken cancellationToken = default)
        => OnChatStreamV1Async is null ? throw new NotImplementedException() : OnChatStreamV1Async(request);

    // V2 chat
    public Task<ChatResponseV2> ChatV2Async(ChatRequestV2 requestV2, CancellationToken cancellationToken = default)
        => OnChatAsync is null ? throw new NotImplementedException() : OnChatAsync(requestV2);

    public IAsyncEnumerable<ChatStreamEventV2> ChatStreamV2Async(ChatRequestV2 requestV2,
        CancellationToken cancellationToken = default)
        => OnChatStreamAsync is null ? throw new NotImplementedException() : OnChatStreamAsync(requestV2);

    public void Dispose()
    {

    }
}
