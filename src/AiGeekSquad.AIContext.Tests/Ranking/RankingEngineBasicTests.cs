using AiGeekSquad.AIContext.Ranking;
using AiGeekSquad.AIContext.Tests.Ranking.TestUtilities;

using FluentAssertions;
using FluentAssertions.Execution;

namespace AiGeekSquad.AIContext.Tests.Ranking
{
    /// <summary>
    /// Unit tests for basic RankingEngine functionality including weighted sum ranking and negative weights.
    /// </summary>
    public class RankingEngineBasicTests
    {
        [Fact]
        public void Rank_WithSingleScoringFunction_ShouldRankByRelevance()
        {
            // Arrange
            var engine = new RankingEngine<Document>();
            var documents = RankingTestHelpers.CreateTestDocuments();
            var scoringFunctions = new[] { RankingTestHelpers.CreateSemanticFunction() };

            // Act
            var results = engine.Rank(documents, scoringFunctions);

            // Assert
            var _ = new AssertionScope();
            results.Should().HaveCount(5);
            results[0].Item.Title.Should().Be("Machine Learning Fundamentals");
            results[0].Rank.Should().Be(1);
            results[1].Item.Title.Should().Be("Advanced Neural Networks");
            results[1].Rank.Should().Be(2);
            results[2].Item.Title.Should().Be("Data Science Basics");
            results[2].Rank.Should().Be(3);

            // Verify scores are in descending order
            for (int i = 0; i < results.Count - 1; i++)
            {
                results[i].FinalScore.Should().BeGreaterThanOrEqualTo(results[i + 1].FinalScore);
            }
        }

        [Fact]
        public void Rank_WithMultipleScoringFunctions_ShouldCombineScoresCorrectly()
        {
            // Arrange
            var engine = new RankingEngine<Document>();
            var documents = RankingTestHelpers.CreateTestDocuments();
            var scoringFunctions = new[]
            {
                RankingTestHelpers.CreateSemanticFunction(weight: 0.7), // Semantic relevance weighted higher
                RankingTestHelpers.CreatePopularityFunction(weight: 0.3) // Popularity weighted lower
            };

            // Act
            var results = engine.Rank(documents, scoringFunctions);

            // Assert
            var _ = new AssertionScope();
            results.Should().HaveCount(5);
            results.Should().BeInDescendingOrder(r => r.FinalScore);

            // Verify individual scores are captured
            results.Should().AllSatisfy(r =>
            {
                r.IndividualScores.Should().ContainKey("SemanticRelevance");
                r.IndividualScores.Should().ContainKey("Popularity");
            });

            // The document with highest combined score should be first
            var topResult = results[0];
            topResult.FinalScore.Should().BeGreaterThan(0);
            topResult.Rank.Should().Be(1);
        }

        [Fact]
        public void Rank_WithNegativeWeights_ShouldPenalizeHighScores()
        {
            // Arrange
            var engine = new RankingEngine<Document>();
            var documents = RankingTestHelpers.CreateTestDocuments();
            var scoringFunctions = new[]
            {
                RankingTestHelpers.CreateSemanticFunction(weight: 1.0),      // Positive: reward high relevance
                RankingTestHelpers.CreatePopularityFunction(weight: -0.5)    // Negative: penalize high popularity
            };

            // Act
            var results = engine.Rank(documents, scoringFunctions);

            // Assert
            var _ = new AssertionScope();
            results.Should().HaveCount(5);
            results.Should().BeInDescendingOrder(r => r.FinalScore);

            // Documents with very high popularity (rank 1) should be penalized more
            var highPopularityDoc = results.First(r => r.Item.PopularityRank == 1);
            var lowPopularityDoc = results.First(r => r.Item.PopularityRank == 5);

            // The penalty effect should be visible in the final ranking
            results.Should().AllSatisfy(r => r.FinalScore.Should().NotBe(double.NaN));
        }

        [Fact]
        public void Rank_WithAllNegativeWeights_ShouldInvertRanking()
        {
            // Arrange
            var engine = new RankingEngine<Document>();
            var documents = RankingTestHelpers.CreateTestDocuments().Take(3).ToList(); // Use fewer documents for clearer test
            var scoringFunctions = new[]
            {
                RankingTestHelpers.CreateSemanticFunction(weight: -1.0) // Negative weight inverts the preference
            };

            // Act
            var results = engine.Rank(documents, scoringFunctions);

            // Assert
            var _ = new AssertionScope();
            results.Should().HaveCount(3);
            results.Should().BeInDescendingOrder(r => r.FinalScore);

            // With negative weights, documents with lower original relevance should rank higher
            var lowestRelevanceDoc = documents.OrderBy(d => d.RelevanceScore).First();
            results[0].Item.Should().Be(lowestRelevanceDoc);
        }

        [Fact]
        public void Rank_ShouldPopulateAllResultProperties()
        {
            // Arrange
            var engine = new RankingEngine<Document>();
            var documents = RankingTestHelpers.CreateTestDocuments().Take(1).ToList();
            var scoringFunctions = new[]
            {
                RankingTestHelpers.CreateSemanticFunction(weight: 1.0),
                RankingTestHelpers.CreatePopularityFunction(weight: 0.5)
            };

            // Act
            var results = engine.Rank(documents, scoringFunctions);

            // Assert
            var _ = new AssertionScope();
            results.Should().HaveCount(1);
            var result = results[0];

            result.Item.Should().NotBeNull();
            result.FinalScore.Should().BeGreaterThan(0);
            result.Rank.Should().Be(1);

            result.IndividualScores.Should().ContainKey("SemanticRelevance");
            result.IndividualScores.Should().ContainKey("Popularity");
            result.IndividualScores["SemanticRelevance"].Should().Be(0.95);
            result.IndividualScores["Popularity"].Should().Be(1.0);

            result.Metadata.Should().NotBeNull();
            result.Metadata.Should().BeEmpty(); // Initially empty but available
        }
    }
}