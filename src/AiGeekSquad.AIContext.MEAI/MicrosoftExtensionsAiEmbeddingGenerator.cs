using MathNet.Numerics.LinearAlgebra;

using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using IEmbeddingGenerator = AiGeekSquad.AIContext.Chunking.IEmbeddingGenerator;

namespace AiGeekSquad.AIContext.MEAI;

/// <summary>
/// An adapter that implements the AiGeekSquad.AIContext.Chunking.IEmbeddingGenerator interface
/// by wrapping a Microsoft.Extensions.AI IEmbeddingGenerator.
/// This enables seamless integration between Microsoft's AI abstractions and the AIContext library.
/// </summary>
public class MicrosoftExtensionsAiEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _innerGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftExtensionsAiEmbeddingGenerator"/> class.
    /// </summary>
    /// <param name="innerGenerator">The Microsoft Extensions AI embedding generator to wrap.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="innerGenerator"/> is null.</exception>
    public MicrosoftExtensionsAiEmbeddingGenerator(
        IEmbeddingGenerator<string, Embedding<float>> innerGenerator)
    {
        _innerGenerator = innerGenerator ?? throw new ArgumentNullException(nameof(innerGenerator));
    }

    /// <summary>
    /// Asynchronously generates a vector embedding for the specified text.
    /// </summary>
    /// <param name="text">The text to generate an embedding for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the vector embedding.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the embedding generation fails or returns unexpected results.</exception>
    public async Task<Vector<double>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        try
        {
            // Use the extension method to generate a single embedding
            var embedding = await _innerGenerator.GenerateAsync(text, cancellationToken: cancellationToken);

            if (embedding?.Vector == null)
                throw new InvalidOperationException("The embedding generator returned a null or invalid embedding.");

            // Convert ReadOnlyMemory<float> to Math.NET Vector<double>
            var floatArray = embedding.Vector.ToArray();
            var doubleArray = floatArray.Select(f => (double)f).ToArray();

            return Vector<double>.Build.DenseOfArray(doubleArray);
        }
        catch (Exception ex) when (!(ex is ArgumentNullException || ex is InvalidOperationException))
        {
            throw new InvalidOperationException($"Failed to generate embedding: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Asynchronously generates vector embeddings for multiple texts in a batch operation.
    /// </summary>
    /// <param name="texts">The collection of texts to generate embeddings for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async enumerable of vector embeddings that can be streamed for efficiency with large batches.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="texts"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the embedding generation fails or returns unexpected results.</exception>
    public IAsyncEnumerable<Vector<double>> GenerateBatchEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (texts == null)
            throw new ArgumentNullException(nameof(texts));

        return GenerateBatchEmbeddingsAsyncCore(texts, cancellationToken);
    }

    /// <summary>
    /// Core implementation for batch embedding generation.
    /// </summary>
    private async IAsyncEnumerable<Vector<double>> GenerateBatchEmbeddingsAsyncCore(
        IEnumerable<string> texts,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        GeneratedEmbeddings<Embedding<float>> embeddings;

        try
        {
            // Generate embeddings using the batch method
            embeddings = await _innerGenerator.GenerateAsync(texts, cancellationToken: cancellationToken);

            if (embeddings == null)
                throw new InvalidOperationException("The embedding generator returned null embeddings.");
        }
        catch (Exception ex) when (!(ex is ArgumentNullException || ex is InvalidOperationException))
        {
            throw new InvalidOperationException($"Failed to generate batch embeddings: {ex.Message}", ex);
        }

        // Convert each embedding and yield them (outside of try-catch to allow yield)
        foreach (var embedding in embeddings)
        {
            if (embedding?.Vector == null)
                throw new InvalidOperationException("The embedding generator returned a null or invalid embedding in the batch.");

            // Convert ReadOnlyMemory<float> to Math.NET Vector<double>
            var floatArray = embedding.Vector.ToArray();
            var doubleArray = floatArray.Select(f => (double)f).ToArray();

            yield return Vector<double>.Build.DenseOfArray(doubleArray);
        }
    }
}