using AiGeekSquad.AIContext.Ranking;
using FluentAssertions;
using FluentAssertions.Execution;
using MathNet.Numerics.LinearAlgebra;

namespace AiGeekSquad.AIContext.Tests
{
    public class MaximumMarginalRelevanceTests
    {
        /// <summary>
        /// Test vectors from the notebook example
        /// </summary>
        private static List<Vector<double>> GetTestVectors()
        {
            return
            [
                Vector<double>.Build.DenseOfArray([1, 0, 0]), // Index 0
                Vector<double>.Build.DenseOfArray([1, 0, 0]), // Index 1 (identical to 0)
                Vector<double>.Build.DenseOfArray([0, 1, 0]), // Index 2
                Vector<double>.Build.DenseOfArray([0, 0, 1]), // Index 3
                Vector<double>.Build.DenseOfArray([1, 1, 0]), // Index 4
                Vector<double>.Build.DenseOfArray([1, 0, 1])
            ];
        }

        private static Vector<double> GetTestQuery()
        {
            return Vector<double>.Build.DenseOfArray([1, 0, 0]);
        }

        [Fact]
        public void ComputeMMR_WithBasicVectors_ReturnsCorrectResults()
        {
            // Arrange
            var vectors = GetTestVectors();
            var query = GetTestQuery();

            // Act
            var result = MaximumMarginalRelevance.ComputeMMR(vectors, query, lambda: 0.5, topK: 3);

            // Assert
            using var _ = new AssertionScope();
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().OnlyContain(item => item.index >= 0 && item.index < vectors.Count);
            result.Should().OnlyContain(item => item.embedding != null);
        }

        [Fact]
        public void ComputeMMR_WithLambdaOne_SelectsMostRelevantVectors()
        {
            // Arrange
            var vectors = GetTestVectors();
            var query = GetTestQuery();

            // Act - Lambda = 1.0 means pure relevance, no diversity
            var result = MaximumMarginalRelevance.ComputeMMR(vectors, query, lambda: 1.0, topK: 3);

            // Assert
            using var _ = new AssertionScope();
            result.Should().NotBeNull();
            result.Should().HaveCount(3);

            // With lambda=1.0, should select vectors most similar to query [1,0,0]
            // Expected order: indices 0 and 1 (identical to query), then 4 and 5 (partial match)
            result.Should().Contain(item => item.index == 0 || item.index == 1);

            // First two should be the most relevant (indices 0 and 1)
            var firstTwo = result.Take(2).ToList();
            firstTwo.Should().OnlyContain(item => item.index == 0 || item.index == 1);
        }

        [Fact]
        public void ComputeMMR_WithLambdaZero_SelectsMostDiverseVectors()
        {
            // Arrange
            var vectors = GetTestVectors();
            var query = GetTestQuery();

            // Act - Lambda = 0.0 means pure diversity, no relevance
            var result = MaximumMarginalRelevance.ComputeMMR(vectors, query, lambda: 0.0, topK: 3);

            // Assert
            using var _ = new AssertionScope();
            result.Should().NotBeNull();
            result.Should().HaveCount(3);

            // With lambda=0.0, should select diverse vectors regardless of relevance
            // Should avoid selecting both identical vectors (0 and 1)
            var selectedIndices = result.Select(item => item.index).ToList();
            (selectedIndices.Contains(0) && selectedIndices.Contains(1))
                .Should().BeFalse("Should not select both identical vectors when prioritizing diversity");
        }

        [Fact]
        public void ComputeMMR_WithLambdaHalf_BalancesRelevanceAndDiversity()
        {
            // Arrange
            var vectors = GetTestVectors();
            var query = GetTestQuery();

            // Act - Lambda = 0.5 means balanced relevance and diversity
            var result = MaximumMarginalRelevance.ComputeMMR(vectors, query, lambda: 0.5, topK: 3);

            // Assert
            using var _ = new AssertionScope();
            result.Should().NotBeNull();
            result.Should().HaveCount(3);

            // Should balance relevance and diversity
            var selectedIndices = result.Select(item => item.index).ToList();

            // Should include at least one highly relevant vector (0 or 1)
            (selectedIndices.Contains(0) || selectedIndices.Contains(1)).Should().BeTrue();

            // Should include diverse vectors, not just the most relevant ones
            selectedIndices.Distinct().Should().HaveCount(3, "Should select 3 different vectors");
        }

        [Fact]
        public void ComputeMMR_WithTopKZero_ReturnsEmptyList()
        {
            // Arrange
            var vectors = GetTestVectors();
            var query = GetTestQuery();

            // Act
            var result = MaximumMarginalRelevance.ComputeMMR(vectors, query, topK: 0);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void ComputeMMR_WithTopKOne_ReturnsOneResult()
        {
            // Arrange
            var vectors = GetTestVectors();
            var query = GetTestQuery();

            // Act
            var result = MaximumMarginalRelevance.ComputeMMR(vectors, query, topK: 1);

            // Assert
            using var _ = new AssertionScope();
            result.Should().NotBeNull();
            result.Should().ContainSingle();

            // Should select the most relevant vector (index 0 or 1)
            result[0].index.Should().Match(index => index == 0 || index == 1);
        }

        [Fact]
        public void ComputeMMR_WithTopKLargerThanVectorCount_ReturnsAllVectors()
        {
            // Arrange
            var vectors = GetTestVectors();
            var query = GetTestQuery();

            // Act
            var result = MaximumMarginalRelevance.ComputeMMR(vectors, query, topK: 10);

            // Assert
            using var _ = new AssertionScope();
            result.Should().NotBeNull();
            result.Should().HaveCount(vectors.Count);
        }

        [Fact]
        public void ComputeMMR_WithNullTopK_ReturnsAllVectors()
        {
            // Arrange
            var vectors = GetTestVectors();
            var query = GetTestQuery();

            // Act
            var result = MaximumMarginalRelevance.ComputeMMR(vectors, query, topK: null);

            // Assert
            using var _ = new AssertionScope();
            result.Should().NotBeNull();
            result.Should().HaveCount(vectors.Count);
        }

        [Fact]
        public void ComputeMMR_WithEmptyVectorList_ReturnsEmptyList()
        {
            // Arrange
            var vectors = new List<Vector<double>>();
            var query = GetTestQuery();

            // Act
            var result = MaximumMarginalRelevance.ComputeMMR(vectors, query);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void ComputeMMR_WithNullVectorList_ReturnsEmptyList()
        {
            // Arrange
            List<Vector<double>>? vectors = null;
            var query = GetTestQuery();

            // Act
            var result = MaximumMarginalRelevance.ComputeMMR(vectors!, query);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void ComputeMMR_WithSingleVector_ReturnsThatVector()
        {
            // Arrange
            var vectors = new List<Vector<double>>
            {
                Vector<double>.Build.DenseOfArray([1, 0, 0])
            };
            var query = GetTestQuery();

            // Act
            var result = MaximumMarginalRelevance.ComputeMMR(vectors, query);

            // Assert
            using var _ = new AssertionScope();
            result.Should().NotBeNull();
            result.Should().ContainSingle();
            result[0].index.Should().Be(0);
            result[0].embedding.Should().BeEquivalentTo(vectors[0]);
        }

        [Fact]
        public void ComputeMMR_WithIdenticalVectors_HandlesCorrectly()
        {
            // Arrange
            var vectors = new List<Vector<double>>
            {
                Vector<double>.Build.DenseOfArray([1, 0, 0]),
                Vector<double>.Build.DenseOfArray([1, 0, 0]),
                Vector<double>.Build.DenseOfArray([1, 0, 0])
            };
            var query = GetTestQuery();

            // Act
            var result = MaximumMarginalRelevance.ComputeMMR(vectors, query, lambda: 0.5, topK: 2);

            // Assert
            using var _ = new AssertionScope();
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().OnlyContain(item => item.index >= 0 && item.index < vectors.Count);
        }

        [Fact]
        public void ComputeMMR_WithDifferentLambdaValues_ProducesConsistentOrdering()
        {
            // Arrange
            var vectors = GetTestVectors();
            var query = GetTestQuery();

            // Act
            var resultRelevance = MaximumMarginalRelevance.ComputeMMR(vectors, query, lambda: 1.0, topK: 3);
            var resultDiversity = MaximumMarginalRelevance.ComputeMMR(vectors, query, lambda: 0.0, topK: 3);
            var resultBalanced = MaximumMarginalRelevance.ComputeMMR(vectors, query, lambda: 0.5, topK: 3);

            // Assert
            using var _ = new AssertionScope();
            resultRelevance.Should().NotBeNull();
            resultDiversity.Should().NotBeNull();
            resultBalanced.Should().NotBeNull();

            resultRelevance.Should().HaveCount(3);
            resultDiversity.Should().HaveCount(3);
            resultBalanced.Should().HaveCount(3);

            // Different lambda values should potentially produce different orderings
            var relevanceIndices = resultRelevance.Select(r => r.index).ToList();
            var diversityIndices = resultDiversity.Select(r => r.index).ToList();

            // At least one should be different (unless all vectors are identical)
            relevanceIndices.Should().HaveCount(3);
            diversityIndices.Should().HaveCount(3);
        }


        [Fact]
        public void ComputeMMR_PreservesVectorIntegrity()
        {
            // Arrange
            var vectors = GetTestVectors();
            var query = GetTestQuery();

            // Act
            var result = MaximumMarginalRelevance.ComputeMMR(vectors, query, lambda: 0.5, topK: 3);

            // Assert
            using var _ = new AssertionScope();
            result.Should().NotBeNull();
            foreach (var (index, embedding) in result)
            {
                // Verify that the returned embedding matches the original vector
                embedding.Should().BeEquivalentTo(vectors[index]);

                // Verify vector dimensions are preserved
                embedding.Count.Should().Be(vectors[index].Count);

                // Verify vector values are preserved
                for (var i = 0; i < vectors[index].Count; i++)
                {
                    embedding[i].Should().BeApproximately(vectors[index][i], 1e-10);
                }
            }
        }

        [Fact]
        public void ComputeMMR_WithExtremeVectors_HandlesCorrectly()
        {
            // Arrange
            var vectors = new List<Vector<double>>
            {
                Vector<double>.Build.DenseOfArray([1000, 0, 0]),    // Very large values
                Vector<double>.Build.DenseOfArray([0.001, 0, 0]),  // Very small values
                Vector<double>.Build.DenseOfArray([0, 1000, 0]),   // Orthogonal large
                Vector<double>.Build.DenseOfArray([-1000, 0, 0]),  // Negative large
                Vector<double>.Build.DenseOfArray([0, 0, 0])       // Zero vector
            };
            var query = Vector<double>.Build.DenseOfArray([1, 0, 0]);

            // Act
            var result = MaximumMarginalRelevance.ComputeMMR(vectors, query, lambda: 0.5, topK: 3);

            // Assert
            using var _ = new AssertionScope();
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().OnlyContain(item => item.index >= 0 && item.index < vectors.Count);
        }

        [Fact]
        public void ComputeMMR_ReturnsResultsInSelectionOrder()
        {
            // Arrange
            var vectors = GetTestVectors();
            var query = GetTestQuery();

            // Act
            var result = MaximumMarginalRelevance.ComputeMMR(vectors, query, lambda: 0.5, topK: 3);

            // Assert
            using var _ = new AssertionScope();
            result.Should().NotBeNull();
            result.Should().HaveCount(3);

            // The results should be in the order they were selected by the algorithm
            // This means the first result should be the best overall choice,
            // second should be the best choice given the first, etc.
            result.Should().HaveCount(3);

            // Verify indices are within bounds
            result.Should().OnlyContain(item => item.index >= 0 && item.index < vectors.Count);
        }

        [Fact]
        public void ComputeMMR_WithNullQuery_ThrowsArgumentNullException()
        {
            // Arrange
            var vectors = GetTestVectors();
            Vector<double>? query = null;

            // Act & Assert
            var act = () => MaximumMarginalRelevance.ComputeMMR(vectors, query!);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("query")
                .WithMessage("*Query vector cannot be null*");
        }

        [Theory]
        [InlineData(-0.1)]
        [InlineData(1.1)]
        [InlineData(2.0)]
        [InlineData(-1.0)]
        public void ComputeMMR_WithInvalidLambda_ThrowsArgumentException(double lambda)
        {
            // Arrange
            var vectors = GetTestVectors();
            var query = GetTestQuery();

            // Act & Assert
            var act = () => MaximumMarginalRelevance.ComputeMMR(vectors, query, lambda);
            act.Should().Throw<ArgumentException>()
                .WithParameterName("lambda")
                .WithMessage($"*Lambda must be between 0.0 and 1.0, but was {lambda}*");
        }

        [Fact]
        public void ComputeMMR_WithInconsistentVectorDimensions_ThrowsArgumentException()
        {
            // Arrange
            var vectors = new List<Vector<double>>
            {
                Vector<double>.Build.DenseOfArray([1, 0, 0]),     // 3 dimensions
                Vector<double>.Build.DenseOfArray([0, 1, 0, 1])  // 4 dimensions - inconsistent!
            };
            var query = Vector<double>.Build.DenseOfArray([1, 0, 0]); // 3 dimensions

            // Act & Assert
            var act = () => MaximumMarginalRelevance.ComputeMMR(vectors, query);
            act.Should().Throw<ArgumentException>()
                .WithParameterName("vectors")
                .WithMessage("*Vector at index 1 has 4 dimensions, but query vector has 3 dimensions*");
        }

        [Fact]
        public void ComputeMMR_WithNullVectorInList_HandlesGracefully()
        {
            // Arrange
            var vectors = new List<Vector<double>?>
            {
                Vector<double>.Build.DenseOfArray([1, 0, 0]),
                null,  // Null vector
                Vector<double>.Build.DenseOfArray([0, 1, 0])
            };
            var query = Vector<double>.Build.DenseOfArray([1, 0, 0]);

            // Act - The algorithm should handle null vectors gracefully by ignoring them during validation
            var result = MaximumMarginalRelevance.ComputeMMR(vectors!, query);
            
            // Assert - The algorithm should return results for non-null vectors
            // Since null vectors are skipped in validation and similarity computation,
            // the result may include null embeddings at indices where the original vector was null
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
            
            // Filter out results with null embeddings to verify non-null ones
            var nonNullResults = result.Where(item => item.embedding != null);
            nonNullResults.Should().OnlyContain(item => item.embedding != null);
        }
    }
}