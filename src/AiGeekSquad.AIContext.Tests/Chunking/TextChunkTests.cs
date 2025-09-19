using AiGeekSquad.AIContext.Chunking;
using FluentAssertions;
using FluentAssertions.Execution;

namespace AiGeekSquad.AIContext.Tests.Chunking;

public class TextChunkTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var text = "Sample text chunk";
        var startIndex = 10;
        var endIndex = 27;
        var metadata = new Dictionary<string, object> { { "key1", "value1" } };

        // Act
        var textChunk = new TextChunk(text, startIndex, endIndex, metadata);

        // Assert
        using var _ = new AssertionScope();
        textChunk.Text.Should().Be(text);
        textChunk.StartIndex.Should().Be(startIndex);
        textChunk.EndIndex.Should().Be(endIndex);
        textChunk.Length.Should().Be(text.Length);
        textChunk.Metadata.Should().BeSameAs(metadata);
        textChunk.Metadata.Should().ContainKey("key1");
    }

    [Fact]
    public void Constructor_WithNullText_ThrowsArgumentNullException()
    {
        // Arrange
        string text = null!;
        var startIndex = 0;
        var endIndex = 10;

        // Act & Assert
        var act = () => new TextChunk(text, startIndex, endIndex);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(text));
    }

    [Fact]
    public void Constructor_WithNullMetadata_CreatesEmptyMetadata()
    {
        // Arrange
        var text = "Sample text";
        var startIndex = 0;
        var endIndex = 11;

        // Act
        var textChunk = new TextChunk(text, startIndex, endIndex, null);

        // Assert
        using var _ = new AssertionScope();
        textChunk.Metadata.Should().NotBeNull();
        textChunk.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithoutMetadata_CreatesEmptyMetadata()
    {
        // Arrange
        var text = "Sample text";
        var startIndex = 0;
        var endIndex = 11;

        // Act
        var textChunk = new TextChunk(text, startIndex, endIndex);

        // Assert
        using var _ = new AssertionScope();
        textChunk.Metadata.Should().NotBeNull();
        textChunk.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void Length_ReturnsTextLength()
    {
        // Arrange
        var text = "This is a test text";
        var textChunk = new TextChunk(text, 0, text.Length);

        // Act
        var length = textChunk.Length;

        // Assert
        length.Should().Be(text.Length);
        length.Should().Be(19);
    }

    [Fact]
    public void ToString_WithShortText_ReturnsFullText()
    {
        // Arrange
        var text = "Short text";
        var textChunk = new TextChunk(text, 5, 15);

        // Act
        var result = textChunk.ToString();

        // Assert
        result.Should().Be("TextChunk[5-15]: Short text");
    }

    [Fact]
    public void ToString_WithLongText_TruncatesText()
    {
        // Arrange
        var longText = new string('A', 100); // 100 characters
        var textChunk = new TextChunk(longText, 0, 100);

        // Act
        var result = textChunk.ToString();

        // Assert
        using var _ = new AssertionScope();
        result.Should().StartWith("TextChunk[0-100]: ");
        result.Should().EndWith("...");
        result.Should().Contain(new string('A', 50)); // First 50 characters should be present
        result.Length.Should().BeLessThan(longText.Length + 20); // Should be truncated
    }

    [Fact]
    public void ToString_WithExactly50Characters_DoesNotTruncate()
    {
        // Arrange
        var text = new string('B', 50); // Exactly 50 characters
        var textChunk = new TextChunk(text, 0, 50);

        // Act
        var result = textChunk.ToString();

        // Assert
        using var _ = new AssertionScope();
        result.Should().Be($"TextChunk[0-50]: {text}");
        result.Should().NotEndWith("...");
    }

    [Fact]
    public void ToString_With51Characters_TruncatesText()
    {
        // Arrange
        var text = new string('C', 51); // 51 characters
        var textChunk = new TextChunk(text, 10, 61);

        // Act
        var result = textChunk.ToString();

        // Assert
        using var _ = new AssertionScope();
        result.Should().StartWith("TextChunk[10-61]: ");
        result.Should().EndWith("...");
        result.Should().Contain(new string('C', 50)); // First 50 characters
    }

    [Fact]
    public void Metadata_CanBeModified()
    {
        // Arrange
        var text = "Test text";
        var textChunk = new TextChunk(text, 0, 9);

        // Act
        textChunk.Metadata["newKey"] = "newValue";

        // Assert
        using var _ = new AssertionScope();
        textChunk.Metadata.Should().ContainKey("newKey");
        textChunk.Metadata["newKey"].Should().Be("newValue");
    }

    [Fact]
    public void Properties_AreReadOnly_AfterConstruction()
    {
        // Arrange
        var text = "Immutable text";
        var startIndex = 5;
        var endIndex = 19;
        var textChunk = new TextChunk(text, startIndex, endIndex);

        // Act & Assert
        using var _ = new AssertionScope();
        textChunk.Text.Should().Be(text);
        textChunk.StartIndex.Should().Be(startIndex);
        textChunk.EndIndex.Should().Be(endIndex);
        textChunk.Length.Should().Be(text.Length);
    }
}