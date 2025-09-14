using System.Net.Http.Headers;
using Cohere.Client;
using Cohere.Extensions.AI.Chat;
using Microsoft.Extensions.DependencyInjection;

namespace Cohere.Extensions.AI;

/// <summary>
/// DI extensions to register the Cohere-backed Microsoft.Extensions.AI chat client.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a Cohere-backed <see cref="Microsoft.Extensions.AI.IChatClient"/> using the provided API key and optional default model ID.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="apiKey">Cohere API key. If null or empty, value is read from COHERE_API_KEY environment variable.</param>
    /// <param name="configure">Optional configuration for <see cref="CohereChatClientOptions"/> (e.g., default ModelId).</param>
    public static IServiceCollection AddCohereChatClient(this IServiceCollection services, string? apiKey = null, Action<CohereChatClientOptions>? configure = null)
    {
        var keyFromEnv = Environment.GetEnvironmentVariable("COHERE_API_KEY");
        var effectiveKey = string.IsNullOrWhiteSpace(apiKey) ? keyFromEnv : apiKey;
        if (string.IsNullOrWhiteSpace(effectiveKey))
        {
            throw new InvalidOperationException("Cohere API key is not provided. Set it via parameter or COHERE_API_KEY environment variable.");
        }

        var opts = new CohereChatClientOptions();
        configure?.Invoke(opts);

        services.AddHttpClient("Cohere", client =>
        {
            client.BaseAddress = new Uri("https://api.cohere.ai/");
            if (!client.DefaultRequestHeaders.Contains("Authorization"))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", effectiveKey);
            }
        });

        services.AddSingleton<ICohereClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var http = httpClientFactory.CreateClient("Cohere");
            return new CohereClient(effectiveKey!, http);
        });

        services.AddSingleton<Microsoft.Extensions.AI.IChatClient>(sp =>
        {
            var cohere = sp.GetRequiredService<ICohereClient>();
            return new CohereChatClient(cohere, opts);
        });

        return services;
    }
}

