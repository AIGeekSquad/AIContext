using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;

namespace AiGeekSquad.AIContext.Chunking
{
    /// <summary>
    /// Provides functionality for generating vector embeddings from text.
    /// </summary>
    public interface IEmbeddingGenerator
    {
        /// <summary>
        /// Asynchronously generates a vector embedding for the specified text.
        /// </summary>
        /// <param name="text">The text to generate an embedding for.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the vector embedding.</returns>
        Task<Vector<double>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously generates vector embeddings for multiple texts in a batch operation.
        /// </summary>
        /// <param name="texts">The collection of texts to generate embeddings for.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>An async enumerable of vector embeddings that can be streamed for efficiency with large batches.</returns>
        IAsyncEnumerable<Vector<double>> GenerateBatchEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
    }
}