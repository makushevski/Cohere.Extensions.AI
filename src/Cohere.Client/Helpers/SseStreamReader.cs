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
                yield return evt;
            }
            else
            {
                yield return new TEvent { Delta = data };
            }
        }
    }
}
