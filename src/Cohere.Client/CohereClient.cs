using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Cohere.Client.Abstractions;
using Cohere.Client.Configuration;
using Cohere.Client.Helpers;
using Cohere.Client.Models;
using Cohere.Client.Models.V1;

namespace Cohere.Client;

public class CohereClient : ICohereClient
{
    private HttpHelpers httpHelpers;
    private bool disposed = false;

    public CohereClient(string apiKey, HttpClient? httpClient = null, Uri? baseUri = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API key must be provided", nameof(apiKey));

        httpHelpers = new HttpHelpers(httpClient ?? new HttpClient(), baseUri ?? new Uri(Constants.CohereApiUri), httpClient is null);

        /*if (!this.httpClient.DefaultRequestHeaders.Contains("Authorization"))
            this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);*/
    }

    public Task<ChatResponseV1> ChatV1Async(ChatRequestV1 request, CancellationToken cancellationToken = default)
    {
        return httpHelpers.PostJsonAsync<ChatRequestV1, ChatResponseV1>("v1/chat", request, cancellationToken);
    }

    public async IAsyncEnumerable<ChatStreamEventV1> ChatStreamV1Async(ChatRequestV1 request, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var e in httpHelpers.PostSseAsync<ChatRequestV1, ChatStreamEventV1>("v1/chat", request, ct))
            yield return e;
    }

    public Task<ChatResponseV2> ChatV2Async(ChatRequestV2 requestV2, CancellationToken ct = default)
    {
        return httpHelpers.PostJsonAsync<ChatRequestV2, ChatResponseV2>("v2/chat", requestV2, ct);
    }

    public async IAsyncEnumerable<ChatStreamEventV2> ChatStreamV2Async(ChatRequestV2 requestV2, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var e in httpHelpers.PostSseAsync<ChatRequestV2, ChatStreamEventV2>("v2/chat", requestV2, ct))
            yield return e;
    }

    public void Dispose()
    {
        if (!disposed)
        {
            httpHelpers.Dispose();
            disposed = true;
        }

    }
}
