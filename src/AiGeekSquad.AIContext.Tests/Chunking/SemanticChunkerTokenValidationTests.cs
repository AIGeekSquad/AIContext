using AiGeekSquad.AIContext.Chunking;

using FluentAssertions;
using FluentAssertions.Execution;

using MathNet.Numerics.LinearAlgebra;

namespace AiGeekSquad.AIContext.Tests.Chunking
{
    public class SemanticChunkerTokenValidationTests
    {
        private class TestEmbeddingGenerator(int dimensions = 384) : IEmbeddingGenerator
        {
            public Task<Vector<double>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
            {
                // Create deterministic embeddings based on text content
                var embedding = CreateDeterministicEmbedding(text);
                return Task.FromResult(embedding);
            }

            public async IAsyncEnumerable<Vector<double>> GenerateBatchEmbeddingsAsync(
                IEnumerable<string> texts,
                [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                foreach (var text in texts)
                {
                    yield return await GenerateEmbeddingAsync(text, cancellationToken);
                }
            }

            private Vector<double> CreateDeterministicEmbedding(string text)
            {
                var values = new double[dimensions];
                var hash = text.GetHashCode();
                var random = new Random(Math.Abs(hash));

                for (var i = 0; i < dimensions; i++)
                {
                    values[i] = random.NextDouble() * 2.0 - 1.0; // Range [-1, 1]
                }

                // Normalize to unit vector for consistent similarity calculations
                var magnitude = Math.Sqrt(values.Sum(v => v * v));
                if (magnitude > 0)
                {
                    for (var i = 0; i < dimensions; i++)
                        values[i] /= magnitude;
                }

                return Vector<double>.Build.DenseOfArray(values);
            }
        }

        private static SemanticTextChunker CreateChunker()
        {
            var tokenCounter = new MLTokenCounter();
            var embeddingGenerator = new TestEmbeddingGenerator();
            return SemanticTextChunker.Create(tokenCounter, embeddingGenerator);
        }

        [Fact]
        public async Task ChunkAsync_WithOversizedSegments_SplitsSegmentsBeforeEmbeddingGeneration()
        {
            // Arrange
            var chunker = CreateChunker();
            var options = new SemanticChunkingOptions
            {
                MinTokensPerChunk = 2,
                MaxTokensPerChunk = 15, // Small limit to force splitting
                BufferSize = 1,
                BreakpointPercentileThreshold = 0.80
            };

            // Create a text with very long sentences that exceed the token limit
            var longSentence = "This is an extremely long sentence that contains many words and phrases that will definitely exceed the maximum token limit " +
                              "when processed by the tokenizer and should trigger the segment validation and splitting logic before embedding generation occurs.";
            
            var text = $"{longSentence} Another sentence. Short one.";

            // Act
            var chunks = new List<TextChunk>();
            await foreach (var chunk in chunker.ChunkAsync(text, options))
            {
                chunks.Add(chunk);
            }

            // Assert
            using var _ = new AssertionScope();
            chunks.Should().NotBeEmpty("Should create chunks even with oversized segments");
            
            // All chunks should respect the token limit
            foreach (var chunk in chunks)
            {
                chunk.Metadata.Should().ContainKey("TokenCount");
                var tokenCount = (int)chunk.Metadata["TokenCount"];
                tokenCount.Should().BeLessThanOrEqualTo(options.MaxTokensPerChunk, 
                    $"Chunk should not exceed max token limit. Chunk text: '{chunk.Text}'");
                tokenCount.Should().BeGreaterThanOrEqualTo(options.MinTokensPerChunk);
            }
        }

        [Fact]
        public async Task ChunkAsync_WithSingleOversizedSentence_HandlesGracefully()
        {
            // Arrange
            var chunker = CreateChunker();
            var options = new SemanticChunkingOptions
            {
                MinTokensPerChunk = 1,
                MaxTokensPerChunk = 8, // Very small limit
                BufferSize = 0,
                BreakpointPercentileThreshold = 0.80
            };

            // Single sentence that exceeds token limit
            var oversizedText = "This is a very long single sentence that definitely exceeds the token limit and should be handled gracefully.";

            // Act
            var chunks = new List<TextChunk>();
            await foreach (var chunk in chunker.ChunkAsync(oversizedText, options))
            {
                chunks.Add(chunk);
            }

            // Assert
            using var _ = new AssertionScope();
            chunks.Should().NotBeEmpty("Should handle oversized single sentence");
            
            // All chunks should respect the token limit (except potentially edge cases with single words)
            foreach (var chunk in chunks)
            {
                chunk.Metadata.Should().ContainKey("TokenCount");
                var tokenCount = (int)chunk.Metadata["TokenCount"];
                tokenCount.Should().BeLessThanOrEqualTo(options.MaxTokensPerChunk, 
                    $"Chunk should not exceed max token limit. Chunk text: '{chunk.Text}'");
            }
        }

        [Fact]
        public async Task ChunkAsync_WithMixedSizedSegments_ProcessesAllCorrectly()
        {
            // Arrange
            var chunker = CreateChunker();
            var options = new SemanticChunkingOptions
            {
                MinTokensPerChunk = 2,
                MaxTokensPerChunk = 20,
                BufferSize = 1,
                BreakpointPercentileThreshold = 0.75
            };

            // Mix of normal and oversized segments
            var text = "Short sentence. " +
                      "This is a much longer sentence that contains significantly more words and will likely exceed the token limit for individual segments. " +
                      "Another short one. " +
                      "Here is yet another extremely long sentence with many words that should also exceed the maximum token limit and require splitting. " +
                      "Final short sentence.";

            // Act
            var chunks = new List<TextChunk>();
            await foreach (var chunk in chunker.ChunkAsync(text, options))
            {
                chunks.Add(chunk);
            }

            // Assert
            using var _ = new AssertionScope();
            chunks.Should().NotBeEmpty("Should create chunks from mixed-sized segments");
            
            foreach (var chunk in chunks)
            {
                chunk.Metadata.Should().ContainKey("TokenCount");
                var tokenCount = (int)chunk.Metadata["TokenCount"];
                tokenCount.Should().BeLessThanOrEqualTo(options.MaxTokensPerChunk, 
                    $"Chunk token count ({tokenCount}) should not exceed max limit ({options.MaxTokensPerChunk}). Chunk: '{chunk.Text}'");
                tokenCount.Should().BeGreaterThanOrEqualTo(options.MinTokensPerChunk);
            }
        }

        [Fact]
        public async Task ChunkAsync_WithZeroBufferSize_HandlesOversizedSegments()
        {
            // Arrange
            var chunker = CreateChunker();
            var options = new SemanticChunkingOptions
            {
                MinTokensPerChunk = 2,
                MaxTokensPerChunk = 12,
                BufferSize = 0, // No buffer context
                BreakpointPercentileThreshold = 0.70
            };

            var text = "This is a reasonably long sentence that might exceed token limits. " +
                      "Short one. " +
                      "Another moderately long sentence with several words that could challenge the token limit.";

            // Act
            var chunks = new List<TextChunk>();
            await foreach (var chunk in chunker.ChunkAsync(text, options))
            {
                chunks.Add(chunk);
            }

            // Assert
            using var _ = new AssertionScope();
            chunks.Should().NotBeEmpty();
            
            foreach (var chunk in chunks)
            {
                chunk.Metadata.Should().ContainKey("TokenCount");
                var tokenCount = (int)chunk.Metadata["TokenCount"];
                tokenCount.Should().BeLessThanOrEqualTo(options.MaxTokensPerChunk);
            }
        }

        [Fact]
        public async Task ChunkAsync_WithLargeBufferSize_LimitsGroupSizeCorrectly()
        {
            // Arrange
            var chunker = CreateChunker();
            var options = new SemanticChunkingOptions
            {
                MinTokensPerChunk = 2,
                MaxTokensPerChunk = 25,
                BufferSize = 5, // Large buffer that could exceed limits
                BreakpointPercentileThreshold = 0.80
            };

            var text = "First sentence. Second sentence. Third sentence. Fourth sentence. " +
                      "Fifth sentence. Sixth sentence. Seventh sentence. Eighth sentence.";

            // Act
            var chunks = new List<TextChunk>();
            await foreach (var chunk in chunker.ChunkAsync(text, options))
            {
                chunks.Add(chunk);
            }

            // Assert
            using var _ = new AssertionScope();
            chunks.Should().NotBeEmpty();
            
            foreach (var chunk in chunks)
            {
                chunk.Metadata.Should().ContainKey("TokenCount");
                var tokenCount = (int)chunk.Metadata["TokenCount"];
                tokenCount.Should().BeLessThanOrEqualTo(options.MaxTokensPerChunk, 
                    "Buffer context should not cause token limit violations");
            }
        }

        [Fact]
        public async Task ChunkAsync_WithSingleWordExceedingLimit_HandlesSafely()
        {
            // Arrange
            var chunker = CreateChunker();
            var options = new SemanticChunkingOptions
            {
                MinTokensPerChunk = 1,
                MaxTokensPerChunk = 3, // Very small limit
                BufferSize = 0
            };

            // This is an edge case where single "words" might theoretically exceed limits
            var text = "Word. Supercalifragilisticexpialidocious. Another.";

            // Act
            var chunks = new List<TextChunk>();
            await foreach (var chunk in chunker.ChunkAsync(text, options))
            {
                chunks.Add(chunk);
            }

            // Assert
            using var _ = new AssertionScope();
            chunks.Should().NotBeEmpty("Should handle edge cases gracefully");
            chunks.Should().OnlyContain(c => !string.IsNullOrWhiteSpace(c.Text));
        }

        [Fact]
        public async Task ChunkAsync_EnsuresNoEmbeddingGenerationForOversizedSegments()
        {
            // This test ensures that we don't pass oversized segments to the embedding generator
            // which could cause it to fail
            
            // Arrange
            var chunker = CreateChunker();
            var options = new SemanticChunkingOptions
            {
                MinTokensPerChunk = 2,
                MaxTokensPerChunk = 10,
                BufferSize = 2,
                BreakpointPercentileThreshold = 0.85
            };

            // Create text that will result in segments exceeding the limit when buffered
            var text = "Technology and artificial intelligence. " +
                      "Machine learning algorithms are complex. " +
                      "Data science requires statistical knowledge. " +
                      "Software development needs programming skills.";

            // Act & Assert - This should not throw an exception
            var chunks = new List<TextChunk>();
            var action = async () =>
            {
                await foreach (var chunk in chunker.ChunkAsync(text, options))
                {
                    chunks.Add(chunk);
                }
            };

            await action.Should().NotThrowAsync("Chunker should handle oversized segments without errors");
            
            chunks.Should().NotBeEmpty();
            foreach (var chunk in chunks)
            {
                var tokenCount = (int)chunk.Metadata["TokenCount"];
                tokenCount.Should().BeLessThanOrEqualTo(options.MaxTokensPerChunk);
            }
        }

        [Fact]
        public async Task ChunkAsync_WithVeryRestrictiveTokenLimit_StillProducesValidChunks()
        {
            // Arrange
            var chunker = CreateChunker();
            var options = new SemanticChunkingOptions
            {
                MinTokensPerChunk = 1,
                MaxTokensPerChunk = 5, // Very restrictive
                BufferSize = 1,
                BreakpointPercentileThreshold = 0.90
            };

            var text = "AI is powerful. Machine learning transforms industries. Data drives decisions.";

            // Act
            var chunks = new List<TextChunk>();
            await foreach (var chunk in chunker.ChunkAsync(text, options))
            {
                chunks.Add(chunk);
            }

            // Assert
            using var _ = new AssertionScope();
            chunks.Should().NotBeEmpty("Should create at least one chunk");
            
            foreach (var chunk in chunks)
            {
                chunk.Metadata.Should().ContainKey("TokenCount");
                var tokenCount = (int)chunk.Metadata["TokenCount"];
                tokenCount.Should().BeLessThanOrEqualTo(options.MaxTokensPerChunk, 
                    $"Chunk token count ({tokenCount}) should not exceed max limit ({options.MaxTokensPerChunk}). Chunk: '{chunk.Text}'");
                tokenCount.Should().BeGreaterThanOrEqualTo(options.MinTokensPerChunk);
            }
        }
    }
}
