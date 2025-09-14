using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Cohere.Client.Models.V1;
using Cohere.Client.Tests.Fakes;

namespace Cohere.Client.Tests;

[TestFixture]
public class CohereClientTestsChatV1
{
    [Test]
    public async Task ChatV1_SendsMessageField_AndReturnsText()
    {
        // Arrange
        AuthenticationHeaderValue? capturedAuth = null;
        string? capturedBody = null;
        var handler = new FakeHttpMessageHandler(req =>
        {
            capturedAuth = req.Headers.Authorization;
            Assert.That(req.Method, Is.EqualTo(HttpMethod.Post));
            StringAssert.EndsWith("/v1/chat", req.RequestUri!.AbsoluteUri.TrimEnd('/'));

            capturedBody = req.Content is null ? null : req.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            // Respond with minimal v1 chat response
            const string json = "{\"id\":\"abc\",\"text\":\"hello\"}";
            return FakeHttpMessageHandler.Json(HttpStatusCode.OK, json);
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.cohere.ai/") };
        var client = new CohereClient("KEY", http);

        // Act
        var resp = await client.ChatV1Async(new ChatRequestV1
        {
            Model = "command-r-plus",
            Message = "Hi"
        });

        // Assert auth header set
        Assert.That(capturedAuth, Is.Not.Null);
        Assert.That(capturedAuth!.Scheme, Is.EqualTo("Bearer"));
        Assert.That(capturedAuth!.Parameter, Is.EqualTo("KEY"));

        // Assert payload contains message
        Assert.That(capturedBody, Is.Not.Null);
        StringAssert.Contains("\"message\":\"Hi\"", capturedBody!);

        // Assert response body deserialized
        Assert.That(resp.Text, Is.EqualTo("hello"));
    }

    [Test]
    public async Task ChatV1_Stream_ParsesDeltas()
    {
        // Arrange SSE response with two deltas and [DONE]
        var sse = new StringBuilder()
            .AppendLine("data: {\"type\":\"text-delta\",\"delta\":\"Part1\"}")
            .AppendLine()
            .AppendLine("data: {\"type\":\"text-delta\",\"delta\":\"Part2\"}")
            .AppendLine()
            .AppendLine("data: [DONE]")
            .ToString();

        var handler = new FakeHttpMessageHandler(req =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(sse, Encoding.UTF8, "text/event-stream")
            };
            resp.Content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");
            return resp;
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.cohere.ai/") };
        var client = new CohereClient("KEY", http);

        var req = new ChatRequestV1 { Model = "command", Message = "Hello", Stream = true };

        // Act
        var deltas = new List<string>();
        await foreach (var e in client.ChatStreamV1Async(req))
            if (!string.IsNullOrEmpty(e.Delta))
                deltas.Add(e.Delta!);

        // Assert
        Assert.That(deltas, Is.EqualTo(new[] { "Part1", "Part2" }));
    }

    [Test]
    public async Task ChatV1_Errors_IncludeBodyInException()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            FakeHttpMessageHandler.Json(HttpStatusCode.BadRequest, "{\"message\":\"invalid\"}"));
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.cohere.ai/") };
        var client = new CohereClient("KEY", http);

        var ex = Assert.ThrowsAsync<HttpRequestException>(async () =>
            await client.ChatV1Async(new ChatRequestV1 { Model = "m", Message = "x" }));
        Assert.That(ex!.Message, Does.Contain("400"));
        Assert.That(ex!.Message, Does.Contain("invalid"));
    }
}
