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
        // Test constants to avoid magic numbers
        private const int DefaultMaxCacheSize = 1000;
        private const int CustomMaxSize = 500;
        private const int SmallMaxSize = 2;
        private const int TaskCount = 10;
        private const int OperationsPerTask = 20;
        private const int LongTextLength = 10000;
        private const double TestValue1 = 1.0;
        private const double TestValue2 = 2.0;
        private const double TestValue3 = 3.0;
        private const double TestValue4 = 4.0;
        private const double TestValue5 = 5.0;
        private const double TestValue6 = 6.0;
        
        private const string TestText = "test text";
        private const string TestText1 = "text1";
        private const string TestText2 = "text2";
        private const string TestText3 = "text3";
        private const string TestText4 = "text4";
        private const string NewText = "new_text";
        private const string NonExistentText = "non-existent text";
        private const string UnicodeText = "Hello 世界 🌍 Здравствуй мир";

        #region Constructor Tests

        [Fact]
        public void Constructor_WithDefaultMaxSize_SetsCorrectProperties()
        {
            // Act
            var cache = new EmbeddingCache();

            // Assert
            cache.MaxCacheSize.Should().Be(DefaultMaxCacheSize);
            cache.Count.Should().Be(0);
        }

        [Fact]
        public void Constructor_WithCustomMaxSize_SetsCorrectProperties()
        {
            // Act
            var cache = new EmbeddingCache(CustomMaxSize);

            // Assert
            cache.MaxCacheSize.Should().Be(CustomMaxSize);
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
            var result = cache.TryGetEmbedding(NonExistentText, out var embedding);

            // Assert
            result.Should().BeFalse();
            embedding.Should().BeNull();
        }

        [Fact]
        public void TryGetEmbedding_WithExistingText_ReturnsTrue()
        {
            // Arrange
            var cache = new EmbeddingCache();
            var originalEmbedding = Vector<double>.Build.DenseOfArray(new[] { TestValue1, TestValue2, TestValue3 });
            cache.StoreEmbedding(TestText, originalEmbedding);

            // Act
            var result = cache.TryGetEmbedding(TestText, out var retrievedEmbedding);

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
            var embedding = Vector<double>.Build.DenseOfArray(new[] { TestValue1, TestValue2, TestValue3 });

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

            // Act
            cache.StoreEmbedding(TestText, null!);

            // Assert
            cache.Count.Should().Be(0);
        }

        [Fact]
        public void StoreEmbedding_WithValidInputs_StoresCorrectly()
        {
            // Arrange
            var cache = new EmbeddingCache();
            var embedding = Vector<double>.Build.DenseOfArray(new[] { TestValue1, TestValue2, TestValue3 });

            // Act
            cache.StoreEmbedding(TestText, embedding);

            // Assert
            cache.Count.Should().Be(1);
            
            var result = cache.TryGetEmbedding(TestText, out var retrievedEmbedding);
            result.Should().BeTrue();
            retrievedEmbedding!.ToArray().Should().Equal(embedding.ToArray());
        }

        [Fact]
        public void StoreEmbedding_WithSameTextMultipleTimes_DoesNotIncrementCount()
        {
            // Arrange
            var cache = new EmbeddingCache();
            var embedding1 = Vector<double>.Build.DenseOfArray(new[] { TestValue1, TestValue2, TestValue3 });
            var embedding2 = Vector<double>.Build.DenseOfArray(new[] { TestValue4, TestValue5, TestValue6 });

            // Act
            cache.StoreEmbedding(TestText, embedding1);
            cache.StoreEmbedding(TestText, embedding2);

            // Assert
            cache.Count.Should().Be(1);
            
            // Should retrieve the first embedding (due to TryAdd behavior)
            var result = cache.TryGetEmbedding(TestText, out var retrievedEmbedding);
            result.Should().BeTrue();
            retrievedEmbedding!.ToArray().Should().Equal(embedding1.ToArray());
        }

        #endregion

        #region Cache Eviction Tests

        [Fact]
        public void StoreEmbedding_WhenCacheIsFull_EvictsOldEntries()
        {
            // Arrange
            var cache = new EmbeddingCache(5);
            
            // Fill cache to capacity
            for (var i = 0; i < 5; i++)
            {
                var text = $"text_{i}";
                var embedding = Vector<double>.Build.DenseOfArray(new[] { (double)i, (double)i + 1, (double)i + 2 });
                cache.StoreEmbedding(text, embedding);
            }

            // Act - Add one more to trigger eviction
            var newEmbedding = Vector<double>.Build.DenseOfArray(new[] { 10.0, 11.0, 12.0 });
            cache.StoreEmbedding(NewText, newEmbedding);

            // Assert
            cache.Count.Should().BeLessOrEqualTo(5);
            
            // The new embedding should be stored
            var result = cache.TryGetEmbedding(NewText, out var retrievedEmbedding);
            result.Should().BeTrue();
            retrievedEmbedding!.ToArray().Should().Equal(newEmbedding.ToArray());
        }

        [Fact]
        public void StoreEmbedding_WithSmallMaxSize_HandlesEvictionCorrectly()
        {
            // Arrange
            var cache = new EmbeddingCache(SmallMaxSize);
            
            // Fill cache to capacity
            cache.StoreEmbedding(TestText1, Vector<double>.Build.DenseOfArray(new[] { TestValue1, TestValue2 }));
            cache.StoreEmbedding(TestText2, Vector<double>.Build.DenseOfArray(new[] { TestValue3, TestValue4 }));
            
            // Act - Add more to trigger multiple evictions
            cache.StoreEmbedding(TestText3, Vector<double>.Build.DenseOfArray(new[] { TestValue5, TestValue6 }));
            cache.StoreEmbedding(TestText4, Vector<double>.Build.DenseOfArray(new[] { 7.0, 8.0 }));

            // Assert
            cache.Count.Should().BeLessOrEqualTo(SmallMaxSize);
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

            // Act
            var tasks = Enumerable.Range(0, TaskCount).Select(taskId => Task.Run(() =>
            {
                for (var i = 0; i < OperationsPerTask; i++)
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
            var embedding1 = Vector<double>.Build.DenseOfArray(new[] { TestValue1, TestValue2, TestValue3 });
            var embedding2 = Vector<double>.Build.DenseOfArray(new[] { TestValue4, TestValue5, TestValue6 });

            // Act
            cache.StoreEmbedding("Some text content", embedding1);
            cache.StoreEmbedding("Different content", embedding2);

            // Assert
            cache.Count.Should().Be(2);
            
            cache.TryGetEmbedding("Some text content", out var retrieved1).Should().BeTrue();
            retrieved1!.ToArray().Should().Equal(embedding1.ToArray());
            
            cache.TryGetEmbedding("Different content", out var retrieved2).Should().BeTrue();
            retrieved2!.ToArray().Should().Equal(embedding2.ToArray());
        }

        [Fact]
        public void StoreEmbedding_WithUnicodeText_HandlesCorrectly()
        {
            // Arrange
            var cache = new EmbeddingCache();
            var embedding = Vector<double>.Build.DenseOfArray(new[] { TestValue1, TestValue2, TestValue3 });

            // Act
            cache.StoreEmbedding(UnicodeText, embedding);

            // Assert
            cache.Count.Should().Be(1);
            
            var result = cache.TryGetEmbedding(UnicodeText, out var retrievedEmbedding);
            result.Should().BeTrue();
            retrievedEmbedding!.ToArray().Should().Equal(embedding.ToArray());
        }

        [Fact]
        public void StoreEmbedding_WithVeryLongText_HandlesCorrectly()
        {
            // Arrange
            var cache = new EmbeddingCache();
            var longText = new string('A', LongTextLength); // Very long text
            var embedding = Vector<double>.Build.DenseOfArray(new[] { TestValue1, TestValue2, TestValue3 });

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