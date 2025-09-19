using System;
using System.Threading;
using System.Threading.Tasks;

namespace AiGeekSquad.AIContext.Chunking;

/// <summary>
/// Provides functionality for counting tokens in text.
/// </summary>
public interface ITokenCounter : IDisposable
{
    /// <summary>
    /// Counts the number of tokens in the specified text.
    /// </summary>
    /// <param name="text">The text to count tokens for.</param>
    /// <returns>The number of tokens in the text.</returns>
    int CountTokens(string text);

    /// <summary>
    /// Asynchronously counts the number of tokens in the specified text.
    /// </summary>
    /// <param name="text">The text to count tokens for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of tokens in the text.</returns>
    Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default);
}