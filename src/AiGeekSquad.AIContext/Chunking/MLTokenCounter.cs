using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ML.Tokenizers;

namespace AiGeekSquad.AIContext.Chunking;

/// <summary>
/// Implementation of <see cref="ITokenCounter"/> using Microsoft.ML.Tokenizers.
/// Uses GPT-4 tokenizer for accurate token counting compatible with OpenAI models.
/// </summary>
public class MLTokenCounter : ITokenCounter , IDisposable
{
    private readonly Tokenizer _tokenizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="MLTokenCounter"/> class.
    /// </summary>
    /// <param name="tokenizer">The tokenizer to use for counting tokens. If null, uses GPT-4 tokenizer.</param>
    public MLTokenCounter(Tokenizer? tokenizer = null)
    {
        _tokenizer = tokenizer ?? GetDefaultTokenizer();
    }

    /// <summary>
    /// Gets the default GPT-4 tokenizer.
    /// </summary>
    /// <returns>A GPT-4 compatible tokenizer.</returns>
    private static Tokenizer GetDefaultTokenizer()
    {
        try
        {
            // Try to create GPT-4 tokenizer
            return TiktokenTokenizer.CreateForModel("gpt-4");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to create default tokenizer. Ensure Microsoft.ML.Tokenizers is properly installed.", ex);
        }
    }

    /// <summary>
    /// Counts the number of tokens in the specified text.
    /// </summary>
    /// <param name="text">The text to count tokens for.</param>
    /// <returns>The number of tokens in the text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
    public int CountTokens(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var encoded = _tokenizer.EncodeToIds(text);
        return encoded.Count;
    }

    /// <summary>
    /// Asynchronously counts the number of tokens in the specified text.
    /// </summary>
    /// <param name="text">The text to count tokens for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of tokens in the text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
    public Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Tokenization is typically fast enough to run synchronously
        // But we wrap it in Task.FromResult for the async interface
        var result = CountTokens(text);
        return Task.FromResult(result);
    }

    /// <summary>
    /// Creates a new instance of <see cref="MLTokenCounter"/> with GPT-4 tokenizer.
    /// </summary>
    /// <returns>A new <see cref="MLTokenCounter"/> instance.</returns>
    public static MLTokenCounter CreateGpt4()
    {
        return new MLTokenCounter(TiktokenTokenizer.CreateForModel("gpt-4"));
    }

    /// <summary>
    /// Creates a new instance of <see cref="MLTokenCounter"/> with GPT-3.5-turbo tokenizer.
    /// </summary>
    /// <returns>A new <see cref="MLTokenCounter"/> instance.</returns>
    public static MLTokenCounter CreateGpt35Turbo()
    {
        return new MLTokenCounter(TiktokenTokenizer.CreateForModel("gpt-3.5-turbo"));
    }

    /// <summary>
    /// Creates a new instance of <see cref="MLTokenCounter"/> with text-embedding-ada-002 tokenizer.
    /// This is commonly used for OpenAI embedding models.
    /// </summary>
    /// <returns>A new <see cref="MLTokenCounter"/> instance.</returns>
    public static MLTokenCounter CreateTextEmbeddingAda002()
    {
        return new MLTokenCounter(TiktokenTokenizer.CreateForModel("text-embedding-ada-002"));
    }

    /// <summary>
    /// Creates a new instance of <see cref="MLTokenCounter"/> with text-embedding-3-small tokenizer.
    /// This is used for OpenAI's newer small embedding model.
    /// </summary>
    /// <returns>A new <see cref="MLTokenCounter"/> instance.</returns>
    public static MLTokenCounter CreateTextEmbedding3Small()
    {
        return new MLTokenCounter(TiktokenTokenizer.CreateForModel("text-embedding-3-small"));
    }

    /// <summary>
    /// Creates a new instance of <see cref="MLTokenCounter"/> with text-embedding-3-large tokenizer.
    /// This is used for OpenAI's newer large embedding model.
    /// </summary>
    /// <returns>A new <see cref="MLTokenCounter"/> instance.</returns>
    public static MLTokenCounter CreateTextEmbedding3Large()
    {
        return new MLTokenCounter(TiktokenTokenizer.CreateForModel("text-embedding-3-large"));
    }

    /// <summary>
    /// Creates a new instance of <see cref="MLTokenCounter"/> with cl100k_base encoding.
    /// This is the base encoding used by many OpenAI models including GPT-4 and embedding models.
    /// </summary>
    /// <returns>A new <see cref="MLTokenCounter"/> instance.</returns>
    public static MLTokenCounter CreateCl100kBase()
    {
        return new MLTokenCounter(TiktokenTokenizer.CreateForEncoding("cl100k_base"));
    }

    /// <summary>
    /// Creates a new instance of <see cref="MLTokenCounter"/> with a specific model tokenizer.
    /// </summary>
    /// <param name="modelName">The model name to create tokenizer for (e.g., "gpt-4", "gpt-3.5-turbo", "text-embedding-ada-002").</param>
    /// <returns>A new <see cref="MLTokenCounter"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when model name is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when tokenizer creation fails.</exception>
    public static MLTokenCounter CreateForModel(string modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new ArgumentException("Model name cannot be null or empty.", nameof(modelName));
        }

        try
        {
            var tokenizer = TiktokenTokenizer.CreateForModel(modelName);
            return new MLTokenCounter(tokenizer);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create tokenizer for model '{modelName}'.", ex);
        }
    }

    /// <summary>
    /// Creates a new instance of <see cref="MLTokenCounter"/> with a specific encoding.
    /// </summary>
    /// <param name="encodingName">The encoding name to create tokenizer for (e.g., "cl100k_base", "p50k_base", "r50k_base").</param>
    /// <returns>A new <see cref="MLTokenCounter"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when encoding name is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when tokenizer creation fails.</exception>
    public static MLTokenCounter CreateForEncoding(string encodingName)
    {
        if (string.IsNullOrWhiteSpace(encodingName))
        {
            throw new ArgumentException("Encoding name cannot be null or empty.", nameof(encodingName));
        }

        try
        {
            var tokenizer = TiktokenTokenizer.CreateForEncoding(encodingName);
            return new MLTokenCounter(tokenizer);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create tokenizer for encoding '{encodingName}'.", ex);
        }
    }

    /// <summary>
    /// Releases any resources associated with the tokenizer.
    /// Note: Microsoft.ML.Tokenizers.Tokenizer doesn't implement IDisposable,
    /// so no cleanup is actually required for this implementation.
    /// </summary>
    public void Release()
    {
        // Microsoft.ML.Tokenizers.Tokenizer doesn't implement IDisposable
        // No cleanup required for this implementation
    }

    /// <summary>
    /// Disposes the tokenizer resources by calling Release().
    /// </summary>
    public void Dispose()
    {
        Release();
    }
}