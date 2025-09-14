using Cohere.Client.Models.V1;
using Cohere.Client.Models;

namespace Cohere.Client;

/// <summary>
/// Cohere API client surface covering API v1 and v2 endpoints.
/// Only method contracts are defined here; implementations live elsewhere.
/// Refer to https://docs.cohere.com for request/response semantics.
/// </summary>
public interface ICohereClient : IAsyncDisposable
{
    // =====================
    // API v1
    // =====================

    // Text generation (Generate)
    Task<GenerateResponseV1> GenerateV1Async(GenerateRequestV1 request, CancellationToken cancellationToken = default);

    // Chat
    Task<ChatResponseV1> ChatV1Async(ChatRequestV1 request, CancellationToken cancellationToken = default);

    // Chat streaming (server-sent events / token deltas)
    IAsyncEnumerable<ChatStreamEventV1> ChatStreamV1Async(ChatRequestV1 request, CancellationToken cancellationToken = default);

    // Embeddings
    Task<EmbedResponseV1> EmbedV1Async(EmbedRequestV1 request, CancellationToken cancellationToken = default);

    // Rerank
    Task<RerankResponseV1> RerankV1Async(RerankRequestV1 request, CancellationToken cancellationToken = default);

    // Classify (legacy)
    Task<ClassifyResponseV1> ClassifyV1Async(ClassifyRequestV1 request, CancellationToken cancellationToken = default);

    // Tokenization utils
    Task<TokenizeResponseV1> TokenizeV1Async(TokenizeRequestV1 request, CancellationToken cancellationToken = default);
    
    Task<DetokenizeResponseV1> DetokenizeV1Async(DetokenizeRequestV1 request, CancellationToken cancellationToken = default);

    // =====================
    // API v2
    // =====================

    // Chat
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default);

    // Chat streaming
    IAsyncEnumerable<ChatStreamEvent> ChatStreamAsync(ChatRequest request, CancellationToken cancellationToken = default);

    // Embeddings
    Task<EmbeddingsResponse> EmbedAsync(EmbeddingsRequest request, CancellationToken cancellationToken = default);

    // Rerank
    Task<RerankResponse> RerankAsync(RerankRequest request, CancellationToken cancellationToken = default);
}
