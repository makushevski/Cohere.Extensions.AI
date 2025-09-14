using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Cohere.Client.Models;
using Cohere.Client.Models.V1;

namespace Cohere.Client;

public class CohereClient : ICohereClient
{
    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;
    private readonly Uri _baseUri;
    private readonly JsonSerializerOptions _json;

    public CohereClient(string apiKey, HttpClient? httpClient = null, Uri? baseUri = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API key must be provided", nameof(apiKey));

        _disposeHttpClient = httpClient is null;
        _httpClient = httpClient ?? new HttpClient();
        _baseUri = baseUri ?? new Uri("https://api.cohere.ai/");

        if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        _json = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };
    }

    public ValueTask DisposeAsync()
    {
        if (_disposeHttpClient)
        {
            _httpClient.Dispose();
        }
        return ValueTask.CompletedTask;
    }

    // =====================
    // V1 endpoints
    // =====================
    public Task<GenerateResponseV1> GenerateV1Async(GenerateRequestV1 request, CancellationToken cancellationToken = default)
        => PostJsonAsync<GenerateRequestV1, GenerateResponseV1>("v1/generate", request, cancellationToken);

    public Task<ChatResponseV1> ChatV1Async(ChatRequestV1 request, CancellationToken cancellationToken = default)
        => PostJsonAsync<ChatRequestV1, ChatResponseV1>("v1/chat", request, cancellationToken);

    public async IAsyncEnumerable<ChatStreamEventV1> ChatStreamV1Async(ChatRequestV1 request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Ensure streaming flag is set when available
        request.Stream ??= true;
        await foreach (var e in PostSseAsync<ChatRequestV1, ChatStreamEventV1>("v1/chat", request, cancellationToken))
        {
            yield return e;
        }
    }

    public Task<EmbedResponseV1> EmbedV1Async(EmbedRequestV1 request, CancellationToken cancellationToken = default)
        => PostJsonAsync<EmbedRequestV1, EmbedResponseV1>("v1/embed", request, cancellationToken);

    public Task<RerankResponseV1> RerankV1Async(RerankRequestV1 request, CancellationToken cancellationToken = default)
        => PostJsonAsync<RerankRequestV1, RerankResponseV1>("v1/rerank", request, cancellationToken);

    public Task<ClassifyResponseV1> ClassifyV1Async(ClassifyRequestV1 request, CancellationToken cancellationToken = default)
        => PostJsonAsync<ClassifyRequestV1, ClassifyResponseV1>("v1/classify", request, cancellationToken);

    public Task<TokenizeResponseV1> TokenizeV1Async(TokenizeRequestV1 request, CancellationToken cancellationToken = default)
        => PostJsonAsync<TokenizeRequestV1, TokenizeResponseV1>("v1/tokenize", request, cancellationToken);

    public Task<DetokenizeResponseV1> DetokenizeV1Async(DetokenizeRequestV1 request, CancellationToken cancellationToken = default)
        => PostJsonAsync<DetokenizeRequestV1, DetokenizeResponseV1>("v1/detokenize", request, cancellationToken);

    // =====================
    // V2 endpoints
    // =====================
    public Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
        => PostJsonAsync<ChatRequest, ChatResponse>("v2/chat", request, cancellationToken);

    public async IAsyncEnumerable<ChatStreamEvent> ChatStreamAsync(ChatRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var e in PostSseAsync<ChatRequest, ChatStreamEvent>("v2/chat", request, cancellationToken))
        {
            yield return e;
        }
    }

    public Task<EmbeddingsResponse> EmbedAsync(EmbeddingsRequest request, CancellationToken cancellationToken = default)
        => PostJsonAsync<EmbeddingsRequest, EmbeddingsResponse>("v2/embeddings", request, cancellationToken);

    public Task<RerankResponse> RerankAsync(RerankRequest request, CancellationToken cancellationToken = default)
        => PostJsonAsync<RerankRequest, RerankResponse>("v2/rerank", request, cancellationToken);

    // =====================
    // Internal helpers
    // =====================
    private async Task<TResponse> PostJsonAsync<TRequest, TResponse>(string relativePath, TRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var req = new HttpRequestMessage(HttpMethod.Post, new Uri(_baseUri, relativePath))
        {
            Content = new StringContent(JsonSerializer.Serialize(request, _json), Encoding.UTF8, "application/json")
        };

        using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        await EnsureSuccess(resp, ct).ConfigureAwait(false);

        await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var result = await JsonSerializer.DeserializeAsync<TResponse>(stream, _json, ct).ConfigureAwait(false);
        if (result == null)
        {
            throw new InvalidOperationException("Failed to deserialize response body.");
        }
        return result;
    }

    private async IAsyncEnumerable<TEvent> PostSseAsync<TRequest, TEvent>(string relativePath, TRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var req = new HttpRequestMessage(HttpMethod.Post, new Uri(_baseUri, relativePath));
        req.Headers.Accept.Clear();
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        var payload = JsonSerializer.Serialize(request, _json);
        req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        await EnsureSuccess(resp, ct).ConfigureAwait(false);

        await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        string? line;
        while (!reader.EndOfStream && (line = await reader.ReadLineAsync().ConfigureAwait(false)) is not null)
        {
            ct.ThrowIfCancellationRequested();

            if (line.Length == 0)
            {
                continue; // keep-alive/record delimiter
            }

            // Expect lines like: "data: {...json...}" or "data: [DONE]"
            if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var data = line.Substring(5).TrimStart();
                if (string.IsNullOrWhiteSpace(data))
                {
                    continue;
                }

                if (data == "[DONE]")
                {
                    yield break;
                }

                TEvent? evt = default;
                bool success = false;
                try
                {
                    evt = JsonSerializer.Deserialize<TEvent>(data, _json);
                    success = evt is not null;
                }
                catch
                {
                    success = false;
                }

                if (success && evt is not null)
                {
                    yield return evt;
                }
                else
                {
                    // Fallback: try to wrap as a delta string when TEvent has a property named "Delta"
                    if (typeof(TEvent) == typeof(ChatStreamEventV1))
                    {
                        var fallback = new ChatStreamEventV1 { Delta = data };
                        yield return (TEvent)(object)fallback;
                    }
                    else if (typeof(TEvent) == typeof(ChatStreamEvent))
                    {
                        var fallback = new ChatStreamEvent { Delta = data };
                        yield return (TEvent)(object)fallback;
                    }
                }
            }
        }
    }

    private static async Task EnsureSuccess(HttpResponseMessage resp, CancellationToken ct)
    {
        if (resp.IsSuccessStatusCode) return;

        string? body = null;
        try
        {
            body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            // ignore
        }

        var message = $"Cohere API request failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {body}";
        throw new HttpRequestException(message);
    }
}
