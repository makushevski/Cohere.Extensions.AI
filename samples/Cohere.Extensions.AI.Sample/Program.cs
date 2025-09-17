using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Cohere.Extensions.AI.Sample;

internal static class Program
{
    private static async Task Main()
    {
        var services = new ServiceCollection();

        services.AddCohereChatClient(null, opts =>
        {
            opts.ModelId = "command-r-plus-08-2024";
            //opts.UseV1 = true;
        });

        await using var provider = services.BuildServiceProvider();
        var chat = provider.GetRequiredService<IChatClient>();

        var messages = new[]
        {
            new ChatMessage(ChatRole.System, "You are a helpful assistant."),
            new ChatMessage(ChatRole.User, "Write a short haiku about the sea.")
        };

        var options = new ChatOptions();
        var ct = new CancellationTokenSource().Token;

        Console.WriteLine("=== Non-streaming ===");
        var response = await chat.GetResponseAsync(messages, options, ct);
        Console.WriteLine(response.Text);

        Console.WriteLine();
        Console.WriteLine("=== Streaming ===");
        await foreach (var update in chat.GetStreamingResponseAsync(messages, options, ct))
            if (!string.IsNullOrEmpty(update.Text))
                Console.Write(update.Text);

        Console.WriteLine();
        Console.WriteLine("\nDone.");
    }
}
