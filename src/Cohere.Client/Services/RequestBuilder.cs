using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Cohere.Client.Configuration;

namespace Cohere.Client.Services;

public class RequestBuilder
{
    private readonly Uri baseUrl;
    private readonly MediaTypeWithQualityHeaderValue eventStreamMediaType = new("text/event-stream");

    public RequestBuilder(Uri baseUrl)
    {
        this.baseUrl = baseUrl;
    }

    public HttpRequestMessage BuildSseRequest<TRequest>(TRequest request, string relativePath)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUrl, relativePath));
        req.Headers.Accept.Add(eventStreamMediaType);
        var payload = JsonSerializer.Serialize(request, JsonSettings.JsonOptions);
        req.Content = new StringContent(payload, Encoding.UTF8, MediaTypeNames.Application.Json);
        return req;
    }

    public HttpRequestMessage BuildPostRequest<TRequest>(TRequest request, string relativePath)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUrl, relativePath))
        {
            Content = new StringContent(JsonSerializer.Serialize(request, JsonSettings.JsonOptions), Encoding.UTF8, MediaTypeNames.Application.Json)
        };

        return req;
    }
}
