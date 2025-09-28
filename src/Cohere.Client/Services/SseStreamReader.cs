using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using Cohere.Client.Configuration;
using Cohere.Client.Models;
using Cohere.Client.Models.V2;

namespace Cohere.Client.Services;

internal static class SseStreamReader
{
    private static readonly string[] DirectTextKeys = { "text", "text_delta" };
    private static readonly string[] NestedContentKeys = { "message", "content", "content_delta", "delta" };

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
                if (evt is ChatStreamEventV2 v2 && v2.RawDelta.ValueKind != JsonValueKind.Undefined)
                {
                    var nested = TryExtractTextDelta(v2.RawDelta);
                    if (!string.IsNullOrEmpty(nested))
                    {
                        evt.Delta = nested;
                    }
                }

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
            return TryExtractTextDelta(doc.RootElement);
        }
        catch
        {
            return null;
        }
    }

    private static string? TryExtractTextDelta(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Array => ExtractFromArray(element),
            JsonValueKind.Object => ExtractFromObject(element),
            _ => null
        };
    }

    private static string? ExtractFromArray(JsonElement array)
    {
        var sb = new StringBuilder();
        foreach (var item in array.EnumerateArray())
        {
            var part = TryExtractTextDelta(item);
            if (!string.IsNullOrEmpty(part))
            {
                sb.Append(part);
            }
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }

    private static string? ExtractFromObject(JsonElement obj)
    {
        foreach (var key in DirectTextKeys)
        {
            if (obj.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }

        foreach (var key in NestedContentKeys)
        {
            if (!obj.TryGetProperty(key, out var nested)) continue;
            var candidate = TryExtractTextDelta(nested);
            if (!string.IsNullOrEmpty(candidate))
            {
                return candidate;
            }
        }

        foreach (var property in obj.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.String &&
                property.Name.IndexOf("text", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return property.Value.GetString();
            }

            var nested = TryExtractTextDelta(property.Value);
            if (!string.IsNullOrEmpty(nested))
            {
                return nested;
            }
        }

        return null;
    }
}
