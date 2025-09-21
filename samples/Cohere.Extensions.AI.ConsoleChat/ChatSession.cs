using System;
using System.Collections.Generic;
using Microsoft.Extensions.AI;

namespace Cohere.Extensions.AI.ConsoleChat;

internal sealed class ChatSession
{
    private readonly List<ChatMessage> history = new();
    private readonly string systemPrompt;

    public ChatSession(string systemPrompt)
    {
        this.systemPrompt = systemPrompt;
        Reset();
    }

    public IReadOnlyList<ChatMessage> Messages => history;

    public void AddUserMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text must not be empty.", nameof(text));
        }

        history.Add(new ChatMessage(ChatRole.User, text));
    }

    public void AddAssistantMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text must not be empty.", nameof(text));
        }

        history.Add(new ChatMessage(ChatRole.Assistant, text));
    }

    public void Reset()
    {
        history.Clear();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            history.Add(new ChatMessage(ChatRole.System, systemPrompt));
        }
    }
}
