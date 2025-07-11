using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;

using System;
using System.Collections.Generic;
using System.Linq;

namespace AiGeekSquad.AIContext.Chunking
{
    /// <summary>
    /// Provides extension methods and utility functions for working with Math.NET vectors.
    /// </summary>
    internal static class VectorExtensions
    {
        /// <summary>
        /// Calculates the percentile value from a collection of distances.
        /// </summary>
        /// <param name="distances">The collection of distance values.</param>
        /// <param name="percentile">The percentile to calculate (0.0 to 1.0).</param>
        /// <returns>The percentile value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when distances is null.</exception>
        /// <exception cref="ArgumentException">Thrown when percentile is not between 0 and 1.</exception>
        public static double CalculatePercentile(IEnumerable<double> distances, double percentile)
        {
            if (distances == null)
                throw new ArgumentNullException(nameof(distances));
            if (percentile < 0.0 || percentile > 1.0)
                throw new ArgumentException("Percentile must be between 0.0 and 1.0.", nameof(percentile));

            var sortedDistances = distances.Where(d => !double.IsNaN(d) && !double.IsInfinity(d))
                                          .OrderBy(d => d)
                                          .ToArray();

            if (sortedDistances.Length == 0)
                return 0.0;

            if (sortedDistances.Length == 1)
                return sortedDistances[0];

            // Calculate percentile manually
            var realIndex = percentile * (sortedDistances.Length - 1);
            var index = (int)realIndex;
            var frac = realIndex - index;

            if (index + 1 < sortedDistances.Length)
            {
                return sortedDistances[index] * (1 - frac) + sortedDistances[index + 1] * frac;
            }
            else
            {
                return sortedDistances[index];
            }
        }

        /// <summary>
        /// Identifies breakpoint indices where distances exceed the specified threshold.
        /// </summary>
        /// <param name="distances">The collection of distance values.</param>
        /// <param name="threshold">The threshold value for identifying breakpoints.</param>
        /// <returns>A collection of indices where breakpoints occur.</returns>
        /// <exception cref="ArgumentNullException">Thrown when distances is null.</exception>
        public static IEnumerable<int> FindBreakpoints(IEnumerable<double> distances, double threshold)
        {
            if (distances == null)
                throw new ArgumentNullException(nameof(distances));

            var distanceArray = distances.ToArray();
            var breakpoints = new List<int>();

            for (var i = 0; i < distanceArray.Length; i++)
            {
                if (!double.IsNaN(distanceArray[i]) && !double.IsInfinity(distanceArray[i]) && distanceArray[i] >= threshold)
                {
                    breakpoints.Add(i);
                }
            }

            return breakpoints;
        }

        /// <summary>
        /// Creates a vector from an array of double values using Math.NET.
        /// </summary>
        /// <param name="values">The array of values to create the vector from.</param>
        /// <returns>A Math.NET vector containing the specified values.</returns>
        /// <exception cref="ArgumentNullException">Thrown when values is null.</exception>
        public static Vector<double> CreateVector(double[] values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            return Vector<double>.Build.DenseOfArray(values);
        }

        /// <summary>
        /// Creates a vector from an enumerable of double values using Math.NET.
        /// </summary>
        /// <param name="values">The enumerable of values to create the vector from.</param>
        /// <returns>A Math.NET vector containing the specified values.</returns>
        /// <exception cref="ArgumentNullException">Thrown when values is null.</exception>
        public static Vector<double> CreateVector(IEnumerable<double> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            return Vector<double>.Build.DenseOfEnumerable(values);
        }

        /// <summary>
        /// Validates that a vector is not null and has a positive dimension.
        /// </summary>
        /// <param name="vector">The vector to validate.</param>
        /// <param name="parameterName">The name of the parameter for exception messages.</param>
        /// <exception cref="ArgumentNullException">Thrown when vector is null.</exception>
        /// <exception cref="ArgumentException">Thrown when vector has zero dimension.</exception>
        public static void ValidateVector(Vector<double> vector, string parameterName)
        {
            if (vector == null)
                throw new ArgumentNullException(parameterName);
            if (vector.Count == 0)
                throw new ArgumentException("Vector cannot have zero dimension.", parameterName);
        }

        /// <summary>
        /// Calculates basic statistics for a collection of distances.
        /// </summary>
        /// <param name="distances">The collection of distance values.</param>
        /// <returns>A tuple containing (mean, standardDeviation, min, max) statistics.</returns>
        public static (double mean, double standardDeviation, double min, double max) CalculateDistanceStatistics(IEnumerable<double> distances)
        {
            if (distances == null)
                return (0.0, 0.0, 0.0, 0.0);

            var validDistances = distances.Where(d => !double.IsNaN(d) && !double.IsInfinity(d)).ToArray();

            if (validDistances.Length == 0)
                return (0.0, 0.0, 0.0, 0.0);

            var mean = validDistances.Mean();
            var standardDeviation = validDistances.StandardDeviation();
            var min = validDistances.Min();
            var max = validDistances.Max();

            return (mean, standardDeviation, min, max);
        }
    }
}