using AiGeekSquad.AIContext.Chunking;

using FluentAssertions;
using FluentAssertions.Execution;

using MathNet.Numerics.LinearAlgebra;

namespace AiGeekSquad.AIContext.Tests.Chunking;

public class SentenceGroupTests
{
    // Static readonly fields to avoid repeated array allocations in tests
    private static readonly string[] TwoSentencesArray = ["First sentence.", "Second sentence."];
    private static readonly string[] SingleSentenceArray = ["Test sentence."];
    private static readonly string[] ShortTextArray = ["Short text."];
    private static readonly string[] LongTextArray = [new string('A', 150)];

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var sentences = TwoSentencesArray;
        var startIndex = 0;
        var endIndex = 30;

        // Act
        var sentenceGroup = new SentenceGroup(sentences, startIndex, endIndex);

        // Assert
        using var _ = new AssertionScope();
        sentenceGroup.Sentences.Should().BeEquivalentTo(sentences);
        sentenceGroup.StartIndex.Should().Be(startIndex);
        sentenceGroup.EndIndex.Should().Be(endIndex);
        sentenceGroup.CombinedText.Should().Be("First sentence. Second sentence.");
        sentenceGroup.SentenceCount.Should().Be(2);
        sentenceGroup.HasEmbedding.Should().BeFalse();
        sentenceGroup.Embedding.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNullSentences_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<string> sentences = null!;
        var startIndex = 0;
        var endIndex = 10;

        // Act & Assert
        var act = () => new SentenceGroup(sentences, startIndex, endIndex);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(sentences));
    }

    [Fact]
    public void Constructor_WithEmptySentences_CreatesInstanceWithEmptyList()
    {
        // Arrange
        var sentences = Array.Empty<string>();
        var startIndex = 0;
        var endIndex = 0;

        // Act
        var sentenceGroup = new SentenceGroup(sentences, startIndex, endIndex);

        // Assert
        using var _ = new AssertionScope();
        sentenceGroup.Sentences.Should().BeEmpty();
        sentenceGroup.CombinedText.Should().BeEmpty();
        sentenceGroup.SentenceCount.Should().Be(0);
    }

    [Fact]
    public void Embedding_WhenSet_UpdatesHasEmbeddingProperty()
    {
        // Arrange
        var sentences = SingleSentenceArray;
        var sentenceGroup = new SentenceGroup(sentences, 0, 14);
        var embedding = Vector<double>.Build.Dense([0.1, 0.2, 0.3]);

        // Act
        sentenceGroup.Embedding = embedding;

        // Assert
        using var _ = new AssertionScope();
        sentenceGroup.Embedding.Should().BeSameAs(embedding);
        sentenceGroup.HasEmbedding.Should().BeTrue();
    }

    [Fact]
    public void ToString_WithShortText_ReturnsFullText()
    {
        // Arrange
        var sentences = ShortTextArray;
        var sentenceGroup = new SentenceGroup(sentences, 0, 11);

        // Act
        var result = sentenceGroup.ToString();

        // Assert
        result.Should().Be("SentenceGroup[0-11, 1 sentences]: Short text.");
    }

    [Fact]
    public void ToString_WithLongText_TruncatesText()
    {
        // Arrange
        var sentences = LongTextArray;
        var sentenceGroup = new SentenceGroup(sentences, 0, 150);

        // Act
        var result = sentenceGroup.ToString();

        // Assert
        using var _ = new AssertionScope();
        result.Should().StartWith("SentenceGroup[0-150, 1 sentences]: ");
        result.Should().EndWith("...");
        result.Should().Contain(new string('A', 100));
    }
}