using AiGeekSquad.AIContext.Ranking;
using AiGeekSquad.AIContext.Ranking.Strategies;
using AiGeekSquad.AIContext.Tests.Ranking.TestUtilities;

using FluentAssertions;
using FluentAssertions.Execution;

namespace AiGeekSquad.AIContext.Tests.Ranking;

/// <summary>
/// Unit tests for RankingEngine ranking strategies (RRF and Hybrid).
/// </summary>
public class RankingEngineStrategyTests
{
    [Fact]
    public void Rank_WithReciprocalRankFusionStrategy_ShouldCombineRankings()
    {
        // Arrange
        var strategy = new ReciprocalRankFusionStrategy(k: 60);
        var engine = new RankingEngine<Document>(defaultStrategy: strategy);
        var documents = RankingTestHelpers.CreateTestDocuments();
        var scoringFunctions = new[]
        {
            RankingTestHelpers.CreateSemanticFunction(),
            RankingTestHelpers.CreatePopularityFunction()
        };

        // Act
        var results = engine.Rank(documents, scoringFunctions);

        // Assert
        var _ = new AssertionScope();
        results.Should().HaveCount(5);
        results.Should().BeInDescendingOrder(r => r.FinalScore);
        results.Should().AllSatisfy(r =>
        {
            r.FinalScore.Should().BeGreaterThan(0);
            r.Rank.Should().BeGreaterThan(0);
        });

        // RRF should produce different results than simple weighted sum
        results.First().FinalScore.Should().BeLessThan(2.0); // RRF scores are typically smaller
    }

    [Fact]
    public void Rank_WithDifferentRRFKValues_ShouldProduceDifferentResults()
    {
        // Arrange
        var documents = RankingTestHelpers.CreateTestDocuments();
        var scoringFunctions = new[]
        {
            RankingTestHelpers.CreateSemanticFunction(),
            RankingTestHelpers.CreatePopularityFunction()
        };

        var engine1 = new RankingEngine<Document>(defaultStrategy: new ReciprocalRankFusionStrategy(k: 10));
        var engine2 = new RankingEngine<Document>(defaultStrategy: new ReciprocalRankFusionStrategy(k: 100));

        // Act
        var results1 = engine1.Rank(documents, scoringFunctions);
        var results2 = engine2.Rank(documents, scoringFunctions);

        // Assert
        var _ = new AssertionScope();
        results1.Should().HaveCount(5);
        results2.Should().HaveCount(5);

        // Different K values should potentially produce different scores
        results1.Should().AllSatisfy(r => r.FinalScore.Should().BeGreaterThan(0));
        results2.Should().AllSatisfy(r => r.FinalScore.Should().BeGreaterThan(0));

        // At least some scores should be different due to different K values
        var scoresDiffer = results1.Zip(results2, (r1, r2) => Math.Abs(r1.FinalScore - r2.FinalScore) > 0.001).Any(x => x);
        scoresDiffer.Should().BeTrue("Different K values should produce different RRF scores");
    }

    [Fact]
    public void Rank_WithHybridStrategy_ShouldCombineWeightedSumAndRRF()
    {
        // Arrange
        var strategy = new HybridStrategy(alpha: 0.7, rrfK: 60);
        var engine = new RankingEngine<Document>(defaultStrategy: strategy);
        var documents = RankingTestHelpers.CreateTestDocuments();
        var scoringFunctions = new[]
        {
            RankingTestHelpers.CreateSemanticFunction(),
            RankingTestHelpers.CreatePopularityFunction(weight: 0.5)
        };

        // Act
        var results = engine.Rank(documents, scoringFunctions);

        // Assert
        var _ = new AssertionScope();
        results.Should().HaveCount(5);
        results.Should().BeInDescendingOrder(r => r.FinalScore);
        results.Should().AllSatisfy(r =>
        {
            r.FinalScore.Should().BeGreaterThan(0);
            r.Rank.Should().BeInRange(1, 5);
        });
    }

    [Fact]
    public void Rank_WithDifferentHybridAlphaValues_ShouldBalanceDifferently()
    {
        // Arrange
        var documents = RankingTestHelpers.CreateTestDocuments();
        var scoringFunctions = new[]
        {
            RankingTestHelpers.CreateSemanticFunction(),
            RankingTestHelpers.CreatePopularityFunction()
        };

        var engine1 = new RankingEngine<Document>(defaultStrategy: new HybridStrategy(alpha: 0.1)); // More RRF
        var engine2 = new RankingEngine<Document>(defaultStrategy: new HybridStrategy(alpha: 0.9)); // More WeightedSum

        // Act
        var results1 = engine1.Rank(documents, scoringFunctions);
        var results2 = engine2.Rank(documents, scoringFunctions);

        // Assert
        var _ = new AssertionScope();
        results1.Should().HaveCount(5);
        results2.Should().HaveCount(5);

        results1.Should().AllSatisfy(r => r.FinalScore.Should().BeGreaterThan(0));
        results2.Should().AllSatisfy(r => r.FinalScore.Should().BeGreaterThan(0));

        // Different alpha values should produce different score distributions
        var avgScore1 = results1.Average(r => r.FinalScore);
        var avgScore2 = results2.Average(r => r.FinalScore);
        Math.Abs(avgScore1 - avgScore2).Should().BeGreaterThan(0.01, "Different alpha values should produce different score distributions");
    }
}