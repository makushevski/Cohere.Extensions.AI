using System.Net;
using Cohere.Client.Models;
using Cohere.Client.Tests.Fakes;

namespace Cohere.Client.Tests;

[TestFixture]
public class CohereClientTestsChatV2
{
    [Test]
    public async Task ChatV2_ReturnsText()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            FakeHttpMessageHandler.Json(HttpStatusCode.OK, "{\"id\":\"x\",\"text\":\"ok\"}"));
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.cohere.ai/") };
        var client = new CohereClient("KEY", http);

        var resp = await client.ChatV2Async(new ChatRequestV2
        {
            Model = "command-r-plus",
            Messages = new List<ChatMessageV2> { new() { Role = "user", Content = "hi" } }
        });

        Assert.That(resp.Text, Is.EqualTo("ok"));
    }
}
