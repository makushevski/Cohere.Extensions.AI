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
using Cohere.Client.Models;
using Cohere.Client.Models.V1;

namespace Cohere.Client;

public class CohereClient : ICohereClient
{
    private readonly Uri baseUri;
    private readonly bool disposeHttpClient;
    private readonly HttpClient httpClient;
    private readonly JsonSerializerOptions json;

    public CohereClient(string apiKey, HttpClient? httpClient = null, Uri? baseUri = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API key must be provided", nameof(apiKey));

        disposeHttpClient = httpClient is null;
        this.httpClient = httpClient ?? new HttpClient();
        this.baseUri = baseUri ?? new Uri(Constants.CohereApiUri);

        if (!this.httpClient.DefaultRequestHeaders.Contains("Authorization"))
            this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        json = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };
    }

    public Task<ChatResponseV1> ChatV1Async(ChatRequestV1 request, CancellationToken cancellationToken = default)
        => PostJsonAsync<ChatRequestV1, ChatResponseV1>("v1/chat", request, cancellationToken);

    public async IAsyncEnumerable<ChatStreamEventV1> ChatStreamV1Async(ChatRequestV1 request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        request.Stream ??= true;
        await foreach (var e in PostSseAsync<ChatRequestV1, ChatStreamEventV1>("v1/chat", request, cancellationToken))
            yield return e;
    }

    public Task<ChatResponseV2> ChatV2Async(ChatRequestV2 requestV2, CancellationToken cancellationToken = default)
        => PostJsonAsync<ChatRequestV2, ChatResponseV2>("v2/chat", requestV2, cancellationToken);

    public async IAsyncEnumerable<ChatStreamEventV2> ChatStreamV2Async(ChatRequestV2 requestV2,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var e in PostSseAsync<ChatRequestV2, ChatStreamEventV2>("v2/chat", requestV2, cancellationToken))
            yield return e;
    }

    public void Dispose()
    {
        if (disposeHttpClient)
            httpClient.Dispose();
    }

    private async Task<TResponse> PostJsonAsync<TRequest, TResponse>(string relativePath, TRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var req = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, relativePath))
        {
            Content = new StringContent(JsonSerializer.Serialize(request, json), Encoding.UTF8, "application/json")
        };

        using var resp = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);
        await EnsureSuccessAsync(resp, ct).ConfigureAwait(false);

        await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var result = await JsonSerializer.DeserializeAsync<TResponse>(stream, json, ct).ConfigureAwait(false);
        if (result == null) throw new InvalidOperationException("Failed to deserialize response body.");
        return result;
    }
}
