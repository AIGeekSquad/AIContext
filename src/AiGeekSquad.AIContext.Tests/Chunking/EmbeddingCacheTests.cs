using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MathNet.Numerics.LinearAlgebra;
using Xunit;
using AiGeekSquad.AIContext.Chunking;

namespace AiGeekSquad.AIContext.Tests.Chunking
{
    public class EmbeddingCacheTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithDefaultMaxSize_SetsCorrectProperties()
        {
            // Act
            var cache = new EmbeddingCache();

            // Assert
            cache.MaxCacheSize.Should().Be(1000);
            cache.Count.Should().Be(0);
        }

        [Fact]
        public void Constructor_WithCustomMaxSize_SetsCorrectProperties()
        {
            // Arrange
            const int maxSize = 500;

            // Act
            var cache = new EmbeddingCache(maxSize);

            // Assert
            cache.MaxCacheSize.Should().Be(maxSize);
            cache.Count.Should().Be(0);
        }

        #endregion

        #region TryGetEmbedding Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void TryGetEmbedding_WithNullOrEmptyText_ReturnsFalse(string? text)
        {
            // Arrange
            var cache = new EmbeddingCache();

            // Act
            var result = cache.TryGetEmbedding(text!, out var embedding);

            // Assert
            result.Should().BeFalse();
            embedding.Should().BeNull();
        }

        [Fact]
        public void TryGetEmbedding_WithNonExistentText_ReturnsFalse()
        {
            // Arrange
            var cache = new EmbeddingCache();

            // Act
            var result = cache.TryGetEmbedding("non-existent text", out var embedding);

            // Assert
            result.Should().BeFalse();
            embedding.Should().BeNull();
        }

        [Fact]
        public void TryGetEmbedding_WithExistingText_ReturnsTrue()
        {
            // Arrange
            var cache = new EmbeddingCache();
            var text = "test text";
            var originalEmbedding = Vector<double>.Build.DenseOfArray(new[] { 1.0, 2.0, 3.0 });
            cache.StoreEmbedding(text, originalEmbedding);

            // Act
            var result = cache.TryGetEmbedding(text, out var retrievedEmbedding);

            // Assert
            result.Should().BeTrue();
            retrievedEmbedding.Should().NotBeNull();
            retrievedEmbedding!.ToArray().Should().Equal(originalEmbedding.ToArray());
        }

        #endregion

        #region StoreEmbedding Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void StoreEmbedding_WithNullOrEmptyText_DoesNotStore(string? text)
        {
            // Arrange
            var cache = new EmbeddingCache();
            var embedding = Vector<double>.Build.DenseOfArray(new[] { 1.0, 2.0, 3.0 });

            // Act
            cache.StoreEmbedding(text!, embedding);

            // Assert
            cache.Count.Should().Be(0);
        }

        [Fact]
        public void StoreEmbedding_WithNullEmbedding_DoesNotStore()
        {
            // Arrange
            var cache = new EmbeddingCache();
            var text = "test text";

            // Act
            cache.StoreEmbedding(text, null!);

            // Assert
            cache.Count.Should().Be(0);
        }

        [Fact]
        public void StoreEmbedding_WithValidInputs_StoresCorrectly()
        {
            // Arrange
            var cache = new EmbeddingCache();
            var text = "test text";
            var embedding = Vector<double>.Build.DenseOfArray(new[] { 1.0, 2.0, 3.0 });

            // Act
            cache.StoreEmbedding(text, embedding);

            // Assert
            cache.Count.Should().Be(1);
            
            var result = cache.TryGetEmbedding(text, out var retrievedEmbedding);
            result.Should().BeTrue();
            retrievedEmbedding!.ToArray().Should().Equal(embedding.ToArray());
        }

        [Fact]
        public void StoreEmbedding_WithSameTextMultipleTimes_DoesNotIncrementCount()
        {
            // Arrange
            var cache = new EmbeddingCache();
            var text = "test text";
            var embedding1 = Vector<double>.Build.DenseOfArray(new[] { 1.0, 2.0, 3.0 });
            var embedding2 = Vector<double>.Build.DenseOfArray(new[] { 4.0, 5.0, 6.0 });

            // Act
            cache.StoreEmbedding(text, embedding1);
            cache.StoreEmbedding(text, embedding2);

            // Assert
            cache.Count.Should().Be(1);
            
            // Should retrieve the first embedding (due to TryAdd behavior)
            var result = cache.TryGetEmbedding(text, out var retrievedEmbedding);
            result.Should().BeTrue();
            retrievedEmbedding!.ToArray().Should().Equal(embedding1.ToArray());
        }

        #endregion

        #region Cache Eviction Tests

        [Fact]
        public void StoreEmbedding_WhenCacheIsFull_EvictsOldEntries()
        {
            // Arrange
            const int maxSize = 5;
            var cache = new EmbeddingCache(maxSize);
            
            // Fill cache to capacity
            for (var i = 0; i < maxSize; i++)
            {
                var text = $"text_{i}";
                var embedding = Vector<double>.Build.DenseOfArray(new[] { (double)i, (double)i + 1, (double)i + 2 });
                cache.StoreEmbedding(text, embedding);
            }

            // Act - Add one more to trigger eviction
            var newText = "new_text";
            var newEmbedding = Vector<double>.Build.DenseOfArray(new[] { 10.0, 11.0, 12.0 });
            cache.StoreEmbedding(newText, newEmbedding);

            // Assert
            cache.Count.Should().BeLessOrEqualTo(maxSize);
            
            // The new embedding should be stored
            var result = cache.TryGetEmbedding(newText, out var retrievedEmbedding);
            result.Should().BeTrue();
            retrievedEmbedding!.ToArray().Should().Equal(newEmbedding.ToArray());
        }

        [Fact]
        public void StoreEmbedding_WithSmallMaxSize_HandlesEvictionCorrectly()
        {
            // Arrange
            const int maxSize = 2;
            var cache = new EmbeddingCache(maxSize);
            
            // Fill cache to capacity
            cache.StoreEmbedding("text1", Vector<double>.Build.DenseOfArray(new[] { 1.0, 2.0 }));
            cache.StoreEmbedding("text2", Vector<double>.Build.DenseOfArray(new[] { 3.0, 4.0 }));
            
            // Act - Add more to trigger multiple evictions
            cache.StoreEmbedding("text3", Vector<double>.Build.DenseOfArray(new[] { 5.0, 6.0 }));
            cache.StoreEmbedding("text4", Vector<double>.Build.DenseOfArray(new[] { 7.0, 8.0 }));

            // Assert
            cache.Count.Should().BeLessOrEqualTo(maxSize);
        }

        #endregion

        #region Clear Tests

        [Fact]
        public void Clear_RemovesAllCachedEmbeddings()
        {
            // Arrange
            var cache = new EmbeddingCache();
            
            // Add some embeddings
            for (var i = 0; i < 5; i++)
            {
                var text = $"text_{i}";
                var embedding = Vector<double>.Build.DenseOfArray(new[] { (double)i, (double)i + 1 });
                cache.StoreEmbedding(text, embedding);
            }
            
            cache.Count.Should().Be(5);

            // Act
            cache.Clear();

            // Assert
            cache.Count.Should().Be(0);
            
            // Verify that previous embeddings are no longer retrievable
            var result = cache.TryGetEmbedding("text_0", out var retrievedEmbedding);
            result.Should().BeFalse();
            retrievedEmbedding.Should().BeNull();
        }

        [Fact]
        public void Clear_OnEmptyCache_DoesNotThrow()
        {
            // Arrange
            var cache = new EmbeddingCache();

            // Act & Assert
            var act = () => cache.Clear();
            act.Should().NotThrow();
            
            cache.Count.Should().Be(0);
        }

        #endregion

        #region Thread Safety Tests

        [Fact]
        public async Task ConcurrentOperations_DoNotCauseExceptions()
        {
            // Arrange
            var cache = new EmbeddingCache(100);
            const int taskCount = 10;
            const int operationsPerTask = 20;

            // Act
            var tasks = Enumerable.Range(0, taskCount).Select(taskId => Task.Run(() =>
            {
                for (var i = 0; i < operationsPerTask; i++)
                {
                    var text = $"task_{taskId}_text_{i}";
                    var embedding = Vector<double>.Build.DenseOfArray(new[] { (double)taskId, (double)i });
                    
                    // Store embedding
                    cache.StoreEmbedding(text, embedding);
                    
                    // Try to retrieve it
                    cache.TryGetEmbedding(text, out _);
                    
                    // Occasionally clear cache from one task
                    if (taskId == 0 && i % 10 == 0)
                    {
                        cache.Clear();
                    }
                }
            }));

            // Assert
            var act = async () => await Task.WhenAll(tasks);
            await act.Should().NotThrowAsync();
        }

        #endregion

        #region Hash Computation Edge Cases

        [Fact]
        public void StoreEmbedding_WithDifferentTextsSameHash_HandlesCorrectly()
        {
            // This test verifies the behavior when different texts might produce the same hash
            // (though this is extremely unlikely with SHA256)
            
            // Arrange
            var cache = new EmbeddingCache();
            var text1 = "Some text content";
            var text2 = "Different content";
            var embedding1 = Vector<double>.Build.DenseOfArray(new[] { 1.0, 2.0, 3.0 });
            var embedding2 = Vector<double>.Build.DenseOfArray(new[] { 4.0, 5.0, 6.0 });

            // Act
            cache.StoreEmbedding(text1, embedding1);
            cache.StoreEmbedding(text2, embedding2);

            // Assert
            cache.Count.Should().Be(2);
            
            cache.TryGetEmbedding(text1, out var retrieved1).Should().BeTrue();
            retrieved1!.ToArray().Should().Equal(embedding1.ToArray());
            
            cache.TryGetEmbedding(text2, out var retrieved2).Should().BeTrue();
            retrieved2!.ToArray().Should().Equal(embedding2.ToArray());
        }

        [Fact]
        public void StoreEmbedding_WithUnicodeText_HandlesCorrectly()
        {
            // Arrange
            var cache = new EmbeddingCache();
            var unicodeText = "Hello 世界 🌍 Здравствуй мир";
            var embedding = Vector<double>.Build.DenseOfArray(new[] { 1.0, 2.0, 3.0 });

            // Act
            cache.StoreEmbedding(unicodeText, embedding);

            // Assert
            cache.Count.Should().Be(1);
            
            var result = cache.TryGetEmbedding(unicodeText, out var retrievedEmbedding);
            result.Should().BeTrue();
            retrievedEmbedding!.ToArray().Should().Equal(embedding.ToArray());
        }

        [Fact]
        public void StoreEmbedding_WithVeryLongText_HandlesCorrectly()
        {
            // Arrange
            var cache = new EmbeddingCache();
            var longText = new string('A', 10000); // Very long text
            var embedding = Vector<double>.Build.DenseOfArray(new[] { 1.0, 2.0, 3.0 });

            // Act
            cache.StoreEmbedding(longText, embedding);

            // Assert
            cache.Count.Should().Be(1);
            
            var result = cache.TryGetEmbedding(longText, out var retrievedEmbedding);
            result.Should().BeTrue();
            retrievedEmbedding!.ToArray().Should().Equal(embedding.ToArray());
        }

        #endregion
    }
}