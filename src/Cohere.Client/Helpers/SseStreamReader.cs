using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using Cohere.Client.Configuration;
using Cohere.Client.Models;

namespace Cohere.Client.Helpers;

internal static class SseStreamReader
{
    public static async IAsyncEnumerable<TEvent> ReadSseStreamAsync<TEvent>(
        Stream input,
        [EnumeratorCancellation] CancellationToken ct)
        where TEvent : ITextDelta, new()
    {
        using var reader = new StreamReader(input, Encoding.UTF8);

        while (!reader.EndOfStream && await reader.ReadLineAsync(ct).ConfigureAwait(false) is { } line)
        {
            ct.ThrowIfCancellationRequested();

            if (line.Length == 0) continue; // keep-alive / разделитель

            if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                continue;

            var data = line.AsSpan(5).TrimStart().ToString();
            if (string.IsNullOrWhiteSpace(data)) continue;

            if (string.Equals(data, "[DONE]", StringComparison.Ordinal))
                yield break;

            var evt = default(TEvent);

            try
            {
                evt = JsonSerializer.Deserialize<TEvent>(data, JsonSettings.JsonOptions);
            }
            catch
            {
                // ignored
            }

            if (evt is not null)
            {
                // Some APIs (e.g., Cohere v2) return structured JSON events where the text lives in nested fields.
                // If Delta was not populated by deserialization, try to extract a reasonable text fragment.
                if (string.IsNullOrEmpty(evt.Delta))
                {
                    evt.Delta = TryExtractTextDelta(data);
                }

                yield return evt;
            }
            else
            {
                // Fallback: surface the raw data so callers at least see something
                yield return new TEvent { Delta = TryExtractTextDelta(data) ?? data };
            }
        }
    }

    private static string? TryExtractTextDelta(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // 1) Common case: { "delta": "..." }
            if (root.TryGetProperty("delta", out var deltaProp))
            {
                if (deltaProp.ValueKind == JsonValueKind.String)
                    return deltaProp.GetString();

                if (deltaProp.ValueKind == JsonValueKind.Object)
                {
                    // e.g., { "delta": { "text": "..." } } or { "delta": { "text_delta": "..." } }
                    if (deltaProp.TryGetProperty("text", out var textProp) && textProp.ValueKind == JsonValueKind.String)
                        return textProp.GetString();
                    if (deltaProp.TryGetProperty("text_delta", out var textDeltaProp) && textDeltaProp.ValueKind == JsonValueKind.String)
                        return textDeltaProp.GetString();
                }
            }

            // 2) Non-stream final-ish payload: { "message": { "content": [{ "type": "...text...", "text": "..." }, ...] } }
            if (root.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.Object)
            {
                if (msg.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var block in content.EnumerateArray())
                    {
                        if (block.ValueKind != JsonValueKind.Object) continue;
                        if (block.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String)
                        {
                            var t = type.GetString() ?? string.Empty;
                            if (t.IndexOf("text", System.StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                if (block.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                                    sb.Append(text.GetString());
                            }
                        }
                    }

                    var s = sb.ToString();
                    if (s.Length > 0) return s;
                }
            }

            // 3) Generic: top-level text
            if (root.TryGetProperty("text", out var textTop) && textTop.ValueKind == JsonValueKind.String)
                return textTop.GetString();
        }
        catch
        {
            // ignored
        }

        return null;
    }
}
