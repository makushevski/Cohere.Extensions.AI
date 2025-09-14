using Cohere.Client;
using Cohere.Client.Models;
using Cohere.Client.Models.V1;

namespace Cohere.Extensions.AI.Tests.Fakes;

internal sealed class FakeCohereClient : ICohereClient
{
    public Func<ChatRequest, Task<ChatResponse>>? OnChatAsync { get; set; }
    public Func<ChatRequest, IAsyncEnumerable<ChatStreamEvent>>? OnChatStreamAsync { get; set; }

    public Func<ChatRequestV1, Task<ChatResponseV1>>? OnChatV1Async { get; set; }
    public Func<ChatRequestV1, IAsyncEnumerable<ChatStreamEventV1>>? OnChatStreamV1Async { get; set; }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    // V1 - methods we don't use in tests
    public Task<GenerateResponseV1> GenerateV1Async(GenerateRequestV1 request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
    public Task<EmbedResponseV1> EmbedV1Async(EmbedRequestV1 request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
    public Task<RerankResponseV1> RerankV1Async(RerankRequestV1 request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
    public Task<ClassifyResponseV1> ClassifyV1Async(ClassifyRequestV1 request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
    public Task<TokenizeResponseV1> TokenizeV1Async(TokenizeRequestV1 request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
    public Task<DetokenizeResponseV1> DetokenizeV1Async(DetokenizeRequestV1 request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    // V1 chat
    public Task<ChatResponseV1> ChatV1Async(ChatRequestV1 request, CancellationToken cancellationToken = default)
        => OnChatV1Async is null ? throw new NotImplementedException() : OnChatV1Async(request);
    public IAsyncEnumerable<ChatStreamEventV1> ChatStreamV1Async(ChatRequestV1 request, CancellationToken cancellationToken = default)
        => OnChatStreamV1Async is null ? throw new NotImplementedException() : OnChatStreamV1Async(request);

    // V2 chat
    public Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
        => OnChatAsync is null ? throw new NotImplementedException() : OnChatAsync(request);
    public IAsyncEnumerable<ChatStreamEvent> ChatStreamAsync(ChatRequest request, CancellationToken cancellationToken = default)
        => OnChatStreamAsync is null ? throw new NotImplementedException() : OnChatStreamAsync(request);

    // V2 - methods we don't use in tests
    public Task<EmbeddingsResponse> EmbedAsync(EmbeddingsRequest request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
    public Task<RerankResponse> RerankAsync(RerankRequest request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}

