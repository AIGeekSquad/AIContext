using AiGeekSquad.AIContext.Ranking;
using AiGeekSquad.AIContext.Ranking.Normalizers;
using AiGeekSquad.AIContext.Tests.Ranking.TestUtilities;

using FluentAssertions;
using FluentAssertions.Execution;

namespace AiGeekSquad.AIContext.Tests.Ranking
{
    /// <summary>
    /// Unit tests for RankingEngine normalization strategies.
    /// </summary>
    public class RankingEngineNormalizationTests
    {
        [Fact]
        public void Rank_WithMinMaxNormalizer_ShouldNormalizeToZeroOneRange()
        {
            // Arrange
            var normalizer = new MinMaxNormalizer();
            var engine = new RankingEngine<Document>(normalizer);
            var documents = RankingTestHelpers.CreateTestDocuments();
            var scoringFunctions = new[] { RankingTestHelpers.CreateSemanticFunction() };

            // Act
            var results = engine.Rank(documents, scoringFunctions);

            // Assert
            var _ = new AssertionScope();
            results.Should().HaveCount(5);
            results.Should().BeInDescendingOrder(r => r.FinalScore);

            // MinMax normalization should produce scores in [0,1] range
            results.Should().AllSatisfy(r =>
            {
                r.FinalScore.Should().BeGreaterThanOrEqualTo(0.0);
                r.FinalScore.Should().BeLessThanOrEqualTo(1.0);
            });

            // Highest and lowest scores should be at the extremes
            Math.Abs(results.First().FinalScore - 1.0).Should().BeLessThan(0.01);
            Math.Abs(results.Last().FinalScore - 0.0).Should().BeLessThan(0.01);
        }

        [Fact]
        public void Rank_WithZScoreNormalizer_ShouldStandardizeScores()
        {
            // Arrange
            var normalizer = new ZScoreNormalizer();
            var engine = new RankingEngine<Document>(normalizer);
            var documents = RankingTestHelpers.CreateTestDocuments();
            var scoringFunctions = new[] { RankingTestHelpers.CreateSemanticFunction() };

            // Act
            var results = engine.Rank(documents, scoringFunctions);

            // Assert
            var _ = new AssertionScope();
            results.Should().HaveCount(5);
            results.Should().BeInDescendingOrder(r => r.FinalScore);

            // Z-score normalization can produce negative values
            results.Should().AllSatisfy(r => r.FinalScore.Should().NotBe(double.NaN));

            // The mean of normalized scores should be approximately 0
            var meanScore = results.Average(r => r.FinalScore);
            Math.Abs(meanScore - 0.0).Should().BeLessThan(0.1);
        }

        [Fact]
        public void Rank_WithPercentileNormalizer_ShouldRankByPercentiles()
        {
            // Arrange
            var normalizer = new PercentileNormalizer();
            var engine = new RankingEngine<Document>(normalizer);
            var documents = RankingTestHelpers.CreateTestDocuments();
            var scoringFunctions = new[] { RankingTestHelpers.CreateSemanticFunction() };

            // Act
            var results = engine.Rank(documents, scoringFunctions);

            // Assert
            var _ = new AssertionScope();
            results.Should().HaveCount(5);
            results.Should().BeInDescendingOrder(r => r.FinalScore);

            // Percentile normalization should produce valid rankings
            results.Should().AllSatisfy(r =>
            {
                r.FinalScore.Should().BeGreaterThanOrEqualTo(0.0);
                r.FinalScore.Should().BeLessThanOrEqualTo(1.0);
            });
        }

        [Fact]
        public void Rank_WithPerFunctionNormalizers_ShouldApplyCorrectNormalization()
        {
            // Arrange
            var engine = new RankingEngine<Document>();
            var documents = RankingTestHelpers.CreateTestDocuments();
            var scoringFunctions = new[]
            {
                RankingTestHelpers.CreateSemanticFunction(weight: 1.0, normalizer: new MinMaxNormalizer()),
                RankingTestHelpers.CreatePopularityFunction(weight: 1.0, normalizer: new ZScoreNormalizer())
            };

            // Act
            var results = engine.Rank(documents, scoringFunctions);

            // Assert
            var _ = new AssertionScope();
            results.Should().HaveCount(5);
            results.Should().AllSatisfy(r =>
            {
                r.FinalScore.Should().NotBe(double.NaN);
                r.IndividualScores.Should().ContainKey("SemanticRelevance");
                r.IndividualScores.Should().ContainKey("Popularity");
            });

            // Results should be properly ranked
            results.Should().BeInDescendingOrder(r => r.FinalScore);
        }

        [Fact]
        public void Rank_WithNullDefaultNormalizer_ShouldUseMinMaxNormalizer()
        {
            // Arrange
            var engine = new RankingEngine<Document>(defaultNormalizer: null);
            var documents = RankingTestHelpers.CreateTestDocuments();
            var scoringFunctions = new[]
            {
                new WeightedScoringFunction<Document>(new SemanticRelevanceScorer(), 1.0) { Normalizer = null }
            };

            // Act
            var results = engine.Rank(documents, scoringFunctions);

            // Assert
            var _ = new AssertionScope();
            results.Should().HaveCount(5);
            results.Should().BeInDescendingOrder(r => r.FinalScore);

            // Should work with default MinMax normalization
            results.Should().AllSatisfy(r =>
            {
                r.FinalScore.Should().BeGreaterThanOrEqualTo(0.0);
                r.FinalScore.Should().BeLessThanOrEqualTo(1.0);
            });
        }
    }
}