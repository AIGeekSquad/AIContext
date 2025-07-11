using System.Collections.Generic;
using System.Threading;

namespace AiGeekSquad.AIContext.Chunking
{
    /// <summary>
    /// Provides functionality for splitting text into smaller segments.
    /// </summary>
    public interface ITextSplitter
    {
        /// <summary>
        /// Asynchronously splits the specified text into segments.
        /// </summary>
        /// <param name="text">The text to split.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>An async enumerable of text segments with their positions in the original text.</returns>
        IAsyncEnumerable<TextSegment> SplitAsync(string text, CancellationToken cancellationToken = default);
    }
}