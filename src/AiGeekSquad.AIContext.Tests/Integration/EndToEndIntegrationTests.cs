using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using AiGeekSquad.AIContext.Chunking;
using AiGeekSquad.AIContext.Ranking;
using FluentAssertions;
using FluentAssertions.Execution;
using MathNet.Numerics.LinearAlgebra;

namespace AiGeekSquad.AIContext.Tests.Integration
{
    /// <summary>
    /// Integration tests that verify the interaction between different components
    /// of the AIContext library, simulating real-world usage scenarios.
    /// </summary>
    public class EndToEndIntegrationTests
    {
        /// <summary>
        /// Test embedding generator that creates deterministic embeddings for predictable testing.
        /// This simulates a real embedding service while maintaining test reliability.
        /// </summary>
        private class TestEmbeddingGenerator : IEmbeddingGenerator
        {
            private readonly int _dimensions;

            public TestEmbeddingGenerator(int dimensions = 384)
            {
                _dimensions = dimensions;
            }

            public Task<Vector<double>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
            {
                var embedding = CreateSemanticEmbedding(text);
                return Task.FromResult(embedding);
            }

            public async IAsyncEnumerable<Vector<double>> GenerateBatchEmbeddingsAsync(
                IEnumerable<string> texts,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                foreach (var text in texts)
                {
                    yield return await GenerateEmbeddingAsync(text, cancellationToken);
                }
            }

            /// <summary>
            /// Creates embeddings that reflect semantic similarity based on content keywords.
            /// This allows for predictable semantic chunking behavior in tests.
            /// </summary>
            private Vector<double> CreateSemanticEmbedding(string text)
            {
                var values = new double[_dimensions];
                var hash = BitConverter.ToInt32(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(text)), 0);
                var random = new Random(Math.Abs(hash));

                // Base random values
                for (var i = 0; i < _dimensions; i++)
                {
                    values[i] = random.NextDouble() * 2.0 - 1.0;
                }

                // Add semantic signals based on content
                if (text.Contains("technology") || text.Contains("AI") || text.Contains("machine"))
                {
                    for (var i = 0; i < Math.Min(20, _dimensions); i++)
                        values[i] += 0.3;
                }

                if (text.Contains("business") || text.Contains("market") || text.Contains("revenue"))
                {
                    for (var i = Math.Max(0, _dimensions - 20); i < _dimensions; i++)
                        values[i] += 0.3;
                }

                // Normalize to unit vector
                var magnitude = Math.Sqrt(values.Sum(v => v * v));
                if (magnitude > 0)
                {
                    for (var i = 0; i < _dimensions; i++)
                        values[i] /= magnitude;
                }

                return Vector<double>.Build.DenseOfArray(values);
            }
        }

        [Fact]
        public async Task CompleteRAGWorkflow_WithSemanticChunkingAndMMR_ProducesCoherentResults()
        {
            // Arrange: Simulate a document about different business topics
            var document = @"
                Artificial intelligence is transforming modern business operations. Machine learning algorithms 
                help companies analyze vast amounts of data to make informed decisions. Technology adoption 
                in enterprises has accelerated significantly over the past decade.

                Market analysis shows that businesses investing in AI technologies see improved efficiency. 
                Revenue growth is often correlated with digital transformation initiatives. Companies that 
                embrace technological innovation tend to outperform their competitors in the marketplace.";

            var embeddingGenerator = new TestEmbeddingGenerator();
            var options = new SemanticChunkingOptions
            {
                MaxTokensPerChunk = 100,
                MinTokensPerChunk = 20,
                BreakpointPercentileThreshold = 0.7,
                BufferSize = 1,
                EnableEmbeddingCaching = true
            };

            // Act: Perform semantic chunking
            var tokenCounter = new MLTokenCounter();
            var chunker = SemanticTextChunker.Create(tokenCounter, embeddingGenerator);
            var chunks = new List<TextChunk>();
            
            await foreach (var chunk in chunker.ChunkAsync(document, options))
            {
                chunks.Add(chunk);
            }

            // Simulate a user query about AI and business
            var query = "How does artificial intelligence impact business operations?";
            var queryEmbedding = await embeddingGenerator.GenerateEmbeddingAsync(query);

            // Create embeddings for chunks to use with MMR
            var chunkEmbeddings = new List<Vector<double>>();
            foreach (var chunk in chunks)
            {
                var embedding = await embeddingGenerator.GenerateEmbeddingAsync(chunk.Text);
                chunkEmbeddings.Add(embedding);
            }

            var mmrResults = MaximumMarginalRelevance.ComputeMMR(
                vectors: chunkEmbeddings,
                query: queryEmbedding,
                lambda: 0.7, // Favor relevance
                topK: Math.Min(3, chunks.Count)
            );

            // Assert: Verify the integration produces sensible results
            using var _ = new AssertionScope();
            
            // Should produce chunks
            chunks.Should().NotBeEmpty("document should be split into chunks");

            // Each chunk should have valid content
            chunks.Should().OnlyContain(c => !string.IsNullOrWhiteSpace(c.Text),
                "all chunks should have meaningful content");

            // MMR should return valid results
            mmrResults.Should().NotBeEmpty("MMR should return results");
            mmrResults.Count.Should().BeLessThanOrEqualTo(3, "MMR should not return more than requested");

            // MMR results should be valid indices
            mmrResults.Should().OnlyContain(r => r.index >= 0 && r.index < chunks.Count,
                "all MMR indices should reference valid chunks");
        }

        /// <summary>
        /// Scoring function that extracts relevance scores from chunk metadata.
        /// </summary>
        private class RelevanceScorer : IScoringFunction<TextChunk>
        {
            public string Name => "Relevance";

            public double ComputeScore(TextChunk item)
            {
                return item.Metadata.TryGetValue("relevance", out var value) ? (double)value : 0.0;
            }

            public double[] ComputeScores(IReadOnlyList<TextChunk> items)
            {
                return items.Select(ComputeScore).ToArray();
            }
        }
    }
}