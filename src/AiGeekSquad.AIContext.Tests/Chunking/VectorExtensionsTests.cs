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
        #region CalculatePercentile Tests

        [Fact]
        public void CalculatePercentile_WithNullDistances_ThrowsArgumentNullException()
        {
            // Act & Assert
            var act = () => VectorUtils.CalculatePercentile(null!, 0.5);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("distances");
        }

        [Theory]
        [InlineData(-0.1)]
        [InlineData(1.1)]
        public void CalculatePercentile_WithInvalidPercentile_ThrowsArgumentException(double percentile)
        {
            // Arrange
            var distances = new[] { 1.0, 2.0, 3.0 };

            // Act & Assert
            var act = () => VectorUtils.CalculatePercentile(distances, percentile);
            act.Should().Throw<ArgumentException>()
                .WithParameterName("percentile")
                .WithMessage("Percentile must be between 0.0 and 1.0.*");
        }

        [Fact]
        public void CalculatePercentile_WithEmptyDistances_ReturnsZero()
        {
            // Arrange
            var distances = Array.Empty<double>();

            // Act
            var result = VectorUtils.CalculatePercentile(distances, 0.5);

            // Assert
            result.Should().Be(0.0);
        }

        [Fact]
        public void CalculatePercentile_WithSingleDistance_ReturnsThatValue()
        {
            // Arrange
            var distances = new[] { 5.0 };

            // Act
            var result = VectorUtils.CalculatePercentile(distances, 0.5);

            // Assert
            result.Should().Be(5.0);
        }

        [Fact]
        public void CalculatePercentile_WithNaNAndInfinityValues_FiltersThemOut()
        {
            // Arrange
            var distances = new[] { 1.0, double.NaN, 2.0, double.PositiveInfinity, 3.0, double.NegativeInfinity };

            // Act
            var result = VectorUtils.CalculatePercentile(distances, 0.5);

            // Assert
            result.Should().Be(2.0); // Median of [1.0, 2.0, 3.0]
        }

        [Theory]
        [InlineData(0.0, 1.0)]
        [InlineData(0.5, 2.5)]
        [InlineData(1.0, 4.0)]
        public void CalculatePercentile_WithMultipleValues_ReturnsCorrectPercentile(double percentile, double expected)
        {
            // Arrange
            var distances = new[] { 1.0, 2.0, 3.0, 4.0 };

            // Act
            var result = VectorUtils.CalculatePercentile(distances, percentile);

            // Assert
            result.Should().BeApproximately(expected, 0.001);
        }

        [Fact]
        public void CalculatePercentile_WithUnsortedValues_SortsThem()
        {
            // Arrange
            var distances = new[] { 4.0, 1.0, 3.0, 2.0 };

            // Act
            var result = VectorUtils.CalculatePercentile(distances, 0.5);

            // Assert
            result.Should().BeApproximately(2.5, 0.001); // Median of sorted [1.0, 2.0, 3.0, 4.0]
        }

        #endregion

        #region FindBreakpoints Tests

        [Fact]
        public void FindBreakpoints_WithNullDistances_ThrowsArgumentNullException()
        {
            // Act & Assert
            var act = () => VectorUtils.FindBreakpoints(null!, 1.0);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("distances");
        }

        [Fact]
        public void FindBreakpoints_WithEmptyDistances_ReturnsEmptyCollection()
        {
            // Arrange
            var distances = Array.Empty<double>();

            // Act
            var result = VectorUtils.FindBreakpoints(distances, 1.0);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void FindBreakpoints_WithNoValuesAboveThreshold_ReturnsEmptyCollection()
        {
            // Arrange
            var distances = new[] { 0.1, 0.2, 0.3, 0.4 };
            var threshold = 0.5;

            // Act
            var result = VectorUtils.FindBreakpoints(distances, threshold);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void FindBreakpoints_WithValuesAboveThreshold_ReturnsCorrectIndices()
        {
            // Arrange
            var distances = new[] { 0.1, 0.8, 0.3, 0.9, 0.2 };
            var threshold = 0.5;

            // Act
            var result = VectorUtils.FindBreakpoints(distances, threshold);

            // Assert
            result.Should().Equal(1, 3); // Indices where values 0.8 and 0.9 are located
        }

        [Fact]
        public void FindBreakpoints_WithNaNAndInfinityValues_SkipsThem()
        {
            // Arrange
            var distances = new[] { 0.1, double.NaN, 0.8, double.PositiveInfinity, 0.9 };
            var threshold = 0.5;

            // Act
            var result = VectorUtils.FindBreakpoints(distances, threshold);

            // Assert
            result.Should().Equal(2, 4); // Indices where values 0.8 and 0.9 are located
        }

        [Fact]
        public void FindBreakpoints_WithExactThresholdValue_IncludesIt()
        {
            // Arrange
            var distances = new[] { 0.1, 0.5, 0.3, 0.5, 0.2 };
            var threshold = 0.5;

            // Act
            var result = VectorUtils.FindBreakpoints(distances, threshold);

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
                .WithParameterName("values");
        }

        [Fact]
        public void CreateVector_WithNullEnumerable_ThrowsArgumentNullException()
        {
            // Act & Assert
            var act = () => VectorUtils.CreateVector((IEnumerable<double>)null!);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("values");
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
            var values = new[] { 1.0, 2.0, 3.0, 4.0 };

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
            var values = new List<double> { 1.0, 2.0, 3.0, 4.0 };

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
            var act = () => VectorUtils.ValidateVector(null!, "testParam");
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("testParam");
        }

        [Fact]
        public void ValidateVector_WithZeroDimensionVector_ThrowsArgumentException()
        {
            // Arrange
            var vector = Vector<double>.Build.Dense(0);

            // Act & Assert
            var act = () => VectorUtils.ValidateVector(vector, "testParam");
            act.Should().Throw<ArgumentException>()
                .WithParameterName("testParam")
                .WithMessage("Vector cannot have zero dimension.*");
        }

        [Fact]
        public void ValidateVector_WithValidVector_DoesNotThrow()
        {
            // Arrange
            var vector = Vector<double>.Build.Dense(new[] { 1.0, 2.0, 3.0 });

            // Act & Assert
            var act = () => VectorUtils.ValidateVector(vector, "testParam");
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
            var distances = new[] { 1.0, double.NaN, 2.0, double.PositiveInfinity, 3.0, double.NegativeInfinity };

            // Act
            var result = VectorUtils.CalculateDistanceStatistics(distances);

            // Assert
            result.mean.Should().BeApproximately(2.0, 0.001);
            result.min.Should().Be(1.0);
            result.max.Should().Be(3.0);
            result.standardDeviation.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CalculateDistanceStatistics_WithValidDistances_ReturnsCorrectStatistics()
        {
            // Arrange
            var distances = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

            // Act
            var result = VectorUtils.CalculateDistanceStatistics(distances);

            // Assert
            result.mean.Should().BeApproximately(3.0, 0.001);
            result.min.Should().Be(1.0);
            result.max.Should().Be(5.0);
            result.standardDeviation.Should().BeApproximately(Math.Sqrt(2.5), 0.001);
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