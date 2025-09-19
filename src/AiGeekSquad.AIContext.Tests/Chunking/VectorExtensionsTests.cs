using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MathNet.Numerics.LinearAlgebra;
using Xunit;
using AiGeekSquad.AIContext.Chunking;

// Alias to avoid naming conflicts with MathNet.Numerics.LinearAlgebra.VectorExtensions
using VectorUtils = AiGeekSquad.AIContext.Chunking.VectorExtensions;

namespace AiGeekSquad.AIContext.Tests.Chunking
{
    public class VectorExtensionsTests
    {
        // Test constants to avoid magic numbers
        private const double DefaultPercentile = 0.5;
        private const double SmallTolerance = 0.001;
        private const double TestValue1 = 1.0;
        private const double TestValue2 = 2.0;
        private const double TestValue3 = 3.0;
        private const double TestValue4 = 4.0;
        private const double TestValue5 = 5.0;
        private const double TestThreshold = 0.5;
        private const double TestSingleValue = 42.0;
        private const string DistancesParameterName = "distances";
        private const string PercentileParameterName = "percentile";
        private const string ValuesParameterName = "values";
        private const string TestParameterName = "testParam";

        #region CalculatePercentile Tests

        [Fact]
        public void CalculatePercentile_WithNullDistances_ThrowsArgumentNullException()
        {
            // Act & Assert
            var act = () => VectorUtils.CalculatePercentile(null!, DefaultPercentile);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName(DistancesParameterName);
        }

        [Theory]
        [InlineData(-0.1)]
        [InlineData(1.1)]
        public void CalculatePercentile_WithInvalidPercentile_ThrowsArgumentException(double percentile)
        {
            // Arrange
            var distances = new[] { TestValue1, TestValue2, TestValue3 };

            // Act & Assert
            var act = () => VectorUtils.CalculatePercentile(distances, percentile);
            act.Should().Throw<ArgumentException>()
                .WithParameterName(PercentileParameterName)
                .WithMessage("Percentile must be between 0.0 and 1.0.*");
        }

        [Fact]
        public void CalculatePercentile_WithEmptyDistances_ReturnsZero()
        {
            // Arrange
            var distances = Array.Empty<double>();

            // Act
            var result = VectorUtils.CalculatePercentile(distances, DefaultPercentile);

            // Assert
            result.Should().Be(0.0);
        }

        [Fact]
        public void CalculatePercentile_WithSingleDistance_ReturnsThatValue()
        {
            // Arrange
            var distances = new[] { TestValue5 };

            // Act
            var result = VectorUtils.CalculatePercentile(distances, DefaultPercentile);

            // Assert
            result.Should().Be(TestValue5);
        }

        [Fact]
        public void CalculatePercentile_WithNaNAndInfinityValues_FiltersThemOut()
        {
            // Arrange
            var distances = new[] { TestValue1, double.NaN, TestValue2, double.PositiveInfinity, TestValue3, double.NegativeInfinity };

            // Act
            var result = VectorUtils.CalculatePercentile(distances, DefaultPercentile);

            // Assert
            result.Should().Be(TestValue2); // Median of [1.0, 2.0, 3.0]
        }

        [Theory]
        [InlineData(0.0, 1.0)]
        [InlineData(0.5, 2.5)]
        [InlineData(1.0, 4.0)]
        public void CalculatePercentile_WithMultipleValues_ReturnsCorrectPercentile(double percentile, double expected)
        {
            // Arrange
            var distances = new[] { TestValue1, TestValue2, TestValue3, TestValue4 };

            // Act
            var result = VectorUtils.CalculatePercentile(distances, percentile);

            // Assert
            result.Should().BeApproximately(expected, SmallTolerance);
        }

        [Fact]
        public void CalculatePercentile_WithUnsortedValues_SortsThem()
        {
            // Arrange
            var distances = new[] { TestValue4, TestValue1, TestValue3, TestValue2 };

            // Act
            var result = VectorUtils.CalculatePercentile(distances, DefaultPercentile);

            // Assert
            result.Should().BeApproximately(2.5, SmallTolerance); // Median of sorted [1.0, 2.0, 3.0, 4.0]
        }

        #endregion

        #region FindBreakpoints Tests

        [Fact]
        public void FindBreakpoints_WithNullDistances_ThrowsArgumentNullException()
        {
            // Act & Assert
            var act = () => VectorUtils.FindBreakpoints(null!, TestValue1);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName(DistancesParameterName);
        }

        [Fact]
        public void FindBreakpoints_WithEmptyDistances_ReturnsEmptyCollection()
        {
            // Arrange
            var distances = Array.Empty<double>();

            // Act
            var result = VectorUtils.FindBreakpoints(distances, TestValue1);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void FindBreakpoints_WithNoValuesAboveThreshold_ReturnsEmptyCollection()
        {
            // Arrange
            var distances = new[] { 0.1, 0.2, 0.3, 0.4 };

            // Act
            var result = VectorUtils.FindBreakpoints(distances, TestThreshold);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void FindBreakpoints_WithValuesAboveThreshold_ReturnsCorrectIndices()
        {
            // Arrange
            var distances = new[] { 0.1, 0.8, 0.3, 0.9, 0.2 };

            // Act
            var result = VectorUtils.FindBreakpoints(distances, TestThreshold);

            // Assert
            result.Should().Equal(1, 3); // Indices where values 0.8 and 0.9 are located
        }

        [Fact]
        public void FindBreakpoints_WithNaNAndInfinityValues_SkipsThem()
        {
            // Arrange
            var distances = new[] { 0.1, double.NaN, 0.8, double.PositiveInfinity, 0.9 };

            // Act
            var result = VectorUtils.FindBreakpoints(distances, TestThreshold);

            // Assert
            result.Should().Equal(2, 4); // Indices where values 0.8 and 0.9 are located
        }

        [Fact]
        public void FindBreakpoints_WithExactThresholdValue_IncludesIt()
        {
            // Arrange
            var distances = new[] { 0.1, TestThreshold, 0.3, TestThreshold, 0.2 };

            // Act
            var result = VectorUtils.FindBreakpoints(distances, TestThreshold);

            // Assert
            result.Should().Equal(1, 3); // Both exact matches included
        }

        #endregion

        #region CreateVector Tests

        [Fact]
        public void CreateVector_WithNullArray_ThrowsArgumentNullException()
        {
            // Act & Assert
            var act = () => VectorUtils.CreateVector((double[])null!);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName(ValuesParameterName);
        }

        [Fact]
        public void CreateVector_WithNullEnumerable_ThrowsArgumentNullException()
        {
            // Act & Assert
            var act = () => VectorUtils.CreateVector((IEnumerable<double>)null!);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName(ValuesParameterName);
        }

        [Fact]
        public void CreateVector_WithEmptyArray_ReturnsEmptyVector()
        {
            // Arrange
            var values = Array.Empty<double>();

            // Act
            var result = VectorUtils.CreateVector(values);

            // Assert
            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Fact]
        public void CreateVector_WithValidArray_ReturnsCorrectVector()
        {
            // Arrange
            var values = new[] { TestValue1, TestValue2, TestValue3, TestValue4 };

            // Act
            var result = VectorUtils.CreateVector(values);

            // Assert
            result.Should().NotBeNull();
            result.Count.Should().Be(4);
            result.ToArray().Should().Equal(values);
        }

        [Fact]
        public void CreateVector_WithValidEnumerable_ReturnsCorrectVector()
        {
            // Arrange
            var values = new List<double> { TestValue1, TestValue2, TestValue3, TestValue4 };

            // Act
            var result = VectorUtils.CreateVector(values);

            // Assert
            result.Should().NotBeNull();
            result.Count.Should().Be(4);
            result.ToArray().Should().Equal(values.ToArray());
        }

        #endregion

        #region ValidateVector Tests

        [Fact]
        public void ValidateVector_WithNullVector_ThrowsArgumentNullException()
        {
            // Act & Assert
            var act = () => VectorUtils.ValidateVector(null!, TestParameterName);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName(TestParameterName);
        }

        [Fact]
        public void ValidateVector_WithZeroDimensionVector_ThrowsArgumentException()
        {
            // Arrange
            var vector = Vector<double>.Build.Dense(0);

            // Act & Assert
            var act = () => VectorUtils.ValidateVector(vector, TestParameterName);
            act.Should().Throw<ArgumentException>()
                .WithParameterName(TestParameterName)
                .WithMessage("Vector cannot have zero dimension.*");
        }

        [Fact]
        public void ValidateVector_WithValidVector_DoesNotThrow()
        {
            // Arrange
            var vector = Vector<double>.Build.Dense(new[] { TestValue1, TestValue2, TestValue3 });

            // Act & Assert
            var act = () => VectorUtils.ValidateVector(vector, TestParameterName);
            act.Should().NotThrow();
        }

        #endregion

        #region CalculateDistanceStatistics Tests

        [Fact]
        public void CalculateDistanceStatistics_WithNullDistances_ReturnsZeroStatistics()
        {
            // Act
            var result = VectorUtils.CalculateDistanceStatistics(null!);

            // Assert
            result.Should().Be((0.0, 0.0, 0.0, 0.0));
        }

        [Fact]
        public void CalculateDistanceStatistics_WithEmptyDistances_ReturnsZeroStatistics()
        {
            // Arrange
            var distances = Array.Empty<double>();

            // Act
            var result = VectorUtils.CalculateDistanceStatistics(distances);

            // Assert
            result.Should().Be((0.0, 0.0, 0.0, 0.0));
        }

        [Fact]
        public void CalculateDistanceStatistics_WithNaNAndInfinityValues_FiltersThemOut()
        {
            // Arrange
            var distances = new[] { TestValue1, double.NaN, TestValue2, double.PositiveInfinity, TestValue3, double.NegativeInfinity };

            // Act
            var result = VectorUtils.CalculateDistanceStatistics(distances);

            // Assert
            result.mean.Should().BeApproximately(TestValue2, SmallTolerance);
            result.min.Should().Be(TestValue1);
            result.max.Should().Be(TestValue3);
            result.standardDeviation.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CalculateDistanceStatistics_WithValidDistances_ReturnsCorrectStatistics()
        {
            // Arrange
            var distances = new[] { TestValue1, TestValue2, TestValue3, TestValue4, TestValue5 };

            // Act
            var result = VectorUtils.CalculateDistanceStatistics(distances);

            // Assert
            result.mean.Should().BeApproximately(3.0, SmallTolerance);
            result.min.Should().Be(TestValue1);
            result.max.Should().Be(TestValue5);
            result.standardDeviation.Should().BeApproximately(1.5811388300841898, SmallTolerance);
        }

        [Fact]
        public void CalculateDistanceStatistics_WithOnlyInvalidValues_ReturnsZeroStatistics()
        {
            // Arrange
            var distances = new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity };

            // Act
            var result = VectorUtils.CalculateDistanceStatistics(distances);

            // Assert
            result.Should().Be((0.0, 0.0, 0.0, 0.0));
        }

        #endregion
    }
}