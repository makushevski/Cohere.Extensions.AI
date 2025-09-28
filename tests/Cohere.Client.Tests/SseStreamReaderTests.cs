using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cohere.Client.Models.V2;
using Cohere.Client.Services;
using NUnit.Framework;

namespace Cohere.Client.Tests;

[TestFixture]
public class SseStreamReaderTests
{
    [Test]
    public async Task ReadSseStreamAsync_ExtractsNestedDeltaText()
    {
        var payload = "{\"type\":\"content_delta\",\"delta\":{\"message\":{\"content\":{\"type\":\"text\",\"text\":\"chunk\"}}}}";
        var buffer = Encoding.UTF8.GetBytes($"data: {payload}\n\n");
        await using var stream = new MemoryStream(buffer);
        var updates = new List<ChatStreamEventV2>();

        await foreach (var evt in SseStreamReader.ReadSseStreamAsync<ChatStreamEventV2>(stream, CancellationToken.None))
        {
            updates.Add(evt);
        }

        Assert.That(updates, Has.Count.EqualTo(1));
        Assert.That(updates[0].Delta, Is.EqualTo("chunk"));
    }

    [TestCase("{\"delta\":\"chunk\"}", "chunk")]
    [TestCase("{\"delta\":{\"text_delta\":\"piece\"}}", "piece")]
    [TestCase("{\"delta\":{\"message\":{\"content\":[{\"type\":\"text\",\"text\":\"Hello\"},{\"type\":\"text\",\"text\":\" world\"}]}}}", "Hello world")]
    [TestCase("{\"delta\":{\"custom_text\":\"alt\"}}", "alt")]
    public async Task ReadSseStreamAsync_ExtractsVariousDeltaShapes(string payload, string expected)
    {
        var buffer = Encoding.UTF8.GetBytes($"data: {payload}\n\n");
        await using var stream = new MemoryStream(buffer);
        var updates = new List<ChatStreamEventV2>();

        await foreach (var evt in SseStreamReader.ReadSseStreamAsync<ChatStreamEventV2>(stream, CancellationToken.None))
        {
            updates.Add(evt);
        }

        Assert.That(updates, Has.Count.EqualTo(1));
        Assert.That(updates[0].Delta, Is.EqualTo(expected));
    }
}
