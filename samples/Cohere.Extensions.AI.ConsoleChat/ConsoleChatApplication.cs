using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace Cohere.Extensions.AI.ConsoleChat;

internal sealed class ConsoleChatApplication
{
    private readonly AppConfiguration configuration;
    private readonly IChatClient chatClient;
    private readonly ChatSession session;

    public ConsoleChatApplication(IChatClient chatClient, ChatSession session, AppConfiguration configuration)
    {
        this.chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        PrintBanner();

        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write("you> ");
            var input = Console.ReadLine();
            if (input is null)
            {
                break;
            }

            var trimmed = input.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (IsCommand(trimmed, "/exit") || IsCommand(trimmed, "/quit"))
            {
                break;
            }

            if (IsCommand(trimmed, "/clear") || IsCommand(trimmed, "/reset"))
            {
                session.Reset();
                WriteSystem("Conversation cleared.");
                continue;
            }

            await SendMessageAsync(input, cancellationToken);
        }
    }

    private async Task SendMessageAsync(string userInput, CancellationToken cancellationToken)
    {
        session.AddUserMessage(userInput);

        Console.Write("assistant> ");
        var builder = new StringBuilder();
        var options = new ChatOptions
        {
            ModelId = configuration.ModelId
        };

        await foreach (var update in chatClient.GetStreamingResponseAsync(session.Messages, options, cancellationToken))
        {
            if (string.IsNullOrEmpty(update.Text))
            {
                continue;
            }

            builder.Append(update.Text);
            Console.Write(update.Text);
        }

        Console.WriteLine();

        if (builder.Length > 0)
        {
            session.AddAssistantMessage(builder.ToString());
        }
        else
        {
            WriteSystem("No response received from the model.");
        }
    }

    private void PrintBanner()
    {
        Console.WriteLine("Cohere Console Chat");
        Console.WriteLine(new string('-', 20));
        Console.WriteLine($"Model: {configuration.ModelId}");
        Console.WriteLine("Commands: /exit, /quit, /clear, /reset");
        if (!string.IsNullOrWhiteSpace(configuration.SystemPrompt))
        {
            Console.WriteLine();
            Console.WriteLine("System prompt:");
            Console.WriteLine(configuration.SystemPrompt);
        }
        Console.WriteLine();
    }

    private static void WriteSystem(string message)
    {
        Console.WriteLine($"system> {message}");
    }

    private static bool IsCommand(string input, string command)
        => string.Equals(input, command, StringComparison.OrdinalIgnoreCase);
}
