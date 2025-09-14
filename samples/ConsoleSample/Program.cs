using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Cohere.Extensions.AI;

namespace ConsoleSample;

class Program
{
    static async Task Main()
    {
        // Configure DI container and register Cohere-backed IChatClient.
        // API key is read from COHERE_API_KEY environment variable if not provided explicitly.
        var services = new ServiceCollection();

        services.AddCohereChatClient(apiKey: null, configure: opts =>
        {
            // Default model (can be overridden per request via ChatOptions.ModelId).
            opts.ModelId = "command-r-plus";
            // For demo reliability, use v1 chat which returns a simple text.
            opts.UseV1 = true;
        });

        await using var provider = services.BuildServiceProvider();
        var chat = provider.GetRequiredService<IChatClient>();

        var messages = new[]
        {
            new ChatMessage(ChatRole.System, "You are a helpful assistant."),
            new ChatMessage(ChatRole.User, "Write a short haiku about the sea.")
        };

        var options = new ChatOptions();

        Console.WriteLine("=== Non-streaming ===");
        var response = await chat.GetResponseAsync(messages, options, CancellationToken.None);
        Console.WriteLine(response.Text);

        Console.WriteLine();
        Console.WriteLine("=== Streaming ===");
        await foreach (var update in chat.GetStreamingResponseAsync(messages, options, CancellationToken.None))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                Console.Write(update.Text);
            }
        }

        Console.WriteLine();
        Console.WriteLine("\nDone.");
    }
}

