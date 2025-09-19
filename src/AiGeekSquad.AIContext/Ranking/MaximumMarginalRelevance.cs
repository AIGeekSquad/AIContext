using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

using System;
using System.Collections.Generic;
using System.Linq;

namespace AiGeekSquad.AIContext.Ranking;

/// <summary>
/// Implements the Maximum Marginal Relevance (MMR) algorithm for diverse document selection.
///
/// <para>
/// Maximum Marginal Relevance (MMR) is a re-ranking algorithm designed to reduce redundancy
/// while maintaining query relevance in information retrieval systems. Originally proposed
/// by Carbonell and Goldstein (1998), MMR addresses the problem of selecting a diverse
/// subset of documents from a larger set of relevant documents.
/// </para>
///
/// <para>
/// The algorithm works by iteratively selecting documents that maximize a linear combination
/// of two criteria:
/// </para>
///
/// <list type="bullet">
/// <item><description><strong>Relevance:</strong> Similarity to the query vector, measured using cosine similarity</description></item>
/// <item><description><strong>Diversity:</strong> Dissimilarity to already selected documents, promoting variety in results</description></item>
/// </list>
///
/// <para>
/// The mathematical formulation is: MMR = λ × Sim(Di, Q) - (1-λ) × max(Sim(Di, Dj))
/// where Di is a candidate document, Q is the query, Dj represents already selected documents,
/// and λ controls the relevance-diversity trade-off.
/// </para>
///
/// <para>
/// <strong>Common Use Cases:</strong>
/// </para>
/// <list type="bullet">
/// <item><description>Search result diversification in recommendation systems</description></item>
/// <item><description>Document summarization where diverse content is needed</description></item>
/// <item><description>Content curation to avoid redundant information</description></item>
/// <item><description>Embedding-based retrieval augmented generation (RAG) systems</description></item>
/// <item><description>Research paper recommendation with topical diversity</description></item>
/// </list>
///
/// <para>
/// This implementation uses cosine similarity for measuring both relevance and diversity,
/// making it suitable for normalized vector embeddings from neural networks, TF-IDF vectors,
/// or any high-dimensional vector space where cosine similarity is meaningful.
/// </para>
/// </summary>
/// <remarks>
/// <para>
/// <strong>Performance Characteristics:</strong>
/// The algorithm has O(n²k) time complexity where n is the number of input vectors and k is
/// the number of items to select (topK). Memory usage is O(n) for storing precomputed similarities.
/// </para>
///
/// <para>
/// <strong>Implementation Details:</strong>
/// This implementation optimizes performance by precomputing all query-document similarities
/// once at the beginning, avoiding redundant cosine distance calculations during the
/// iterative selection process.
/// </para>
///
/// <para>
/// <strong>Dependencies:</strong>
/// Requires MathNet.Numerics library for vector operations and distance calculations.
/// </para>
/// </remarks>
public static class MaximumMarginalRelevance
{
    /// <summary>
    /// Computes Maximum Marginal Relevance selection from a collection of vectors to optimize
    /// both relevance to a query and diversity among selected results.
    /// </summary>
    ///
    /// <param name="vectors">
    /// The collection of vectors to select from. Each vector should be normalized for optimal
    /// cosine similarity calculations. Supports any vector dimensionality, but all vectors
    /// must have the same dimensions as the query vector. Can contain identical vectors,
    /// though this may affect diversity scoring.
    /// </param>
    ///
    /// <param name="query">
    /// The query vector representing the information need or search intent. This vector is used
    /// to compute relevance scores for all candidate vectors. Must have the same dimensionality
    /// as all vectors in the collection. For best results, should be normalized to unit length.
    /// </param>
    ///
    /// <param name="lambda">
    /// Controls the trade-off between relevance and diversity (must be between 0.0 and 1.0, inclusive).
    /// <list type="bullet">
    /// <item><description><strong>1.0:</strong> Pure relevance mode - selects vectors most similar to query, ignoring diversity</description></item>
    /// <item><description><strong>0.7-0.9:</strong> Relevance-focused with some diversity consideration</description></item>
    /// <item><description><strong>0.5:</strong> Balanced approach (recommended default) - equal weight to relevance and diversity</description></item>
    /// <item><description><strong>0.1-0.3:</strong> Diversity-focused with some relevance consideration</description></item>
    /// <item><description><strong>0.0:</strong> Pure diversity mode - selects most diverse vectors, ignoring query relevance</description></item>
    /// </list>
    /// Higher values prioritize relevance; lower values prioritize diversity. Default is 0.5.
    /// </param>
    ///
    /// <param name="topK">
    /// Maximum number of vectors to select from the input collection. If <c>null</c>, selects all
    /// vectors in MMR order. If the value exceeds the number of available vectors, returns all
    /// vectors. If 0 or negative, returns an empty list. This parameter enables efficient
    /// top-k selection without processing the entire collection when only a subset is needed.
    /// </param>
    ///
    /// <returns>
    /// A list of tuples where each tuple contains:
    /// <list type="bullet">
    /// <item><description><strong>index:</strong> Zero-based index of the vector in the original input collection</description></item>
    /// <item><description><strong>embedding:</strong> Reference to the original vector object (not a copy)</description></item>
    /// </list>
    /// Results are ordered by selection priority, with the highest-scoring vector first according to
    /// the MMR algorithm. The list maintains the chronological order in which vectors were selected
    /// by the algorithm, representing the optimal relevance-diversity trade-off for each position.
    /// </returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when lambda is outside the valid range [0.0, 1.0], or when vectors have inconsistent
    /// dimensions compared to the query vector.
    /// </exception>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when the query vector is null.
    /// </exception>
    ///
    /// <example>
    /// <para><strong>Basic Usage with Document Embeddings:</strong></para>
    /// <code>
    /// using MathNet.Numerics.LinearAlgebra;
    /// using AiGeekSquad.AIContext;
    ///
    /// // Create document embeddings (typically from a neural network)
    /// var documents = new List&lt;Vector&lt;double&gt;&gt;
    /// {
    ///     Vector&lt;double&gt;.Build.DenseOfArray(new double[] { 0.8, 0.2, 0.1 }),  // Tech article
    ///     Vector&lt;double&gt;.Build.DenseOfArray(new double[] { 0.7, 0.3, 0.2 }),  // Similar tech article
    ///     Vector&lt;double&gt;.Build.DenseOfArray(new double[] { 0.1, 0.8, 0.3 }),  // Sports article
    ///     Vector&lt;double&gt;.Build.DenseOfArray(new double[] { 0.2, 0.1, 0.9 })   // Arts article
    /// };
    ///
    /// // Query vector representing user's interest
    /// var query = Vector&lt;double&gt;.Build.DenseOfArray(new double[] { 0.9, 0.1, 0.0 });
    ///
    /// // Select top 3 diverse and relevant documents
    /// var results = MaximumMarginalRelevance.ComputeMMR(
    ///     vectors: documents,
    ///     query: query,
    ///     lambda: 0.7,  // Prefer relevance but include some diversity
    ///     topK: 3
    /// );
    ///
    /// // Process results
    /// foreach (var (index, vector) in results)
    /// {
    ///     Console.WriteLine($"Selected document {index} with embedding {vector}");
    /// }
    /// </code>
    /// </example>
    ///
    /// <example>
    /// <para><strong>Recommendation System Usage:</strong></para>
    /// <code>
    /// // For a recommendation system with user preference vector
    /// var userPreferences = Vector&lt;double&gt;.Build.DenseOfArray(new double[] { 0.6, 0.3, 0.1 });
    /// var itemEmbeddings = GetItemEmbeddings(); // Your method to get item vectors
    ///
    /// // Get diverse recommendations
    /// var recommendations = MaximumMarginalRelevance.ComputeMMR(
    ///     vectors: itemEmbeddings,
    ///     query: userPreferences,
    ///     lambda: 0.5,  // Balanced relevance and diversity
    ///     topK: 10      // Top 10 recommendations
    /// );
    ///
    /// var recommendedItems = recommendations.Select(r =&gt; GetItemById(r.index)).ToList();
    /// </code>
    /// </example>
    ///
    /// <example>
    /// <para><strong>RAG System Context Selection:</strong></para>
    /// <code>
    /// // For Retrieval Augmented Generation (RAG) systems
    /// var contextCandidates = await GetSemanticSearchResults(userQuery);
    /// var queryEmbedding = await GetQueryEmbedding(userQuery);
    ///
    /// // Select diverse context chunks to avoid redundancy
    /// var contextForLLM = MaximumMarginalRelevance.ComputeMMR(
    ///     vectors: contextCandidates.Select(c =&gt; c.Embedding).ToList(),
    ///     query: queryEmbedding,
    ///     lambda: 0.8,  // Prioritize relevance for accuracy
    ///     topK: 5       // Limit context size for LLM
    /// );
    ///
    /// var finalContext = contextForLLM
    ///     .Select(result =&gt; contextCandidates[result.index].Text)
    ///     .ToList();
    /// </code>
    /// </example>
    ///
    /// <remarks>
    /// <para>
    /// <strong>Performance Considerations:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Time complexity: O(n²k) where n is input size and k is topK</description></item>
    /// <item><description>Space complexity: O(n) for similarity caching</description></item>
    /// <item><description>For large collections (&gt;1000 vectors), consider pre-filtering with approximate similarity search</description></item>
    /// <item><description>Query similarities are precomputed once for efficiency</description></item>
    /// </list>
    ///
    /// <para>
    /// <strong>Best Practices:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Normalize input vectors to unit length for consistent cosine similarity behavior</description></item>
    /// <item><description>Use lambda values between 0.3-0.7 for most practical applications</description></item>
    /// <item><description>Set appropriate topK values to balance result quality and computational cost</description></item>
    /// <item><description>For very large datasets, combine with approximate nearest neighbor search as a pre-filter</description></item>
    /// </list>
    /// </remarks>
    public static List<(int index, Vector<double> embedding)> ComputeMMR(
        List<Vector<double>> vectors,
        Vector<double> query,
        double lambda = 0.5,
        int? topK = null)
    {
        // Parameter validation
        if (query == null)
            throw new ArgumentNullException(nameof(query), "Query vector cannot be null.");

        if (lambda < 0.0 || lambda > 1.0)
            throw new ArgumentException($"Lambda must be between 0.0 and 1.0, but was {lambda}.", nameof(lambda));

        if (vectors == null || vectors.Count == 0) return new List<(int, Vector<double>)>();

        // Validate vector dimensions consistency
        var expectedDimensions = query.Count;
        for (var i = 0; i < vectors.Count; i++)
        {
            if (vectors[i] != null && vectors[i].Count != expectedDimensions)
            {
                throw new ArgumentException(
                    $"Vector at index {i} has {vectors[i].Count} dimensions, but query vector has {expectedDimensions} dimensions. All vectors must have the same dimensionality.",
                    nameof(vectors));
            }
        }

        var k = Math.Min(topK ?? vectors.Count, vectors.Count);
        if (k <= 0) return new List<(int, Vector<double>)>();
        if (k >= vectors.Count) return vectors.Select((v, i) => (i, v)).ToList();

        var queryArray = query.ToArray();
        var vectorArrays = vectors.Select(v => v.ToArray()).ToArray();

        // Pre-compute all query similarities once for efficiency
        var querySimilarities = new double[vectors.Count];
        for (var i = 0; i < vectors.Count; i++)
        {
            querySimilarities[i] = 1.0 - Distance.Cosine(vectorArrays[i], queryArray);
        }

        var selectedIndices = new List<int>(k);
        var remainingIndices = new bool[vectors.Count];
        Array.Fill(remainingIndices, true);

        // Iteratively select k items using MMR scoring
        for (var iteration = 0; iteration < k; iteration++)
        {
            var bestIndex = -1;
            var bestScore = double.MinValue;

            // Evaluate all remaining candidates
            for (var i = 0; i < vectors.Count; i++)
            {
                if (!remainingIndices[i]) continue;

                // Relevance component: similarity to query
                var relevanceScore = lambda * querySimilarities[i];

                // Diversity component: dissimilarity to already selected items
                double diversityScore;
                if (selectedIndices.Count == 0)
                {
                    // First selection: only diversity weight matters
                    diversityScore = 1.0 - lambda;
                }
                else
                {
                    // Compute average similarity to already selected items
                    var avgSimilarity = 0.0;
                    for (var j = 0; j < selectedIndices.Count; j++)
                    {
                        var similarity = 1.0 - Distance.Cosine(vectorArrays[i], vectorArrays[selectedIndices[j]]);
                        avgSimilarity += similarity;
                    }
                    avgSimilarity /= selectedIndices.Count;

                    // Diversity score: higher when less similar to selected items
                    diversityScore = (1.0 - lambda) * (1.0 - avgSimilarity);
                }

                var totalScore = relevanceScore + diversityScore;

                if (totalScore > bestScore)
                {
                    bestScore = totalScore;
                    bestIndex = i;
                }
            }

            // Break if no valid candidate found
            if (bestIndex == -1) break;

            // Select the best candidate
            selectedIndices.Add(bestIndex);
            remainingIndices[bestIndex] = false;
        }

        return selectedIndices.Select(i => (i, vectors[i])).ToList();
    }
}