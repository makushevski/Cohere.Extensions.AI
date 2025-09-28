using System.Net.Http;
using System.Net.Http.Headers;

namespace Cohere.Client.Services;

public interface IAuthProvider
{
    void Apply(HttpRequestMessage request);
}

public class AuthProvider : IAuthProvider
{
    private readonly string apiKey;

    public AuthProvider(string apiKey)
    {
        this.apiKey = apiKey;
    }

    public void Apply(HttpRequestMessage request) => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
}
