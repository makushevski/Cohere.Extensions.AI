using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Cohere.Extensions.AI.ConsoleChat;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var configuration = AppConfiguration.FromEnvironment();

            using var cancellationSource = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cancellationSource.Cancel();
            };

            var services = new ServiceCollection();
            services.AddSingleton(configuration);
            services.AddCohereChatClient(configuration.ApiKey, options =>
            {
                options.ModelId = configuration.ModelId;
                options.UseV1 = true;
            });
            services.AddSingleton(_ => new ChatSession(configuration.SystemPrompt));
            services.AddSingleton<ConsoleChatApplication>();

            await using var provider = services.BuildServiceProvider();
            var application = provider.GetRequiredService<ConsoleChatApplication>();

            await application.RunAsync(cancellationSource.Token);
            return 0;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
