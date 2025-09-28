using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cohere.Client.Configuration;
using Cohere.Client.Models;

namespace Cohere.Client.Services;

internal class HttpRequestSender : IDisposable
{
    private bool disposed = false;
    private readonly HttpClient httpClient;
    private readonly IAuthProvider authProvider;
    private readonly bool disposeHttpClient;
    private readonly RequestBuilder requestBuilder;

    public HttpRequestSender(HttpClient httpClient, Uri baseUrl, IAuthProvider authProvider, bool disposeHttpClient = true)
    {
        this.httpClient = httpClient;
        this.authProvider = authProvider;
        this.disposeHttpClient = disposeHttpClient;
        requestBuilder = new RequestBuilder(baseUrl);
    }

    public async IAsyncEnumerable<TEvent> PostSseAsync<TRequest, TEvent>(string relativePath, TRequest request,
        [EnumeratorCancellation] CancellationToken ct) where TEvent : ITextDelta, new()
    {
        using var req = requestBuilder.BuildSseRequest(request, relativePath);
        authProvider.Apply(req);
        using var resp = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);
        await EnsureSuccessAsync(resp, ct).ConfigureAwait(false);

        await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await foreach (var result in SseStreamReader.ReadSseStreamAsync<TEvent>(stream, ct).ConfigureAwait(false))
        {
            yield return result;
        }
    }

    public async Task<TResponse> PostJsonAsync<TRequest, TResponse>(string relativePath, TRequest request,
        CancellationToken ct)
    {
        using var req = requestBuilder.BuildPostRequest(request, relativePath);
        authProvider.Apply(req);
        using var resp = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);
        await EnsureSuccessAsync(resp, ct).ConfigureAwait(false);

        await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var result = await JsonSerializer.DeserializeAsync<TResponse>(stream, JsonSettings.JsonOptions, ct)
            .ConfigureAwait(false);
        if (result == null) throw new InvalidOperationException("Failed to deserialize response body.");
        return result;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage resp, CancellationToken ct)
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

    public void Dispose()
    {
        if (!disposed && disposeHttpClient)
        {
            httpClient.Dispose();
            disposed = true;
        }
    }
}
