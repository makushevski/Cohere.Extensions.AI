using System;

namespace Cohere.Extensions.AI.ConsoleChat;

internal sealed class AppConfiguration
{
    private const string ApiKeyEnvironmentVariable = "COHERE_API_KEY";
    private const string ModelEnvironmentVariable = "COHERE_MODEL";
    private const string SystemPromptEnvironmentVariable = "COHERE_SYSTEM_PROMPT";
    private const string DefaultModel = "command-r-plus-08-2024";
    private const string DefaultSystemPrompt = "You are a helpful assistant talking to a user in a terminal.";

    private AppConfiguration(string apiKey, string modelId, string systemPrompt)
    {
        ApiKey = apiKey;
        ModelId = modelId;
        SystemPrompt = systemPrompt;
    }

    public string ApiKey { get; }

    public string ModelId { get; }

    public string SystemPrompt { get; }

    public static AppConfiguration FromEnvironment()
    {
        var apiKey = Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"Environment variable {ApiKeyEnvironmentVariable} must be set with a Cohere API key.");
        }

        var modelId = Environment.GetEnvironmentVariable(ModelEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(modelId))
        {
            modelId = DefaultModel;
        }

        var systemPrompt = Environment.GetEnvironmentVariable(SystemPromptEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(systemPrompt))
        {
            systemPrompt = DefaultSystemPrompt;
        }

        return new AppConfiguration(apiKey, modelId, systemPrompt);
    }
}
