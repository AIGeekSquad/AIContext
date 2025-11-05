using System;
using MathNet.Numerics.LinearAlgebra;

namespace AiGeekSquad.AIContext.ContextRendering;

/// <summary>
/// Represents an item in the context with its content, embedding, timestamp, and token count.
/// </summary>
public class ContextItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContextItem"/> class.
    /// </summary>
    /// <param name="content">The text content of the item.</param>
    /// <param name="embedding">The vector embedding of the content.</param>
    /// <param name="tokenCount">The number of tokens in the content.</param>
    /// <param name="timestamp">The timestamp when the item was added. If null, uses current UTC time.</param>
    /// <exception cref="ArgumentException">Thrown when content is null or empty, or tokenCount is negative.</exception>
    /// <exception cref="ArgumentNullException">Thrown when embedding is null.</exception>
    public ContextItem(string content, Vector<double> embedding, int tokenCount, DateTimeOffset? timestamp = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or empty.", nameof(content));
        if (embedding == null)
            throw new ArgumentNullException(nameof(embedding));
        if (tokenCount < 0)
            throw new ArgumentException("Token count cannot be negative.", nameof(tokenCount));

        Content = content;
        Embedding = embedding;
        TokenCount = tokenCount;
        Timestamp = timestamp ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the text content of the item.
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Gets the vector embedding of the content.
    /// </summary>
    public Vector<double> Embedding { get; }

    /// <summary>
    /// Gets the number of tokens in the content.
    /// </summary>
    public int TokenCount { get; }

    /// <summary>
    /// Gets the timestamp when the item was added to the context.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Returns a string representation of the context item.
    /// </summary>
    public override string ToString()
    {
        var preview = Content.Length > 50 ? Content.Substring(0, 50) + "..." : Content;
        return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {preview} ({TokenCount} tokens)";
    }
}
