using AiGeekSquad.AIContext.Ranking;
using AiGeekSquad.AIContext.Tests.Ranking.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace AiGeekSquad.AIContext.Tests.Ranking
{
    /// <summary>
    /// Unit tests for RankingEngine Top-K functionality and edge cases.
    /// </summary>
    public class RankingEngineEdgeCasesTests
    {
        #region Top-K Ranking Tests

        [Fact]
        public void RankTopK_WithValidK_ShouldReturnTopResults()
        {
            // Arrange
            var engine = new RankingEngine<Document>();
            var documents = RankingTestHelpers.CreateTestDocuments();
            var scoringFunctions = new[] { RankingTestHelpers.CreateSemanticFunction() };

            // Act
            var results = engine.RankTopK(documents, scoringFunctions, k: 3);

            // Assert
            var _ = new AssertionScope();
            results.Should().HaveCount(3);
            results.Should().BeInDescendingOrder(r => r.FinalScore);
            
            results[0].Item.Title.Should().Be("Machine Learning Fundamentals");
            results[0].Rank.Should().Be(1);
            results[1].Item.Title.Should().Be("Advanced Neural Networks");
            results[1].Rank.Should().Be(2);
            results[2].Item.Title.Should().Be("Data Science Basics");
            results[2].Rank.Should().Be(3);
        }

        [Fact]
        public void RankTopK_WithKLargerThanItemCount_ShouldReturnAllItems()
        {
            // Arrange
            var engine = new RankingEngine<Document>();
            var documents = RankingTestHelpers.CreateTestDocuments().Take(2).ToList();
            var scoringFunctions = new[] { RankingTestHelpers.CreateSemanticFunction() };

            // Act
            var results = engine.RankTopK(documents, scoringFunctions, k: 5);

            // Assert
            var _ = new AssertionScope();
            results.Should().HaveCount(2); // Only 2 documents available
            results.Should().BeInDescendingOrder(r => r.FinalScore);
            results.Should().AllSatisfy(r => r.Rank.Should().BeInRange(1, 2));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void RankTopK_WithInvalidK_ShouldReturnEmptyList(int k)
        {
            // Arrange
            var engine = new RankingEngine<Document>();
            var documents = RankingTestHelpers.CreateTestDocuments();
            var scoringFunctions = new[] { RankingTestHelpers.CreateSemanticFunction() };

            // Act
            var results = engine.RankTopK(documents, scoringFunctions, k: k);

            // Assert
            results.Should().BeEmpty();
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public void Rank_WithEmptyItemList_ShouldReturnEmptyResults()
        {
            // Arrange
            var engine = new RankingEngine<Document>();
            var documents = new List<Document>();
            var scoringFunctions = new[] { RankingTestHelpers.CreateSemanticFunction() };

            // Act
            var results = engine.Rank(documents, scoringFunctions);

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public void Rank_WithNullItemList_ShouldReturnEmptyResults()
        {
            // Arrange
            var engine = new RankingEngine<Document>();
            var scoringFunctions = new[] { RankingTestHelpers.CreateSemanticFunction() };

            // Act
            var results = engine.Rank(null!, scoringFunctions);

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public void Rank_WithNullScoringFunctions_ShouldThrowArgumentException()
        {
            // Arrange
            var engine = new RankingEngine<Document>();
            var documents = RankingTestHelpers.CreateTestDocuments();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => engine.Rank(documents, null!));
            exception.Message.Should().Contain("At least one scoring function is required");
        }

        [Fact]
        public void Rank_WithEmptyScoringFunctions_ShouldThrowArgumentException()
        {
            // Arrange
            var engine = new RankingEngine<Document>();
            var documents = RankingTestHelpers.CreateTestDocuments();
            var emptyScoringFunctions = new WeightedScoringFunction<Document>[0];

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => engine.Rank(documents, emptyScoringFunctions));
            exception.Message.Should().Contain("At least one scoring function is required");
        }

        [Fact]
        public void Rank_WithSingleDocument_ShouldReturnSingleRankedResult()
        {
            // Arrange
            var engine = new RankingEngine<Document>();
            var documents = RankingTestHelpers.CreateTestDocuments().Take(1).ToList();
            var scoringFunctions = new[] { RankingTestHelpers.CreateSemanticFunction() };

            // Act
            var results = engine.Rank(documents, scoringFunctions);

            // Assert
            var _ = new AssertionScope();
            results.Should().HaveCount(1);
            results[0].Item.Title.Should().Be("Machine Learning Fundamentals");
            results[0].Rank.Should().Be(1);
            results[0].FinalScore.Should().BeGreaterThan(0);
            results[0].IndividualScores.Should().ContainKey("SemanticRelevance");
        }

        [Fact]
        public void Rank_WithEqualScores_ShouldHandleGracefully()
        {
            // Arrange
            var engine = new RankingEngine<Document>();
            var documents = RankingTestHelpers.CreateTestDocuments().Take(3).ToList();
            var scoringFunctions = new[]
            {
                new WeightedScoringFunction<Document>(new ConstantScorer(0.8), 1.0)
            };

            // Act
            var results = engine.Rank(documents, scoringFunctions);

            // Assert
            var _ = new AssertionScope();
            results.Should().HaveCount(3);
            
            // All documents should have equal final scores due to constant scoring
            var firstScore = results[0].FinalScore;
            results.Should().AllSatisfy(r => 
                Math.Abs(r.FinalScore - firstScore).Should().BeLessThan(0.001));
            
            // Ranks should still be assigned sequentially
            results[0].Rank.Should().Be(1);
            results[1].Rank.Should().Be(2);
            results[2].Rank.Should().Be(3);
        }

        #endregion
    }
}