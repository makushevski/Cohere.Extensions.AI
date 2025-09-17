using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cohere.Client.Models;
using Cohere.Client.Models.V1;
using Cohere.Client.Models.V2;

namespace Cohere.Client.Abstractions;

/// <summary>
///     Cohere API client surface covering API v1 and v2 endpoints.
///     Only method contracts are defined here; implementations live elsewhere.
///     Refer to https://docs.cohere.com for request/response semantics.
/// </summary>
public interface ICohereClient : IDisposable
{
    Task<ChatResponseV1> ChatV1Async(ChatRequestV1 request, CancellationToken ct = default);

    IAsyncEnumerable<ChatStreamEventV1> ChatStreamV1Async(ChatRequestV1 request, CancellationToken ct = default);

    Task<ChatResponseV2> ChatV2Async(ChatRequestV2 requestV2, CancellationToken ct = default);

    IAsyncEnumerable<ChatStreamEventV2> ChatStreamV2Async(ChatRequestV2 requestV2, CancellationToken ct = default);
}
