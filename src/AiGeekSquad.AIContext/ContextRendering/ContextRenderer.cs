using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiGeekSquad.AIContext.Ranking;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.AI;
using IEmbeddingGenerator = AiGeekSquad.AIContext.Chunking.IEmbeddingGenerator;
using ITextSplitter = AiGeekSquad.AIContext.Chunking.ITextSplitter;
using ITokenCounter = AiGeekSquad.AIContext.Chunking.ITokenCounter;

namespace AiGeekSquad.AIContext.ContextRendering;

/// <summary>
/// A container for chat messages and documents that renders context using Maximum Marginal Relevance (MMR)
/// to balance relevance, diversity, and freshness within a token budget.
/// </summary>
public class ContextRenderer
{
    private readonly List<ContextItem> _items;
    private readonly ITokenCounter _tokenCounter;
    private readonly IEmbeddingGenerator _embeddingGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextRenderer"/> class.
    /// </summary>
    /// <param name="tokenCounter">The token counter for measuring text token counts.</param>
    /// <param name="embeddingGenerator">The embedding generator for creating vector embeddings.</param>
    /// <exception cref="ArgumentNullException">Thrown when tokenCounter or embeddingGenerator is null.</exception>
    public ContextRenderer(ITokenCounter tokenCounter, IEmbeddingGenerator embeddingGenerator)
    {
        _tokenCounter = tokenCounter ?? throw new ArgumentNullException(nameof(tokenCounter));
        _embeddingGenerator = embeddingGenerator ?? throw new ArgumentNullException(nameof(embeddingGenerator));
        _items = new List<ContextItem>();
    }

    /// <summary>
    /// Gets the collection of context items.
    /// </summary>
    public IReadOnlyList<ContextItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Adds a chat message to the context using Microsoft.Extensions.AI.ChatMessage.
    /// </summary>
    /// <param name="message">The chat message to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when message is null.</exception>
    public async Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        // Convert ChatMessage to string representation
        var content = FormatChatMessage(message);
        var tokenCount = await _tokenCounter.CountTokensAsync(content, cancellationToken);
        var embedding = await _embeddingGenerator.GenerateEmbeddingAsync(content, cancellationToken);

        var item = new ContextItem(content, embedding, tokenCount);
        _items.Add(item);
    }

    /// <summary>
    /// Adds multiple chat messages to the context.
    /// </summary>
    /// <param name="messages">The chat messages to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when messages is null.</exception>
    public async Task AddMessagesAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        if (messages == null)
            throw new ArgumentNullException(nameof(messages));

        foreach (var message in messages)
        {
            await AddMessageAsync(message, cancellationToken);
        }
    }

    /// <summary>
    /// Adds a document to the context, chunking it if necessary.
    /// </summary>
    /// <param name="document">The document text to add.</param>
    /// <param name="textSplitter">Optional text splitter for chunking. If null, adds the entire document as one item.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when document is null or empty.</exception>
    public async Task AddDocumentAsync(string document, ITextSplitter? textSplitter = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(document))
            throw new ArgumentException("Document cannot be null or empty.", nameof(document));

        if (textSplitter == null)
        {
            // Add entire document as one item
            var tokenCount = await _tokenCounter.CountTokensAsync(document, cancellationToken);
            var embedding = await _embeddingGenerator.GenerateEmbeddingAsync(document, cancellationToken);
            var item = new ContextItem(document, embedding, tokenCount);
            _items.Add(item);
        }
        else
        {
            // Split document into chunks
            await foreach (var segment in textSplitter.SplitAsync(document, cancellationToken))
            {
                var tokenCount = await _tokenCounter.CountTokensAsync(segment.Text, cancellationToken);
                var embedding = await _embeddingGenerator.GenerateEmbeddingAsync(segment.Text, cancellationToken);
                var item = new ContextItem(segment.Text, embedding, tokenCount);
                _items.Add(item);
            }
        }
    }

    /// <summary>
    /// Renders context by selecting items using MMR to balance relevance, diversity, and freshness within a token budget.
    /// </summary>
    /// <param name="query">The query text to find relevant context for.</param>
    /// <param name="tokenBudget">Maximum number of tokens to include in the rendered context. If null, includes all items.</param>
    /// <param name="lambda">MMR lambda parameter controlling relevance vs diversity trade-off (0.0 to 1.0). Default is 0.5.</param>
    /// <param name="freshnessWeight">Weight for prioritizing recent items (0.0 to 1.0). Higher values favor newer items. Default is 0.2.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of selected context items ordered by MMR score, respecting the token budget.</returns>
    /// <exception cref="ArgumentException">Thrown when query is null or empty, lambda or freshnessWeight is out of range.</exception>
    public async Task<List<ContextItem>> RenderContextAsync(
        string query,
        int? tokenBudget = null,
        double lambda = 0.5,
        double freshnessWeight = 0.2,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));
        if (lambda < 0.0 || lambda > 1.0)
            throw new ArgumentException("Lambda must be between 0.0 and 1.0.", nameof(lambda));
        if (freshnessWeight < 0.0 || freshnessWeight > 1.0)
            throw new ArgumentException("Freshness weight must be between 0.0 and 1.0.", nameof(freshnessWeight));

        if (_items.Count == 0)
            return new List<ContextItem>();

        // Generate query embedding
        var queryEmbedding = await _embeddingGenerator.GenerateEmbeddingAsync(query, cancellationToken);

        // Apply freshness scoring if needed
        List<Vector<double>> embeddings;
        if (freshnessWeight > 0.0)
        {
            embeddings = ApplyFreshnessBoost(_items, queryEmbedding, freshnessWeight);
        }
        else
        {
            embeddings = _items.Select(item => item.Embedding).ToList();
        }

        // Use MMR to select diverse and relevant items
        var mmrResults = MaximumMarginalRelevance.ComputeMMR(
            vectors: embeddings,
            query: queryEmbedding,
            lambda: lambda,
            topK: null // Select all, we'll filter by token budget
        );

        // Select items respecting token budget
        var selectedItems = new List<ContextItem>();
        var currentTokens = 0;

        foreach (var (index, _) in mmrResults)
        {
            var item = _items[index];
            
            if (tokenBudget.HasValue)
            {
                if (currentTokens + item.TokenCount <= tokenBudget.Value)
                {
                    selectedItems.Add(item);
                    currentTokens += item.TokenCount;
                }
            }
            else
            {
                selectedItems.Add(item);
            }
        }

        return selectedItems;
    }

    /// <summary>
    /// Renders context by selecting items from a list of chat messages using MMR.
    /// </summary>
    /// <param name="queryMessages">The list of query messages to find relevant context for.</param>
    /// <param name="tokenBudget">Maximum number of tokens to include in the rendered context. If null, includes all items.</param>
    /// <param name="lambda">MMR lambda parameter controlling relevance vs diversity trade-off (0.0 to 1.0). Default is 0.5.</param>
    /// <param name="freshnessWeight">Weight for prioritizing recent items (0.0 to 1.0). Higher values favor newer items. Default is 0.2.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of selected context items ordered by MMR score, respecting the token budget.</returns>
    /// <exception cref="ArgumentNullException">Thrown when queryMessages is null.</exception>
    /// <exception cref="ArgumentException">Thrown when queryMessages is empty or lambda/freshnessWeight is out of range.</exception>
    public async Task<List<ContextItem>> RenderContextAsync(
        IEnumerable<ChatMessage> queryMessages,
        int? tokenBudget = null,
        double lambda = 0.5,
        double freshnessWeight = 0.2,
        CancellationToken cancellationToken = default)
    {
        if (queryMessages == null)
            throw new ArgumentNullException(nameof(queryMessages));

        var messageList = queryMessages.ToList();
        if (messageList.Count == 0)
            throw new ArgumentException("Query messages cannot be empty.", nameof(queryMessages));

        // Combine messages into a single query string
        var query = string.Join("\n", messageList.Select(m => FormatChatMessage(m)));

        return await RenderContextAsync(query, tokenBudget, lambda, freshnessWeight, cancellationToken);
    }

    /// <summary>
    /// Formats a ChatMessage into a string representation.
    /// </summary>
    private static string FormatChatMessage(ChatMessage message)
    {
        // Use the Text property if available, otherwise combine contents
        if (!string.IsNullOrEmpty(message.Text))
        {
            return $"{message.Role}: {message.Text}";
        }

        // If no text, try to extract text from contents
        var textContents = message.Contents?
            .OfType<TextContent>()
            .Select(tc => tc.Text)
            .Where(t => !string.IsNullOrEmpty(t));

        if (textContents != null && textContents.Any())
        {
            return $"{message.Role}: {string.Join(" ", textContents)}";
        }

        // Fallback to role only
        return $"{message.Role}: [No text content]";
    }

    /// <summary>
    /// Applies freshness boost to embeddings based on recency of items.
    /// More recent items get embeddings adjusted to be more similar to the query.
    /// </summary>
    private static List<Vector<double>> ApplyFreshnessBoost(List<ContextItem> items, Vector<double> queryEmbedding, double freshnessWeight)
    {
        if (items.Count == 0)
            return new List<Vector<double>>();

        // Find the time range
        var oldestTimestamp = items.Min(item => item.Timestamp);
        var newestTimestamp = items.Max(item => item.Timestamp);
        var timeRange = (newestTimestamp - oldestTimestamp).TotalSeconds;

        var boostedEmbeddings = new List<Vector<double>>();

        foreach (var item in items)
        {
            // Calculate freshness score (0.0 for oldest, 1.0 for newest)
            double freshnessScore = 0.5; // Default for single timestamp or no range
            if (timeRange > 0)
            {
                var age = (newestTimestamp - item.Timestamp).TotalSeconds;
                freshnessScore = 1.0 - (age / timeRange);
            }

            // Blend the original embedding with the query embedding based on freshness
            // More recent items get more weight towards the query
            var boostFactor = freshnessWeight * freshnessScore;
            var boostedEmbedding = (1.0 - boostFactor) * item.Embedding + boostFactor * queryEmbedding;
            
            boostedEmbeddings.Add(boostedEmbedding);
        }

        return boostedEmbeddings;
    }

    /// <summary>
    /// Clears all items from the context.
    /// </summary>
    public void Clear()
    {
        _items.Clear();
    }
}
