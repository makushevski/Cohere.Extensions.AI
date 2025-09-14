using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Cohere.Client.Tests.Fakes;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = _handler(request);
        return Task.FromResult(response);
    }

    public static HttpResponseMessage Json(HttpStatusCode code, string json)
        => new HttpResponseMessage(code)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
}

