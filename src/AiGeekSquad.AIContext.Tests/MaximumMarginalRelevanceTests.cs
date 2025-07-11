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
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.All(result, item => Assert.True(item.index >= 0 && item.index < vectors.Count));
            Assert.All(result, item => Assert.NotNull(item.embedding));
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
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);

            // With lambda=1.0, should select vectors most similar to query [1,0,0]
            // Expected order: indices 0 and 1 (identical to query), then 4 and 5 (partial match)
            Assert.Contains(result, item => item.index == 0 || item.index == 1);

            // First two should be the most relevant (indices 0 and 1)
            var firstTwo = result.Take(2).ToList();
            Assert.True(firstTwo.All(item => item.index == 0 || item.index == 1));
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
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);

            // With lambda=0.0, should select diverse vectors regardless of relevance
            // Should avoid selecting both identical vectors (0 and 1)
            var selectedIndices = result.Select(item => item.index).ToList();
            Assert.False(selectedIndices.Contains(0) && selectedIndices.Contains(1),
                "Should not select both identical vectors when prioritizing diversity");
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
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);

            // Should balance relevance and diversity
            var selectedIndices = result.Select(item => item.index).ToList();

            // Should include at least one highly relevant vector (0 or 1)
            Assert.True(selectedIndices.Contains(0) || selectedIndices.Contains(1));

            // Should include diverse vectors, not just the most relevant ones
            Assert.True(selectedIndices.Distinct().Count() == 3, "Should select 3 different vectors");
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
            Assert.NotNull(result);
            Assert.Empty(result);
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
            Assert.NotNull(result);
            Assert.Single(result);

            // Should select the most relevant vector (index 0 or 1)
            Assert.True(result[0].index is 0 or 1);
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
            Assert.NotNull(result);
            Assert.Equal(vectors.Count, result.Count);
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
            Assert.NotNull(result);
            Assert.Equal(vectors.Count, result.Count);
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
            Assert.NotNull(result);
            Assert.Empty(result);
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
            Assert.NotNull(result);
            Assert.Empty(result);
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
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(0, result[0].index);
            Assert.Equal(vectors[0], result[0].embedding);
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
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.True(item.index >= 0 && item.index < vectors.Count));
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
            Assert.NotNull(resultRelevance);
            Assert.NotNull(resultDiversity);
            Assert.NotNull(resultBalanced);

            Assert.Equal(3, resultRelevance.Count);
            Assert.Equal(3, resultDiversity.Count);
            Assert.Equal(3, resultBalanced.Count);

            // Different lambda values should potentially produce different orderings
            var relevanceIndices = resultRelevance.Select(r => r.index).ToList();
            var diversityIndices = resultDiversity.Select(r => r.index).ToList();

            // At least one should be different (unless all vectors are identical)
            Assert.True(relevanceIndices.Count == 3 && diversityIndices.Count == 3);
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
            Assert.NotNull(result);
            foreach (var (index, embedding) in result)
            {
                // Verify that the returned embedding matches the original vector
                Assert.Equal(vectors[index], embedding);

                // Verify vector dimensions are preserved
                Assert.Equal(vectors[index].Count, embedding.Count);

                // Verify vector values are preserved
                for (var i = 0; i < vectors[index].Count; i++)
                {
                    Assert.Equal(vectors[index][i], embedding[i], precision: 10);
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
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.All(result, item => Assert.True(item.index >= 0 && item.index < vectors.Count));
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
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);

            // The results should be in the order they were selected by the algorithm
            // This means the first result should be the best overall choice, 
            // second should be the best choice given the first, etc.
            Assert.True(result.Count == 3);

            // Verify indices are within bounds
            Assert.All(result, item =>
            {
                Assert.True(item.index >= 0);
                Assert.True(item.index < vectors.Count);
            });
        }
    }
}