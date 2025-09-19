using System;
using System.Linq;
using AiGeekSquad.AIContext.Ranking.Normalizers;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace AiGeekSquad.AIContext.Tests.Ranking
{
    /// <summary>
    /// Unit tests for normalizer edge cases to improve branch coverage.
    /// </summary>
    public class NormalizerEdgeCaseTests
    {
        // Test constants to avoid magic numbers
        private const double TestValue1 = 1.0;
        private const double TestValue2 = 2.0;
        private const double TestValue3 = 3.0;
        private const double TestValue4 = 4.0;
        private const double TestValue5 = 5.0;
        private const double RepeatedValue = 5.0;
        private const double SingleTestValue = 42.0;
        private const double NormalizedHalf = 0.5;
        private const double SmallTolerance = 0.001;
        private const double LargeNegativeValue = -1000.0;
        private const double LargePositiveValue = 1000.0;
        private const double LargeValue2 = 20.0;
        private const double ExpectedMean = 3.0;
        private const double ExpectedStdDev = 1.5811388300841898; // sqrt(2.5)

        #region MinMaxNormalizer Tests

        [Fact]
        public void MinMaxNormalizer_WithIdenticalScores_ReturnsHalfValues()
        {
            // Arrange
            var normalizer = new MinMaxNormalizer();
            var scores = new[] { RepeatedValue, RepeatedValue, RepeatedValue, RepeatedValue };

            // Act
            var result = normalizer.Normalize(scores);

            // Assert
            result.Should().OnlyContain(x => x == NormalizedHalf, "when all scores are identical, MinMax normalization should return 0.5");
        }

        [Fact]
        public void MinMaxNormalizer_WithSingleScore_ReturnsHalf()
        {
            // Arrange
            var normalizer = new MinMaxNormalizer();
            var scores = new[] { SingleTestValue };

            // Act
            var result = normalizer.Normalize(scores);

            // Assert
            result.Should().HaveCount(1);
            result[0].Should().Be(NormalizedHalf, "single score should normalize to 0.5");
        }

        [Fact]
        public void MinMaxNormalizer_WithEmptyScores_ReturnsEmpty()
        {
            // Arrange
            var normalizer = new MinMaxNormalizer();
            var scores = Array.Empty<double>();

            // Act
            var result = normalizer.Normalize(scores);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void MinMaxNormalizer_WithNormalRange_NormalizesCorrectly()
        {
            // Arrange
            var normalizer = new MinMaxNormalizer();
            var scores = new[] { TestValue1, TestValue3, TestValue5, 7.0, 9.0 };

            // Act
            var result = normalizer.Normalize(scores);

            // Assert
            using var _ = new AssertionScope();
            result.Should().HaveCount(5);
            result[0].Should().BeApproximately(0.0, SmallTolerance); // Min value -> 0
            result[4].Should().BeApproximately(1.0, SmallTolerance); // Max value -> 1
            result[2].Should().BeApproximately(NormalizedHalf, SmallTolerance); // Middle value -> 0.5
            result.Should().OnlyContain(x => x >= 0.0 && x <= 1.0);
        }

        #endregion

        #region ZScoreNormalizer Tests

        [Fact]
        public void ZScoreNormalizer_WithIdenticalScores_ReturnsZeros()
        {
            // Arrange
            var normalizer = new ZScoreNormalizer();
            var scores = new[] { TestValue3, TestValue3, TestValue3, TestValue3 };

            // Act
            var result = normalizer.Normalize(scores);

            // Assert
            result.Should().OnlyContain(x => x == 0.0, "when all scores are identical, Z-score normalization should return 0");
        }

        [Fact]
        public void ZScoreNormalizer_WithSingleScore_ReturnsZero()
        {
            // Arrange
            var normalizer = new ZScoreNormalizer();
            var scores = new[] { SingleTestValue };

            // Act
            var result = normalizer.Normalize(scores);

            // Assert
            result.Should().HaveCount(1);
            result[0].Should().Be(0.0, "single score should normalize to 0");
        }

        [Fact]
        public void ZScoreNormalizer_WithEmptyScores_ReturnsEmpty()
        {
            // Arrange
            var normalizer = new ZScoreNormalizer();
            var scores = Array.Empty<double>();

            // Act
            var result = normalizer.Normalize(scores);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ZScoreNormalizer_WithNormalDistribution_NormalizesCorrectly()
        {
            // Arrange
            var normalizer = new ZScoreNormalizer();
            var scores = new[] { TestValue1, TestValue2, TestValue3, TestValue4, TestValue5 }; // Mean = 3.0, StdDev = sqrt(2.5)

            // Act
            var result = normalizer.Normalize(scores);

            // Assert
            using var _ = new AssertionScope();
            result.Should().HaveCount(5);
            
            // Mean should be approximately 0
            var mean = result.Average();
            mean.Should().BeApproximately(0.0, SmallTolerance);
            
            // Standard deviation should be approximately 1
            var variance = result.Select(x => Math.Pow(x - mean, 2)).Average();
            var stdDev = Math.Sqrt(variance);
            stdDev.Should().BeApproximately(1.0, SmallTolerance);
        }

        #endregion

        #region PercentileNormalizer Tests

        [Fact]
        public void PercentileNormalizer_WithIdenticalScores_ReturnsZeros()
        {
            // Arrange
            var normalizer = new PercentileNormalizer();
            var scores = new[] { 7.0, 7.0, 7.0, 7.0 };

            // Act
            var result = normalizer.Normalize(scores);

            // Assert
            result.Should().OnlyContain(x => x == 0.0, "when all scores are identical, percentile normalization should return 0");
        }

        [Fact]
        public void PercentileNormalizer_WithSingleScore_ReturnsHalf()
        {
            // Arrange
            var normalizer = new PercentileNormalizer();
            var scores = new[] { SingleTestValue };

            // Act
            var result = normalizer.Normalize(scores);

            // Assert
            result.Should().HaveCount(1);
            result[0].Should().Be(NormalizedHalf, "single score should normalize to 0.5");
        }

        [Fact]
        public void PercentileNormalizer_WithEmptyScores_ReturnsEmpty()
        {
            // Arrange
            var normalizer = new PercentileNormalizer();
            var scores = Array.Empty<double>();

            // Act
            var result = normalizer.Normalize(scores);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void PercentileNormalizer_WithDistinctScores_NormalizesCorrectly()
        {
            // Arrange
            var normalizer = new PercentileNormalizer();
            var scores = new[] { TestValue1, TestValue5, 10.0, 15.0, LargeValue2 };

            // Act
            var result = normalizer.Normalize(scores);

            // Assert
            using var _ = new AssertionScope();
            result.Should().HaveCount(5);
            result.Should().OnlyContain(x => x >= 0.0 && x <= 1.0);
            
            // Should be ordered by percentile rank
            result.Should().BeInAscendingOrder();
        }

        [Fact]
        public void PercentileNormalizer_WithDuplicateScores_HandlesCorrectly()
        {
            // Arrange
            var normalizer = new PercentileNormalizer();
            var scores = new[] { TestValue1, TestValue2, TestValue2, TestValue2, TestValue5 }; // Multiple duplicates

            // Act
            var result = normalizer.Normalize(scores);

            // Assert
            using var _ = new AssertionScope();
            result.Should().HaveCount(5);
            result.Should().OnlyContain(x => x >= 0.0 && x <= 1.0);
            
            // Duplicates should have the same percentile rank
            result[1].Should().Be(result[2]);
            result[2].Should().Be(result[3]);
        }

        [Fact]
        public void PercentileNormalizer_WithExtremeValues_HandlesCorrectly()
        {
            // Arrange
            var normalizer = new PercentileNormalizer();
            var scores = new[] { double.MinValue, LargeNegativeValue, 0.0, LargePositiveValue, double.MaxValue };

            // Act
            var result = normalizer.Normalize(scores);

            // Assert
            using var _ = new AssertionScope();
            result.Should().HaveCount(5);
            result.Should().OnlyContain(x => x >= 0.0 && x <= 1.0);
            result.Should().BeInAscendingOrder();
        }

        #endregion

        #region Cross-Normalizer Consistency Tests

        [Fact]
        public void AllNormalizers_WithSameInput_ProduceDifferentOutputs()
        {
            // Arrange
            var minMaxNormalizer = new MinMaxNormalizer();
            var zScoreNormalizer = new ZScoreNormalizer();
            var percentileNormalizer = new PercentileNormalizer();
            var scores = new[] { TestValue1, TestValue3, 7.0, 12.0, LargeValue2 };

            // Act
            var minMaxResult = minMaxNormalizer.Normalize(scores);
            var zScoreResult = zScoreNormalizer.Normalize(scores);
            var percentileResult = percentileNormalizer.Normalize(scores);

            // Assert
            using var _ = new AssertionScope();
            minMaxResult.Should().NotEqual(zScoreResult, "MinMax and Z-Score should produce different results");
            minMaxResult.Should().NotEqual(percentileResult, "MinMax and Percentile should produce different results");
            zScoreResult.Should().NotEqual(percentileResult, "Z-Score and Percentile should produce different results");
        }

        [Fact]
        public void AllNormalizers_PreserveOrdering()
        {
            // Arrange
            var minMaxNormalizer = new MinMaxNormalizer();
            var zScoreNormalizer = new ZScoreNormalizer();
            var percentileNormalizer = new PercentileNormalizer();
            var scores = new[] { TestValue5, TestValue1, 8.0, TestValue3, 12.0 };

            // Act
            var minMaxResult = minMaxNormalizer.Normalize(scores);
            var zScoreResult = zScoreNormalizer.Normalize(scores);
            var percentileResult = percentileNormalizer.Normalize(scores);

            // Assert
            using var _ = new AssertionScope();
            
            // All normalizers should preserve the relative ordering
            var originalOrder = scores.Select((score, index) => new { score, index })
                                    .OrderBy(x => x.score)
                                    .Select(x => x.index)
                                    .ToArray();

            var minMaxOrder = minMaxResult.Select((score, index) => new { score, index })
                                         .OrderBy(x => x.score)
                                         .Select(x => x.index)
                                         .ToArray();

            var zScoreOrder = zScoreResult.Select((score, index) => new { score, index })
                                         .OrderBy(x => x.score)
                                         .Select(x => x.index)
                                         .ToArray();

            var percentileOrder = percentileResult.Select((score, index) => new { score, index })
                                                 .OrderBy(x => x.score)
                                                 .Select(x => x.index)
                                                 .ToArray();

            minMaxOrder.Should().Equal(originalOrder, "MinMax normalizer should preserve ordering");
            zScoreOrder.Should().Equal(originalOrder, "Z-Score normalizer should preserve ordering");
            percentileOrder.Should().Equal(originalOrder, "Percentile normalizer should preserve ordering");
        }

        #endregion
    }
}