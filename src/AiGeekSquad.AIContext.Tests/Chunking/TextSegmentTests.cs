using AiGeekSquad.AIContext.Chunking;
using FluentAssertions;
using FluentAssertions.Execution;

namespace AiGeekSquad.AIContext.Tests.Chunking;

public class TextSegmentTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var text = "Sample text segment";
        var startIndex = 10;
        var endIndex = 29;

        // Act
        var textSegment = new TextSegment(text, startIndex, endIndex);

        // Assert
        using var _ = new AssertionScope();
        textSegment.Text.Should().Be(text);
        textSegment.StartIndex.Should().Be(startIndex);
        textSegment.EndIndex.Should().Be(endIndex);
        textSegment.Length.Should().Be(text.Length);
    }

    [Fact]
    public void Constructor_WithNullText_ThrowsArgumentNullException()
    {
        // Arrange
        string text = null!;
        var startIndex = 0;
        var endIndex = 10;

        // Act & Assert
        var act = () => new TextSegment(text, startIndex, endIndex);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(text));
    }

    [Fact]
    public void Constructor_WithEmptyText_CreatesInstance()
    {
        // Arrange
        var text = "";
        var startIndex = 5;
        var endIndex = 5;

        // Act
        var textSegment = new TextSegment(text, startIndex, endIndex);

        // Assert
        using var _ = new AssertionScope();
        textSegment.Text.Should().BeEmpty();
        textSegment.StartIndex.Should().Be(startIndex);
        textSegment.EndIndex.Should().Be(endIndex);
        textSegment.Length.Should().Be(0);
    }

    [Fact]
    public void Length_ReturnsTextLength()
    {
        // Arrange
        var text = "This is a test segment";
        var textSegment = new TextSegment(text, 0, text.Length);

        // Act
        var length = textSegment.Length;

        // Assert
        length.Should().Be(text.Length);
        length.Should().Be(22);
    }

    [Fact]
    public void ToString_WithShortText_ReturnsFullText()
    {
        // Arrange
        var text = "Short segment";
        var textSegment = new TextSegment(text, 5, 18);

        // Act
        var result = textSegment.ToString();

        // Assert
        result.Should().Be("TextSegment[5-18]: Short segment");
    }

    [Fact]
    public void ToString_WithLongText_TruncatesText()
    {
        // Arrange
        var longText = new string('X', 100); // 100 characters
        var textSegment = new TextSegment(longText, 0, 100);

        // Act
        var result = textSegment.ToString();

        // Assert
        using var _ = new AssertionScope();
        result.Should().StartWith("TextSegment[0-100]: ");
        result.Should().EndWith("...");
        result.Should().Contain(new string('X', 50)); // First 50 characters should be present
        result.Length.Should().BeLessThan(longText.Length + 25); // Should be truncated
    }

    [Fact]
    public void ToString_WithExactly50Characters_DoesNotTruncate()
    {
        // Arrange
        var text = new string('Y', 50); // Exactly 50 characters
        var textSegment = new TextSegment(text, 0, 50);

        // Act
        var result = textSegment.ToString();

        // Assert
        using var _ = new AssertionScope();
        result.Should().Be($"TextSegment[0-50]: {text}");
        result.Should().NotEndWith("...");
    }

    [Fact]
    public void ToString_With51Characters_TruncatesText()
    {
        // Arrange
        var text = new string('Z', 51); // 51 characters
        var textSegment = new TextSegment(text, 10, 61);

        // Act
        var result = textSegment.ToString();

        // Assert
        using var _ = new AssertionScope();
        result.Should().StartWith("TextSegment[10-61]: ");
        result.Should().EndWith("...");
        result.Should().Contain(new string('Z', 50)); // First 50 characters
    }

    [Fact]
    public void Properties_AreReadOnly_AfterConstruction()
    {
        // Arrange
        var text = "Immutable segment";
        var startIndex = 3;
        var endIndex = 20;
        var textSegment = new TextSegment(text, startIndex, endIndex);

        // Act & Assert
        using var _ = new AssertionScope();
        textSegment.Text.Should().Be(text);
        textSegment.StartIndex.Should().Be(startIndex);
        textSegment.EndIndex.Should().Be(endIndex);
        textSegment.Length.Should().Be(text.Length);
    }

    [Fact]
    public void Constructor_WithWhitespaceText_CreatesInstance()
    {
        // Arrange
        var text = "   \t\n   ";
        var startIndex = 0;
        var endIndex = 8;

        // Act
        var textSegment = new TextSegment(text, startIndex, endIndex);

        // Assert
        using var _ = new AssertionScope();
        textSegment.Text.Should().Be(text);
        textSegment.StartIndex.Should().Be(startIndex);
        textSegment.EndIndex.Should().Be(endIndex);
        textSegment.Length.Should().Be(text.Length);
    }

    [Fact]
    public void Constructor_WithSpecialCharacters_CreatesInstance()
    {
        // Arrange
        var text = "Special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?";
        var startIndex = 100;
        var endIndex = 144;

        // Act
        var textSegment = new TextSegment(text, startIndex, endIndex);

        // Assert
        using var _ = new AssertionScope();
        textSegment.Text.Should().Be(text);
        textSegment.StartIndex.Should().Be(startIndex);
        textSegment.EndIndex.Should().Be(endIndex);
        textSegment.Length.Should().Be(text.Length);
    }
}