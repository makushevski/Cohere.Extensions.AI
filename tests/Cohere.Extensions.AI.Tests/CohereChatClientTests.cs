using Cohere.Extensions.AI.Chat;
using Cohere.Extensions.AI.Tests.Fakes;
using Coh = Cohere.Client.Models;
using CohV1 = Cohere.Client.Models.V1;
using Meai = Microsoft.Extensions.AI;

namespace Cohere.Extensions.AI.Tests;

[TestFixture]
public class CohereChatClientTests
{
    [Test]
    public async Task GetResponseAsync_V2_MapsMessagesAndReturnsText()
    {
        // Arrange
        var fake = new FakeCohereClient
        {
            OnChatAsync = req => Task.FromResult(new Coh.ChatResponseV2 { Id = "id", Text = "ok" })
        };

        var sut = new CohereChatClient(fake, new CohereChatClientOptions { ModelId = "command" });

        var messages = new[]
        {
            new Meai.ChatMessage(Meai.ChatRole.System, "sys"),
            new Meai.ChatMessage(Meai.ChatRole.User, "hello")
        };

        // Act
        var resp = await sut.GetResponseAsync(messages, new Meai.ChatOptions(), CancellationToken.None);

        // Assert
        Assert.That(resp.Text, Is.EqualTo("ok"));
        Assert.That(resp.ModelId, Is.EqualTo("command"));
    }

    [Test]
    public async Task GetStreamingResponseAsync_V2_YieldsDeltas()
    {
        // Arrange
        var fake = new FakeCohereClient
        {
            OnChatStreamAsync = req => GetAsync(new[]
            {
                new Coh.ChatStreamEventV2 { Delta = "a" },
                new Coh.ChatStreamEventV2 { Delta = "b" }
            })
        };

        var sut = new CohereChatClient(fake, new CohereChatClientOptions { ModelId = "m" });
        var messages = new[] { new Meai.ChatMessage(Meai.ChatRole.User, "hi") };

        // Act
        var deltas = new List<string>();
        await foreach (var u in sut.GetStreamingResponseAsync(messages, new Meai.ChatOptions(), CancellationToken.None))
            if (!string.IsNullOrEmpty(u.Text))
                deltas.Add(u.Text!);

        // Assert
        CollectionAssert.AreEqual(new[] { "a", "b" }, deltas);
    }

    [Test]
    public async Task GetResponseAsync_V1_UsesMessageFieldFromLastUser()
    {
        // Arrange
        CohV1.ChatRequestV1? captured = null;
        var fake = new FakeCohereClient
        {
            OnChatV1Async = req =>
            {
                captured = req;
                return Task.FromResult(new CohV1.ChatResponseV1 { Id = "id", Text = "resp" });
            }
        };

        var sut = new CohereChatClient(fake, new CohereChatClientOptions { ModelId = "m", UseV1 = true });
        var messages = new[]
        {
            new Meai.ChatMessage(Meai.ChatRole.System, "sys"),
            new Meai.ChatMessage(Meai.ChatRole.User, "first"),
            new Meai.ChatMessage(Meai.ChatRole.Assistant, "..."),
            new Meai.ChatMessage(Meai.ChatRole.User, "second")
        };

        // Act
        var resp = await sut.GetResponseAsync(messages, new Meai.ChatOptions(), CancellationToken.None);

        // Assert
        Assert.That(resp.Text, Is.EqualTo("resp"));
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Message, Is.EqualTo("second"));
        Assert.That(captured!.Stream, Is.False);
    }

    [Test]
    public async Task GetStreamingResponseAsync_V1_FallbacksToNonStreamingIfNoEvents()
    {
        // Arrange: no events from stream -> fallback to ChatV1Async
        var fake = new FakeCohereClient
        {
            OnChatStreamV1Async = req => GetAsync(Array.Empty<CohV1.ChatStreamEventV1>()),
            OnChatV1Async = req => Task.FromResult(new CohV1.ChatResponseV1 { Text = "full" })
        };

        var sut = new CohereChatClient(fake, new CohereChatClientOptions { ModelId = "m", UseV1 = true });
        var messages = new[] { new Meai.ChatMessage(Meai.ChatRole.User, "hello") };

        // Act
        var concatenated = string.Empty;
        await foreach (var u in sut.GetStreamingResponseAsync(messages, new Meai.ChatOptions(), CancellationToken.None))
            concatenated += u.Text;

        // Assert
        Assert.That(concatenated, Is.EqualTo("full"));
    }

    private static async IAsyncEnumerable<T> GetAsync<T>(IEnumerable<T> items)
    {
        foreach (var i in items)
        {
            yield return i;
            await Task.Yield();
        }
    }
}
