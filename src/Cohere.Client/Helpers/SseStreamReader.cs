using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using Cohere.Client.Configuration;
using Cohere.Client.Models;
using Cohere.Client.Models.V1;

namespace Cohere.Client.Helpers;

internal static class SseStreamReader
{
    public static async IAsyncEnumerable<TEvent> ReadSseStreamAsync<TEvent>(Stream input,
        [EnumeratorCancellation] CancellationToken ct)
    {
        using var reader = new StreamReader(input, Encoding.UTF8);

        string? line;
        while (!reader.EndOfStream && (line = await reader.ReadLineAsync(ct).ConfigureAwait(false)) is not null)
        {
            ct.ThrowIfCancellationRequested();

            if (line.Length == 0) continue; // keep-alive/record delimiter

            if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var data = line.Substring(5).TrimStart();
                if (string.IsNullOrWhiteSpace(data)) continue;

                if (data == "[DONE]") yield break;

                TEvent? evt = default;
                var success = false;
                try
                {
                    evt = JsonSerializer.Deserialize<TEvent>(data, JsonSettings.JsonOptions);
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
}
