using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cohere.Client.Models;
using Cohere.Client.Models.V1;

namespace Cohere.Client.Helpers;

internal static class SseParser
{
    public static async IAsyncEnumerable<TEvent> PostSseAsync<TRequest, TEvent>(this HttpClient httpClient,string relativePath, TRequest request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var req = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, relativePath));
        req.Headers.Accept.Clear();
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        var payload = JsonSerializer.Serialize(request, json);
        req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var resp = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);
        await EnsureSuccessAsync(resp, ct).ConfigureAwait(false);

        await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        string? line;
        while (!reader.EndOfStream && (line = await reader.ReadLineAsync(ct).ConfigureAwait(false)) is not null)
        {
            ct.ThrowIfCancellationRequested();

            if (line.Length == 0) continue; // keep-alive/record delimiter

            // Expect lines like: "data: {...json...}" or "data: [DONE]"
            if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var data = line.Substring(5).TrimStart();
                if (string.IsNullOrWhiteSpace(data)) continue;

                if (data == "[DONE]") yield break;

                TEvent? evt = default;
                var success = false;
                try
                {
                    evt = JsonSerializer.Deserialize<TEvent>(data, json);
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
                    else if (typeof(TEvent) == typeof(ChatStreamEventV2))
                    {
                        var fallback = new ChatStreamEventV2 { Delta = data };
                        yield return (TEvent)(object)fallback;
                    }
                }
            }
        }
    }

    private static async IAsyncEnumerable<TEvent> PostSseAsync<TEvent>(Stream input,
        [EnumeratorCancellation] CancellationToken ct)
    {

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
}
