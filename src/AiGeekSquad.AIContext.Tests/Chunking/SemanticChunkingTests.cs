using AiGeekSquad.AIContext.Chunking;

using FluentAssertions;
using FluentAssertions.Execution;

using MathNet.Numerics.LinearAlgebra;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AiGeekSquad.AIContext.Tests.Chunking
{
    public class SemanticChunkingTests
    {
        /// <summary>
        /// Creates a test embedding generator that generates deterministic embeddings based on text content.
        /// This allows for predictable testing of semantic similarity without external dependencies.
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
                var values = new double[_dimensions];
                var hash = text.GetHashCode();
                var random = new Random(Math.Abs(hash));

                for (int i = 0; i < _dimensions; i++)
                {
                    values[i] = random.NextDouble() * 2.0 - 1.0; // Range [-1, 1]
                }

                // Add semantic meaning based on keywords to create predictable similarity patterns
                if (text.Contains("technology") || text.Contains("computer") || text.Contains("software") || text.Contains("AI"))
                {
                    for (int i = 0; i < Math.Min(10, _dimensions); i++)
                        values[i] += 0.4;
                }
                
                if (text.Contains("business") || text.Contains("company") || text.Contains("market") || text.Contains("economy"))
                {
                    for (int i = 10; i < Math.Min(20, _dimensions); i++)
                        values[i] += 0.4;
                }

                if (text.Contains("science") || text.Contains("research") || text.Contains("study") || text.Contains("experiment"))
                {
                    for (int i = 20; i < Math.Min(30, _dimensions); i++)
                        values[i] += 0.4;
                }

                // Normalize to unit vector for consistent similarity calculations
                var magnitude = Math.Sqrt(values.Sum(v => v * v));
                if (magnitude > 0)
                {
                    for (int i = 0; i < _dimensions; i++)
                        values[i] /= magnitude;
                }

                return Vector<double>.Build.DenseOfArray(values);
            }
        }

        private SemanticTextChunker CreateChunker()
        {
            var tokenCounter = new MLTokenCounter();
            var embeddingGenerator = new TestEmbeddingGenerator();
            return SemanticTextChunker.Create(tokenCounter, embeddingGenerator);
        }

        [Fact]
        public async Task ChunkAsync_WithSimpleText_ReturnsNonEmptyChunks()
        {
            // Arrange
            var chunker = CreateChunker();
            var text = "This is a simple test. It has multiple sentences. Each sentence should be processed correctly.";

            // Act
            var chunks = new List<TextChunk>();
            await foreach (var chunk in chunker.ChunkAsync(text))
            {
                chunks.Add(chunk);
            }

            // Assert
            using var _ = new AssertionScope();
            chunks.Should().NotBeEmpty();
            chunks.Should().OnlyContain(chunk => !string.IsNullOrWhiteSpace(chunk.Text));
            chunks.Should().OnlyContain(chunk => chunk.StartIndex >= 0);
            chunks.Should().OnlyContain(chunk => chunk.EndIndex > chunk.StartIndex);
        }

        [Fact]
        public async Task ChunkAsync_WithNullText_ThrowsArgumentNullException()
        {
            // Arrange
            var chunker = CreateChunker();

            // Act & Assert
            var act = async () =>
            {
                var chunks = new List<TextChunk>();
                await foreach (var chunk in chunker.ChunkAsync(null!))
                {
                    chunks.Add(chunk);
                }
            };
            
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ChunkAsync_WithEmptyText_ReturnsEmptyResult()
        {
            // Arrange
            var chunker = CreateChunker();
            var text = "";

            // Act
            var chunks = new List<TextChunk>();
            await foreach (var chunk in chunker.ChunkAsync(text))
            {
                chunks.Add(chunk);
            }

            // Assert
            chunks.Should().BeEmpty();
        }

        [Fact]
        public async Task ChunkAsync_WithWhitespaceText_ReturnsEmptyResult()
        {
            // Arrange
            var chunker = CreateChunker();
            var text = "   \t\n\r   ";

            // Act
            var chunks = new List<TextChunk>();
            await foreach (var chunk in chunker.ChunkAsync(text))
            {
                chunks.Add(chunk);
            }

            // Assert
            chunks.Should().BeEmpty();
        }

        [Fact]
        public async Task ChunkAsync_WithDefaultOptions_RespectsTokenLimits()
        {
            // Arrange
            var chunker = CreateChunker();
            var options = SemanticChunkingOptions.Default;
            
            // Create a longer text that will likely exceed default chunk size
            var sentences = new[]
            {
                "Technology has revolutionized the way we live and work in the modern era.",
                "Artificial intelligence and machine learning are transforming industries across the globe.",
                "Software development practices continue to evolve with new frameworks and methodologies.",
                "Computer science research drives innovation in countless fields of human endeavor.",
                "Business leaders must adapt to rapid technological changes to remain competitive.",
                "Companies are investing heavily in digital transformation initiatives.",
                "Market conditions favor organizations that embrace technological innovation.",
                "Economic growth is increasingly tied to technological advancement and adoption."
            };
            
            var text = string.Join(" ", sentences);

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
                tokenCount.Should().BeGreaterThanOrEqualTo(options.MinTokensPerChunk);
                tokenCount.Should().BeLessThanOrEqualTo(options.MaxTokensPerChunk);
            }
        }

        [Fact]
        public async Task ChunkAsync_WithCustomOptions_RespectsConfiguredLimits()
        {
            // Arrange
            var chunker = CreateChunker();
            var options = new SemanticChunkingOptions
            {
                MinTokensPerChunk = 20,
                MaxTokensPerChunk = 100,
                BufferSize = 2,
                BreakpointPercentileThreshold = 0.90
            };

            var text = "Technology drives innovation. Software development evolves rapidly. " +
                      "Business adapts to change. Markets respond to innovation. " +
                      "Science advances knowledge. Research enables discovery. " +
                      "Economics influences decisions. Growth requires adaptation.";

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
                tokenCount.Should().BeGreaterThanOrEqualTo(options.MinTokensPerChunk);
                tokenCount.Should().BeLessThanOrEqualTo(options.MaxTokensPerChunk);
            }
        }

        [Fact]
        public async Task ChunkAsync_WithSemanticallySimilarContent_MaintainsCoherence()
        {
            // Arrange
            var chunker = CreateChunker();
            var options = new SemanticChunkingOptions
            {
                MinTokensPerChunk = 10,
                MaxTokensPerChunk = 200,
                BreakpointPercentileThreshold = 0.85
            };

            // Text with clear semantic groups
            var text = "AI and machine learning are transforming technology. " +
                      "Computer algorithms enable artificial intelligence systems. " +
                      "Software engineers develop AI applications daily. " +
                      "Business markets require strategic planning approaches. " +
                      "Company executives make important economic decisions. " +
                      "Market analysis drives business strategy development. " +
                      "Scientific research advances human knowledge significantly. " +
                      "Laboratory experiments validate research hypotheses carefully. " +
                      "Study results contribute to scientific understanding.";

            // Act
            var chunks = new List<TextChunk>();
            await foreach (var chunk in chunker.ChunkAsync(text, options))
            {
                chunks.Add(chunk);
            }

            // Assert
            using var _ = new AssertionScope();
            chunks.Should().NotBeEmpty();
            chunks.Should().HaveCountGreaterThan(1, "Should create multiple chunks for semantically different content");
            
            // Verify chunks have reasonable boundaries (not arbitrary splits)
            foreach (var chunk in chunks)
            {
                chunk.Text.Should().NotBeEmpty();
                chunk.Text.Trim().Should().NotStartWith("and");
                chunk.Text.Trim().Should().NotStartWith("or");
                chunk.Text.Trim().Should().NotStartWith("but");
            }
        }

        [Fact]
        public async Task ChunkAsync_WithMetadata_PreservesAndEnhancesMetadata()
        {
            // Arrange
            var chunker = CreateChunker();
            var originalMetadata = new Dictionary<string, object>
            {
                ["DocumentId"] = "test-doc-123",
                ["Source"] = "unit-test",
                ["Category"] = "technology"
            };

            var text = "Technology shapes our modern world. Software development continues evolving. " +
                      "AI research advances rapidly. Business strategies adapt accordingly.";

            // Act
            var chunks = new List<TextChunk>();
            await foreach (var chunk in chunker.ChunkDocumentAsync(text, originalMetadata))
            {
                chunks.Add(chunk);
            }

            // Assert
            using var _ = new AssertionScope();
            chunks.Should().NotBeEmpty();
            
            foreach (var chunk in chunks)
            {
                // Original metadata should be preserved
                chunk.Metadata.Should().ContainKey("DocumentId");
                chunk.Metadata.Should().ContainKey("Source");
                chunk.Metadata.Should().ContainKey("Category");
                
                chunk.Metadata["DocumentId"].Should().Be("test-doc-123");
                chunk.Metadata["Source"].Should().Be("unit-test");
                chunk.Metadata["Category"].Should().Be("technology");
                
                // Enhanced metadata should be added
                chunk.Metadata.Should().ContainKey("TokenCount");
                chunk.Metadata.Should().ContainKey("SegmentCount");
                
                var tokenCount = (int)chunk.Metadata["TokenCount"];
                var segmentCount = (int)chunk.Metadata["SegmentCount"];
                
                tokenCount.Should().BeGreaterThan(0);
                segmentCount.Should().BeGreaterThan(0);
            }
        }

        [Fact]
        public async Task ChunkAsync_WithDifferentBreakpointThresholds_ProducesDifferentChunking()
        {
            // Arrange
            var chunker = CreateChunker();
            var text = "Technology drives innovation in software development. " +
                      "Computer science research enables new breakthroughs. " +
                      "Business strategies must adapt to market changes. " +
                      "Economic factors influence company decisions. " +
                      "Scientific studies provide valuable insights. " +
                      "Research experiments validate theoretical models.";

            var strictOptions = new SemanticChunkingOptions
            {
                MinTokensPerChunk = 5,
                MaxTokensPerChunk = 200,
                BreakpointPercentileThreshold = 0.95 // Stricter threshold
            };

            var relaxedOptions = new SemanticChunkingOptions
            {
                MinTokensPerChunk = 5,
                MaxTokensPerChunk = 200,
                BreakpointPercentileThreshold = 0.75 // More relaxed threshold
            };

            // Act
            var strictChunks = new List<TextChunk>();
            await foreach (var chunk in chunker.ChunkAsync(text, strictOptions))
            {
                strictChunks.Add(chunk);
            }

            var relaxedChunks = new List<TextChunk>();
            await foreach (var chunk in chunker.ChunkAsync(text, relaxedOptions))
            {
                relaxedChunks.Add(chunk);
            }

            // Assert
            using var _ = new AssertionScope();
            strictChunks.Should().NotBeEmpty();
            relaxedChunks.Should().NotBeEmpty();
            
            // Stricter threshold should generally produce fewer, larger chunks
            // Relaxed threshold should generally produce more, smaller chunks
            // Note: This is a probabilistic behavior, so we test the general pattern
            strictChunks.Count.Should().BeLessThanOrEqualTo(relaxedChunks.Count + 1,
                "Stricter threshold should not produce significantly more chunks");
        }

        [Fact]
        public async Task ChunkAsync_WithCachingEnabled_WorksCorrectly()
        {
            // Arrange
            var chunker = CreateChunker();
            var options = new SemanticChunkingOptions
            {
                EnableEmbeddingCaching = true,
                MaxCacheSize = 100
            };

            var text = "Technology advances rapidly. Software development evolves continuously. " +
                      "AI research makes significant progress. Business strategies adapt accordingly.";

            // Act - First pass
            var firstPassChunks = new List<TextChunk>();
            await foreach (var chunk in chunker.ChunkAsync(text, options))
            {
                firstPassChunks.Add(chunk);
            }

            // Act - Second pass (should use cached embeddings)
            var secondPassChunks = new List<TextChunk>();
            await foreach (var chunk in chunker.ChunkAsync(text, options))
            {
                secondPassChunks.Add(chunk);
            }

            // Assert
            using var _ = new AssertionScope();
            firstPassChunks.Should().NotBeEmpty();
            secondPassChunks.Should().NotBeEmpty();
            
            // Results should be identical when using the same text and options
            firstPassChunks.Should().HaveCount(secondPassChunks.Count);
            
            for (int i = 0; i < firstPassChunks.Count; i++)
            {
                firstPassChunks[i].Text.Should().Be(secondPassChunks[i].Text);
                firstPassChunks[i].StartIndex.Should().Be(secondPassChunks[i].StartIndex);
                firstPassChunks[i].EndIndex.Should().Be(secondPassChunks[i].EndIndex);
            }
        }

        [Fact]
        public async Task ChunkAsync_WithCachingDisabled_WorksCorrectly()
        {
            // Arrange
            var chunker = CreateChunker();
            var options = new SemanticChunkingOptions
            {
                EnableEmbeddingCaching = false
            };

            var text = "Technology shapes the future. Innovation drives progress. " +
                      "Research enables discovery. Development creates solutions.";

            // Act
            var chunks = new List<TextChunk>();
            await foreach (var chunk in chunker.ChunkAsync(text, options))
            {
                chunks.Add(chunk);
            }

            // Assert
            using var _ = new AssertionScope();
            chunks.Should().NotBeEmpty();
            chunks.Should().OnlyContain(chunk => !string.IsNullOrWhiteSpace(chunk.Text));
        }

        [Fact]
        public async Task ChunkAsync_WithSingleSentence_ReturnsOneChunk()
        {
            // Arrange
            var chunker = CreateChunker();
            var options = new SemanticChunkingOptions
            {
                MinTokensPerChunk = 5,
                MaxTokensPerChunk = 100
            };

            var text = "This is a single sentence that should form one chunk.";

            // Act
            var chunks = new List<TextChunk>();
            await foreach (var chunk in chunker.ChunkAsync(text, options))
            {
                chunks.Add(chunk);
            }

            // Assert
            using var _ = new AssertionScope();
            chunks.Should().ContainSingle();
            chunks[0].Text.Should().Contain("single sentence");
            chunks[0].StartIndex.Should().Be(0);
        }

        [Fact]
        public async Task ChunkAsync_WithVeryLongSentence_HandlesCorrectly()
        {
            // Arrange
            var chunker = CreateChunker();
            var options = new SemanticChunkingOptions
            {
                MinTokensPerChunk = 10,
                MaxTokensPerChunk = 50
            };

            // Create a very long sentence that exceeds max tokens
            var longSentence = "This is an extremely long sentence that contains many words and should exceed the maximum token limit for a single chunk, forcing the system to handle it appropriately by either splitting it or handling the edge case gracefully.";

            // Act
            var chunks = new List<TextChunk>();
            await foreach (var chunk in chunker.ChunkAsync(longSentence, options))
            {
                chunks.Add(chunk);
            }

            // Assert
            using var _ = new AssertionScope();
            // The system should handle this gracefully, either by creating a chunk or handling the edge case
            // We don't enforce specific behavior for pathological cases, just ensure no exceptions
            chunks.Should().NotBeNull();
        }

        [Fact]
        public async Task ChunkAsync_WithCancellationToken_Respectscancellation()
        {
            // Arrange
            var chunker = CreateChunker();
            var text = "This is a test. Another sentence. Yet another one.";
            
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            var act = async () =>
            {
                var chunks = new List<TextChunk>();
                await foreach (var chunk in chunker.ChunkAsync(text, cancellationToken: cts.Token))
                {
                    chunks.Add(chunk);
                }
            };
            
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public void Create_WithNullTokenCounter_ThrowsArgumentNullException()
        {
            // Arrange
            var embeddingGenerator = new TestEmbeddingGenerator();

            // Act & Assert
            var act = () => SemanticTextChunker.Create(null!, embeddingGenerator);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Create_WithNullEmbeddingGenerator_ThrowsArgumentNullException()
        {
            // Arrange
            var tokenCounter = new MLTokenCounter();

            // Act & Assert
            var act = () => SemanticTextChunker.Create(tokenCounter, null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void TextChunk_Properties_WorkCorrectly()
        {
            // Arrange
            var text = "Sample chunk text";
            var startIndex = 10;
            var endIndex = 27;
            var metadata = new Dictionary<string, object> { ["key"] = "value" };

            // Act
            var chunk = new TextChunk(text, startIndex, endIndex, metadata);

            // Assert
            using var _ = new AssertionScope();
            chunk.Text.Should().Be(text);
            chunk.StartIndex.Should().Be(startIndex);
            chunk.EndIndex.Should().Be(endIndex);
            chunk.Length.Should().Be(text.Length);
            chunk.Metadata.Should().ContainKey("key");
            chunk.Metadata["key"].Should().Be("value");
        }

        [Fact]
        public void TextChunk_ToString_ReturnsFormattedString()
        {
            // Arrange
            var text = "This is a sample text chunk for testing the ToString method functionality";
            var chunk = new TextChunk(text, 0, text.Length);

            // Act
            var result = chunk.ToString();

            // Assert
            result.Should().StartWith("TextChunk[0-");
            result.Should().Contain("This is a sample text chunk for testing the ToStri");
            result.Should().Contain("...");
        }
    }
}