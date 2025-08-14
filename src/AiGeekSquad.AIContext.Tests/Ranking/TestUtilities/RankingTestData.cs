using System;
using System.Collections.Generic;
using System.Linq;
using AiGeekSquad.AIContext.Ranking;

namespace AiGeekSquad.AIContext.Tests.Ranking.TestUtilities
{
    /// <summary>
    /// Represents a document for search ranking scenarios.
    /// </summary>
    public class Document
    {
        public string Title { get; }
        public string Content { get; }
        public double RelevanceScore { get; }
        public int PopularityRank { get; }
        public DateTime PublishedDate { get; }

        public Document(string title, string content, double relevanceScore, int popularityRank, DateTime publishedDate)
        {
            Title = title;
            Content = content;
            RelevanceScore = relevanceScore;
            PopularityRank = popularityRank;
            PublishedDate = publishedDate;
        }

        public override string ToString() => $"{Title} (Relevance: {RelevanceScore}, Popularity: {PopularityRank})";
    }

    /// <summary>
    /// Scores documents based on their semantic relevance to a query.
    /// </summary>
    public class SemanticRelevanceScorer : IScoringFunction<Document>
    {
        public string Name { get; }

        public SemanticRelevanceScorer(string name = "SemanticRelevance")
        {
            Name = name;
        }

        public double ComputeScore(Document document) => document.RelevanceScore;

        public double[] ComputeScores(IReadOnlyList<Document> documents)
        {
            return documents.Select(doc => doc.RelevanceScore).ToArray();
        }
    }

    /// <summary>
    /// Scores documents based on their popularity (inverse of rank - lower rank number = higher popularity).
    /// </summary>
    public class PopularityScorer : IScoringFunction<Document>
    {
        public string Name { get; }

        public PopularityScorer(string name = "Popularity")
        {
            Name = name;
        }

        public double ComputeScore(Document document) => 1.0 / document.PopularityRank;

        public double[] ComputeScores(IReadOnlyList<Document> documents)
        {
            return documents.Select(doc => 1.0 / doc.PopularityRank).ToArray();
        }
    }

    /// <summary>
    /// Scores documents based on recency (newer documents get higher scores).
    /// </summary>
    public class RecencyScorer : IScoringFunction<Document>
    {
        private readonly DateTime _referenceDate;

        public string Name { get; }

        public RecencyScorer(DateTime referenceDate, string name = "Recency")
        {
            _referenceDate = referenceDate;
            Name = name;
        }

        public double ComputeScore(Document document)
        {
            var daysDifference = (_referenceDate - document.PublishedDate).TotalDays;
            return Math.Max(0, 365 - daysDifference) / 365.0; // Score decreases with age
        }

        public double[] ComputeScores(IReadOnlyList<Document> documents)
        {
            return documents.Select(ComputeScore).ToArray();
        }
    }

    /// <summary>
    /// A scoring function that returns constant scores for testing edge cases.
    /// </summary>
    public class ConstantScorer : IScoringFunction<Document>
    {
        private readonly double _constantScore;

        public string Name { get; }

        public ConstantScorer(double constantScore, string name = "Constant")
        {
            _constantScore = constantScore;
            Name = name;
        }

        public double ComputeScore(Document document) => _constantScore;

        public double[] ComputeScores(IReadOnlyList<Document> documents)
        {
            return Enumerable.Repeat(_constantScore, documents.Count).ToArray();
        }
    }

    /// <summary>
    /// Static helper methods for creating test data and scoring functions.
    /// </summary>
    public static class RankingTestHelpers
    {
        public static List<Document> CreateTestDocuments()
        {
            var baseDate = new DateTime(2024, 1, 1);
            return new List<Document>
            {
                new("Machine Learning Fundamentals", "Introduction to ML concepts", 0.95, 1, baseDate.AddDays(-30)),
                new("Advanced Neural Networks", "Deep learning techniques", 0.85, 3, baseDate.AddDays(-60)),
                new("Data Science Basics", "Getting started with data science", 0.75, 2, baseDate.AddDays(-10)),
                new("AI Ethics and Society", "Responsible AI development", 0.65, 5, baseDate.AddDays(-90)),
                new("Python Programming Guide", "Complete Python tutorial", 0.55, 4, baseDate.AddDays(-5))
            };
        }

        public static WeightedScoringFunction<Document> CreateSemanticFunction(double weight = 1.0, IScoreNormalizer? normalizer = null)
        {
            return new WeightedScoringFunction<Document>(new SemanticRelevanceScorer(), weight) { Normalizer = normalizer };
        }

        public static WeightedScoringFunction<Document> CreatePopularityFunction(double weight = 1.0, IScoreNormalizer? normalizer = null)
        {
            return new WeightedScoringFunction<Document>(new PopularityScorer(), weight) { Normalizer = normalizer };
        }

        public static WeightedScoringFunction<Document> CreateRecencyFunction(double weight = 1.0, IScoreNormalizer? normalizer = null)
        {
            var referenceDate = new DateTime(2024, 1, 1);
            return new WeightedScoringFunction<Document>(new RecencyScorer(referenceDate), weight) { Normalizer = normalizer };
        }
    }
}