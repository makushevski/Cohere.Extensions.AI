# Cohere.Extensions.AI

[![NuGet](https://img.shields.io/nuget/v/Cohere.Client.svg?label=Cohere.Client)](https://www.nuget.org/packages/Cohere.Client/)
[![NuGet](https://img.shields.io/nuget/v/Cohere.Extensions.AI.svg?label=Cohere.Extensions.AI)](https://www.nuget.org/packages/Cohere.Extensions.AI/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

**Cohere.Extensions.AI** is an open-source library that provides:

- A lightweight .NET client for the [Cohere API](https://docs.cohere.com/) (v1/v2, including SSE streaming).
- An `IChatClient` adapter for [`Microsoft.Extensions.AI`](https://learn.microsoft.com/dotnet/ai/overview).

⚠️ This project is independent and **not maintained or endorsed by Cohere Inc.**

---

## Installation

```bash
dotnet add package Cohere.Client
dotnet add package Cohere.Extensions.AI
````

---

## Quickstart

### Direct access to Cohere API

```csharp
using Cohere.Client;

var client = new CohereClient(Environment.GetEnvironmentVariable("COHERE_API_KEY")!);

var resp = await client.ChatV2Async(new ChatRequestV2 {
    Model = "command-r-08-2024",
    Messages = [ new() { Role = "user", Content = "Hello" } ]
});

Console.WriteLine(resp.Text);
```

### Integration with Microsoft.Extensions.AI

```csharp
using Cohere.Extensions.AI.Chat;
using Microsoft.Extensions.AI;

var chat = new CohereChatClient(
    new CohereClient(Environment.GetEnvironmentVariable("COHERE_API_KEY")!),
    new CohereChatClientOptions { ModelId = "command-r-08-2024" }
);

var answer = await chat.GetResponseAsync(
    new[] { new ChatMessage(ChatRole.User, "Ping") },
    new ChatOptions(), CancellationToken.None
);

Console.WriteLine(answer.Message?.Text);
```

---

## Features

* Support for **Cohere Chat API v1 and v2**.
* **Streaming responses** via SSE (`IAsyncEnumerable`).
* Seamless integration with `Microsoft.Extensions.AI.IChatClient`.
* Configurable `HttpClient`, `BaseUri`, and cancellation support.

---

## Documentation

* 📖 [Cohere API Reference](https://docs.cohere.com/docs)
* 📦 [NuGet Gallery](https://www.nuget.org/profiles/makushevski)

---

## License

MIT — see [LICENSE](LICENSE).