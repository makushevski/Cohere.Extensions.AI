using System.Net;
using System.Text;

namespace Cohere.Client.Tests.Fakes;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> handler;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = handler(request);
        return Task.FromResult(response);
    }

    public static HttpResponseMessage Json(HttpStatusCode code, string json)
        => new(code)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
}
