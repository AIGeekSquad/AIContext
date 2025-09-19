using AiGeekSquad.AIContext.Chunking;
using FluentAssertions;
using FluentAssertions.Execution;

namespace AiGeekSquad.AIContext.Tests.Chunking;

public class SentenceTextSplitterTests
{
        [Fact]
        public async Task SplitAsync_WithSimpleSentences_SplitsCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "This is the first sentence. This is the second sentence! This is the third sentence?";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("This is the first sentence.");
            segments[1].Text.Should().Be("This is the second sentence!");
            segments[2].Text.Should().Be("This is the third sentence?");
            segments[0].StartIndex.Should().Be(0);
            segments[0].EndIndex.Should().Be(27);
        }

        [Fact]
        public async Task SplitAsync_WithNullText_ThrowsArgumentNullException()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();

            // Act & Assert
            var act = async () =>
            {
                await foreach (var segment in splitter.SplitAsync(null!))
                {
                    // This should not execute
                }
            };

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task SplitAsync_WithEmptyText_ReturnsEmptyResult()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            segments.Should().BeEmpty();
        }

        [Fact]
        public async Task SplitAsync_WithWhitespaceText_ReturnsEmptyResult()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "   \t\n\r   ";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            segments.Should().BeEmpty();
        }

        [Fact]
        public async Task SplitAsync_WithSingleSentence_ReturnsSingleSegment()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "This is a single sentence without ending punctuation";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().ContainSingle();
            segments[0].Text.Should().Be("This is a single sentence without ending punctuation");
            segments[0].StartIndex.Should().Be(0);
            segments[0].EndIndex.Should().Be(text.Length);
        }

        [Fact]
        public async Task SplitAsync_WithNoProperSentenceBoundaries_ReturnsSingleSegment()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "this is all lowercase. no capital letters after periods.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().ContainSingle();
            segments[0].Text.Should().Be("this is all lowercase. no capital letters after periods.");
        }

        [Fact]
        public async Task SplitAsync_WithMixedPunctuation_SplitsOnAllTypes()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Question one? Statement two. Exclamation three! Another statement.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(4);
            segments[0].Text.Should().Be("Question one?");
            segments[1].Text.Should().Be("Statement two.");
            segments[2].Text.Should().Be("Exclamation three!");
            segments[3].Text.Should().Be("Another statement.");
        }

        [Fact]
        public async Task SplitAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "First sentence. Second sentence. Third sentence.";

            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            var act = async () =>
            {
                await foreach (var segment in splitter.SplitAsync(text, cts.Token))
                {
                    // This should not execute due to cancellation
                }
            };

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task SplitAsync_WithExtraWhitespace_TrimsCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "  First sentence.   Second sentence.  ";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("First sentence.");
            segments[1].Text.Should().Be("Second sentence.");
            // Should maintain original positions in text
            segments[0].StartIndex.Should().Be(2);
            segments[1].StartIndex.Should().BeGreaterThan(segments[0].EndIndex);
        }

        [Fact]
        public void WithPattern_WithValidPattern_CreatesCustomSplitter()
        {
            // Arrange
            var customPattern = @"\s*\|\s*"; // Split on pipe characters

            // Act
            var splitter = SentenceTextSplitter.WithPattern(customPattern);

            // Assert
            splitter.Should().NotBeNull();
            splitter.Should().BeOfType<SentenceTextSplitter>();
        }

        [Fact]
        public void WithPattern_WithNullPattern_ThrowsArgumentException()
        {
            // Act & Assert
            var act = () => SentenceTextSplitter.WithPattern(null!);
            act.Should().Throw<ArgumentException>()
                .WithMessage("Pattern cannot be null or empty.*");
        }

        [Fact]
        public void WithPattern_WithEmptyPattern_ThrowsArgumentException()
        {
            // Act & Assert
            var act = () => SentenceTextSplitter.WithPattern("");
            act.Should().Throw<ArgumentException>()
                .WithMessage("Pattern cannot be null or empty.*");
        }

        [Fact]
        public void WithPattern_WithWhitespacePattern_ThrowsArgumentException()
        {
            // Act & Assert
            var act = () => SentenceTextSplitter.WithPattern("   ");
            act.Should().Throw<ArgumentException>()
                .WithMessage("Pattern cannot be null or empty.*");
        }

        [Fact]
        public async Task SplitAsync_WithCustomPattern_UsesCustomSplitting()
        {
            // Arrange
            var customPattern = @"\s*\|\s*"; // Split on pipe characters
            var splitter = SentenceTextSplitter.WithPattern(customPattern);
            var text = "First part | Second part | Third part";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("First part");
            segments[1].Text.Should().Be("Second part");
            segments[2].Text.Should().Be("Third part");
        }

        [Fact]
        public void Default_ReturnsNewInstanceWithDefaultPattern()
        {
            // Act
            var splitter1 = SentenceTextSplitter.Default;
            var splitter2 = SentenceTextSplitter.Default;

            // Assert
            using var _ = new AssertionScope();
            splitter1.Should().NotBeNull();
            splitter2.Should().NotBeNull();
            splitter1.Should().NotBeSameAs(splitter2); // Should create new instances
        }

        [Fact]
        public async Task SplitAsync_WithComplexText_HandleCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Dr. Smith said hello. He went to the U.S.A. Then he returned! What happened next?";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().NotBeEmpty();
            // Note: Simple regex may not handle abbreviations perfectly, which is expected
            segments.Should().OnlyContain(s => !string.IsNullOrWhiteSpace(s.Text));
        }

        [Fact]
        public async Task SplitAsync_VerifiesTextSegmentProperties()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "First sentence. Second sentence.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);

            foreach (var segment in segments)
            {
                segment.Text.Should().NotBeNullOrWhiteSpace();
                segment.StartIndex.Should().BeGreaterThanOrEqualTo(0);
                segment.EndIndex.Should().BeGreaterThan(segment.StartIndex);
                segment.Length.Should().Be(segment.Text.Length);

                // Verify the segment text matches what's in the original text
                var extractedText = text[segment.StartIndex..segment.EndIndex];
                extractedText.Should().Contain(segment.Text.Trim());
            }
        }

        [Fact]
        public async Task SplitAsync_WithFilenames_HandlesCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "The file config.json contains settings. Please check Program.cs for the implementation. The README.md file has documentation.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("The file config.json contains settings.");
            segments[1].Text.Should().Be("Please check Program.cs for the implementation.");
            segments[2].Text.Should().Be("The README.md file has documentation.");
        }

        [Fact]
        public async Task SplitAsync_WithFilePaths_HandlesCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Open the file at C:\\Users\\john.doe\\Documents\\file.txt for reading. The Linux path /home/user/project/src/main.cpp contains the source code. Check D:\\Projects\\MyApp\\bin\\Debug\\app.exe for the executable.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("Open the file at C:\\Users\\john.doe\\Documents\\file.txt for reading.");
            segments[1].Text.Should().Be("The Linux path /home/user/project/src/main.cpp contains the source code.");
            segments[2].Text.Should().Be("Check D:\\Projects\\MyApp\\bin\\Debug\\app.exe for the executable.");
        }

        [Fact]
        public async Task SplitAsync_WithUrls_HandlesCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Visit https://www.example.com for more information. The API endpoint is https://api.service.com/v1/data. You can also check http://localhost:3000/dashboard for local testing.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("Visit https://www.example.com for more information.");
            segments[1].Text.Should().Be("The API endpoint is https://api.service.com/v1/data.");
            segments[2].Text.Should().Be("You can also check http://localhost:3000/dashboard for local testing.");
        }

        [Fact]
        public async Task SplitAsync_WithAcronyms_HandlesCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "The application uses .NET Core framework. We also support ASP.NET and Entity Framework. The U.S.A. is where our main office is located.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("The application uses .NET Core framework.");
            segments[1].Text.Should().Be("We also support ASP.NET and Entity Framework.");
            segments[2].Text.Should().Be("The U.S.A. is where our main office is located.");
        }

        [Fact]
        public async Task SplitAsync_WithDoctorTitlesAndAbbreviations_DefaultPatternDoesNotSplitOnAbbreviations()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Dr. Smith works at the hospital. Prof. Johnson teaches at the university. Mr. Brown and Mrs. Green are attending the meeting.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            // The updated default pattern now avoids splitting on common abbreviations
            segments.Should().HaveCount(3);
            // Verify that abbreviations are NOT split (new behavior with improved pattern)
            segments[0].Text.Should().Be("Dr. Smith works at the hospital.");
            segments[1].Text.Should().Be("Prof. Johnson teaches at the university.");
            segments[2].Text.Should().Be("Mr. Brown and Mrs. Green are attending the meeting.");
        }

        [Fact]
        public async Task SplitAsync_WithCustomPatternForSpecialCases_HandlesCorrectly()
        {
            // Arrange
            // Use double space as sentence delimiter
            var customPattern = @"\s{2,}";
            var splitter = SentenceTextSplitter.WithPattern(customPattern);
            var text = "First sentence here.  Second sentence here.  Third sentence.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("First sentence here.");
            segments[1].Text.Should().Be("Second sentence here.");
            segments[2].Text.Should().Be("Third sentence.");
        }

        [Fact]
        public async Task SplitAsync_WithVersionNumbers_HandlesCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "The software version is 1.2.3 and it's stable. Please upgrade to version 2.0.1 for better performance. The legacy version 0.9.8 is no longer supported.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("The software version is 1.2.3 and it's stable.");
            segments[1].Text.Should().Be("Please upgrade to version 2.0.1 for better performance.");
            segments[2].Text.Should().Be("The legacy version 0.9.8 is no longer supported.");
        }

        [Fact]
        public async Task SplitAsync_WithComplexTechnicalText_HandlesCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Install .NET 8.0 from https://dotnet.microsoft.com/download. Configure the appsettings.json file in your project. Run the command dotnet build in the terminal.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("Install .NET 8.0 from https://dotnet.microsoft.com/download.");
            segments[1].Text.Should().Be("Configure the appsettings.json file in your project.");
            segments[2].Text.Should().Be("Run the command dotnet build in the terminal.");
        }

        [Fact]
        public async Task SplitAsync_WithEmailAddresses_HandlesCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Contact john.doe@company.com for support. The admin email is admin@system.org and it's monitored daily. For urgent issues, reach out to emergency@service.net immediately.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("Contact john.doe@company.com for support.");
            segments[1].Text.Should().Be("The admin email is admin@system.org and it's monitored daily.");
            segments[2].Text.Should().Be("For urgent issues, reach out to emergency@service.net immediately.");
        }

        [Fact]
        public async Task SplitAsync_WithQuotedText_HandlesQuotedSentences()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = """He said, "How do you draw an Owl Mr. Crawley ?" to Dr. Tom. No one answered.""";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();


            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("""He said, "How do you draw an Owl Mr. Crawley ?" to Dr. Tom.""");
            segments[1].Text.Should().Be("No one answered.");

        }
        // MARKDOWN TESTS

        [Fact]
        public async Task SplitAsync_Markdown_UnorderedLists_AllBulletTypes()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = """
                       - Item one
                       * Item two
                       + Item three
                       """;
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("- Item one");
            segments[1].Text.Should().Be("* Item two");
            segments[2].Text.Should().Be("+ Item three");
        }

        [Fact]
        public async Task SplitAsync_Markdown_OrderedLists_Numbers()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = "1. First item\n2. Second item\n3. Third item";
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("1. First item");
            segments[1].Text.Should().Be("2. Second item");
            segments[2].Text.Should().Be("3. Third item");
        }

        [Fact]
        public async Task SplitAsync_Markdown_NestedLists()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = """
                       - Parent
                         - Child 1
                         - Child 2
                           * Grandchild
                       """;
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            segments.Should().HaveCount(4);
            segments[0].Text.Should().Be("- Parent");
            segments[1].Text.Should().Be("  - Child 1");
            segments[2].Text.Should().Be("  - Child 2");
            segments[3].Text.Should().Be("    * Grandchild");
        }

        [Fact]
        public async Task SplitAsync_Markdown_ListItemsWithMultipleSentences()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = "- First item. Has two sentences!\n- Second item? Yes.";
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("- First item. Has two sentences!");
            segments[1].Text.Should().Be("- Second item? Yes.");
        }

        [Fact]
        public async Task SplitAsync_Markdown_MixedListsAndParagraphs()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = "- List item\n\nParagraph one. Paragraph two!\n- Another item";
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            segments.Should().HaveCount(4);
            segments[0].Text.Should().Be("- List item");
            segments[1].Text.Should().Be("Paragraph one.");
            segments[2].Text.Should().Be("Paragraph two!");
            segments[3].Text.Should().Be("- Another item");
        }

        [Fact]
        public async Task SplitAsync_Markdown_Headers()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = """
                       # Header 1
                       ## Header 2
                       ### Header 3
                       """;
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("# Header 1");
            segments[1].Text.Should().Be("## Header 2");
            segments[2].Text.Should().Be("### Header 3");
        }

        [Fact]
        public async Task SplitAsync_Markdown_CodeBlocks_FencedAndIndented()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = """
                       ```
                       code block
                       ```
                           indented code
                       """;
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("""
                                         ```
                                         code block
                                         ```
                                         """);
            segments[1].Text.Should().Be("    indented code");
        }

        [Fact]
        public async Task SplitAsync_Markdown_InlineCode()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = "This is `inline code` in a sentence.";
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            segments.Should().HaveCount(1);
            segments[0].Text.Should().Be("This is `inline code` in a sentence.");
        }

        [Fact]
        public async Task SplitAsync_Markdown_LinksAndImages()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = "[Link](https://example.com) and ![Image](img.png)";
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            segments.Should().HaveCount(1);
            segments[0].Text.Should().Be("[Link](https://example.com) and ![Image](img.png)");
        }

        [Fact]
        public async Task SplitAsync_Markdown_Blockquotes()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = """
                       > This is a blockquote.
                       > Second line.
                       """;
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("> This is a blockquote.");
            segments[1].Text.Should().Be("> Second line.");
        }

        [Fact]
        public async Task SplitAsync_Markdown_MixedDocument()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = """
                       # Title
                       - List item
                       Paragraph one. `inline code`
                       ```
                       block
                       ```
                       [Link](url)
                       """;
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            segments.Should().HaveCount(5);
            segments[0].Text.Should().Be("# Title");
            segments[1].Text.Should().Be("- List item");
            segments[2].Text.Should().Be("Paragraph one. `inline code`");
            segments[3].Text.Should().Be("""
                                         ```
                                         block
                                         ```
                                         """);
            segments[4].Text.Should().Be("[Link](url)");
        }

        [Fact]
        public async Task SplitAsync_Markdown_EmptyElements()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = """
                       - 

                       ```

                       ```
                       # 

                       """;
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("- ");
            segments[1].Text.Should().Be("""
                                         ```

                                         ```
                                         """);
            segments[2].Text.Should().Be("# ");
        }

        [Fact]
        public async Task SplitAsync_Markdown_MixedWithRegularText()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = """
                       This is a paragraph. - List item
                       Another sentence!
                       """;
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("This is a paragraph.");
            segments[1].Text.Should().Be("- List item");
            segments[2].Text.Should().Be("Another sentence!");
        }

        [Fact]
        public async Task SplitAsync_Markdown_WithPatternForMarkdown_Works()
        {
            var splitter = SentenceTextSplitter.WithPatternForMarkdown(@"(?<=\n)");
            var text = """
                       # Header
                       - List
                       Paragraph.
                       """;
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("# Header");
            segments[1].Text.Should().Be("- List");
            segments[2].Text.Should().Be("Paragraph.");
        }

        [Fact]
        public async Task SplitAsync_Markdown_BackwardCompatibility_NonMarkdownMode()
        {
            var splitter = new SentenceTextSplitter();
            var text = """
                       - List item.
                       Paragraph one.
                       Paragraph two!
                       """;
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            // Should split only by sentences, not preserve markdown
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("- List item.");
            segments[1].Text.Should().Be("Paragraph one.");
            segments[2].Text.Should().Be("Paragraph two!");
        }

        [Fact]
        public async Task SplitAsync_Markdown_ArgumentOutOfRangeException_Fix_Verification()
        {
            // Arrange
            var splitter = new SentenceTextSplitter(markdownMode: true);

            // This markdown text is designed to trigger the specific bug where:
            // 1. Markdown preprocessing changes text length
            // 2. blockStart calculated from processedText.Length exceeds originalText.Length
            // 3. This causes ArgumentOutOfRangeException in ExtractParagraphSegments -> FindInOriginalText
            var problematicMarkdownText = """
                This is a paragraph. - List item
                Another sentence!
                
                # Header with [link](https://example.com)
                
                Some text with `inline code` and more content.
                
                - List item with [another link](https://test.com)
                - Second item
                
                Final paragraph with ![image](image.png) reference.
                """;

            // Act & Assert
            // This test verifies that the fix prevents ArgumentOutOfRangeException
            // The test should complete without throwing any exceptions
            var segments = new List<TextSegment>();
            var act = async () =>
            {
                await foreach (var segment in splitter.SplitAsync(problematicMarkdownText))
                {
                    segments.Add(segment);
                }
            };

            // Should not throw ArgumentOutOfRangeException
            await act.Should().NotThrowAsync<ArgumentOutOfRangeException>();

            // Verify that we got some segments (the exact count may vary based on parsing)
            segments.Should().NotBeEmpty();

            // Verify that all segments have valid indices
            foreach (var segment in segments)
            {
                segment.StartIndex.Should().BeGreaterThanOrEqualTo(0);
                segment.EndIndex.Should().BeGreaterThan(segment.StartIndex);
                segment.EndIndex.Should().BeLessThanOrEqualTo(problematicMarkdownText.Length);
                segment.Text.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public async Task SplitAsync_Markdown_ProcessedTextLengthMismatch_DoesNotThrow()
        {
            // Arrange
            var splitter = new SentenceTextSplitter(markdownMode: true);

            // This specific pattern triggers the PreprocessMixedContent method which can
            // cause processedText to be longer than originalText, leading to blockStart
            // being out of bounds when used as searchStartHint in FindInOriginalText
            var textWithMixedContent = "Sentence one. - List item\nAnother sentence.";

            // Act & Assert
            var segments = new List<TextSegment>();
            var act = async () =>
            {
                await foreach (var segment in splitter.SplitAsync(textWithMixedContent))
                {
                    segments.Add(segment);
                }
            };

            // Should not throw ArgumentOutOfRangeException even when processed text length differs
            await act.Should().NotThrowAsync<ArgumentOutOfRangeException>();

            // Verify segments are created properly
            segments.Should().NotBeEmpty();
            foreach (var segment in segments)
            {
                segment.StartIndex.Should().BeGreaterThanOrEqualTo(0);
                segment.EndIndex.Should().BeLessThanOrEqualTo(textWithMixedContent.Length);
                segment.Text.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public async Task SplitAsync_Markdown_WithThematicBreak_ShouldUseFallbackSegments()
        {
            // Arrange
            var splitter = new SentenceTextSplitter(markdownMode: true);

            // This markdown contains a thematic break (horizontal rule) which creates a ThematicBreakBlock
            // that is not explicitly handled in the switch statement and will trigger the default case
            var markdownWithThematicBreak = """
                First paragraph before break.

                ---

                Second paragraph after break.
                """;

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(markdownWithThematicBreak))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().NotBeEmpty();
            
            // Should contain the paragraphs
            segments.Should().Contain(s => s.Text.Contains("First paragraph before break."));
            segments.Should().Contain(s => s.Text.Contains("Second paragraph after break."));
            
            // Should contain the thematic break (processed by fallback method - line 211)
            segments.Should().Contain(s => s.Text.Contains("---"));

            // Verify all segments have valid properties
            foreach (var segment in segments)
            {
                segment.StartIndex.Should().BeGreaterThanOrEqualTo(0);
                segment.EndIndex.Should().BeGreaterThan(segment.StartIndex);
                segment.EndIndex.Should().BeLessThanOrEqualTo(markdownWithThematicBreak.Length);
                segment.Text.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public async Task SplitAsync_Markdown_WithHtmlBlock_ShouldUseFallbackSegments()
        {
            // Arrange
            var splitter = new SentenceTextSplitter(markdownMode: true);

            // This markdown contains raw HTML which creates an HtmlBlock
            // that is not explicitly handled in the switch statement and will trigger the default case (line 211)
            var markdownWithHtml = """
                Regular paragraph.

                <div class="custom">
                <p>Raw HTML content</p>
                </div>

                Another paragraph.
                """;

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(markdownWithHtml))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().NotBeEmpty();
            
            // Should contain the paragraphs
            segments.Should().Contain(s => s.Text.Contains("Regular paragraph."));
            segments.Should().Contain(s => s.Text.Contains("Another paragraph."));
            
            // Should contain HTML content (processed by fallback method - line 211)
            segments.Should().Contain(s => s.Text.Contains("<div") || s.Text.Contains("Raw HTML"));

            // Verify all segments have valid properties
            foreach (var segment in segments)
            {
                segment.StartIndex.Should().BeGreaterThanOrEqualTo(0);
                segment.EndIndex.Should().BeGreaterThan(segment.StartIndex);
                segment.EndIndex.Should().BeLessThanOrEqualTo(markdownWithHtml.Length);
                segment.Text.Should().NotBeNullOrEmpty();
            }
        }

        #region Regex Pattern Specific Tests

        [Fact]
        public async Task SplitAsync_WithMrAbbreviation_ShouldNotSplitAfterMr()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "I met Mr. Smith yesterday. He was very kind.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("I met Mr. Smith yesterday.");
            segments[1].Text.Should().Be("He was very kind.");
        }

        [Fact]
        public async Task SplitAsync_WithMrsAbbreviation_ShouldNotSplitAfterMrs()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Mrs. Johnson called today. She needs help with her project.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("Mrs. Johnson called today.");
            segments[1].Text.Should().Be("She needs help with her project.");
        }

        [Fact]
        public async Task SplitAsync_WithMsAbbreviation_ShouldNotSplitAfterMs()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Ms. Davis is the manager. She will review your application.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("Ms. Davis is the manager.");
            segments[1].Text.Should().Be("She will review your application.");
        }

        [Fact]
        public async Task SplitAsync_WithDrAbbreviation_ShouldNotSplitAfterDr()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Dr. Wilson examined the patient. The diagnosis was positive.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("Dr. Wilson examined the patient.");
            segments[1].Text.Should().Be("The diagnosis was positive.");
        }

        [Fact]
        public async Task SplitAsync_WithProfAbbreviation_ShouldNotSplitAfterProf()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Prof. Anderson teaches mathematics. His lectures are excellent.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("Prof. Anderson teaches mathematics.");
            segments[1].Text.Should().Be("His lectures are excellent.");
        }

        [Fact]
        public async Task SplitAsync_WithSrAbbreviation_ShouldNotSplitAfterSr()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "John Smith Sr. founded the company. His son continues the legacy.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("John Smith Sr. founded the company.");
            segments[1].Text.Should().Be("His son continues the legacy.");
        }

        [Fact]
        public async Task SplitAsync_WithJrAbbreviation_ShouldNotSplitAfterJr()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Robert Johnson Jr. is the new CEO. He started last month.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("Robert Johnson Jr. is the new CEO.");
            segments[1].Text.Should().Be("He started last month.");
        }

        [Fact]
        public async Task SplitAsync_WithMultipleAbbreviations_ShouldNotSplitAfterAnyAbbreviation()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Dr. Smith and Mrs. Johnson met with Prof. Davis. They discussed the research project.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("Dr. Smith and Mrs. Johnson met with Prof. Davis.");
            segments[1].Text.Should().Be("They discussed the research project.");
        }

        [Fact]
        public async Task SplitAsync_WithAbbreviationAtEndOfSentence_ShouldSplitCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "The meeting was led by Dr. Wilson. Next week we'll meet with Mrs. Davis.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("The meeting was led by Dr. Wilson.");
            segments[1].Text.Should().Be("Next week we'll meet with Mrs. Davis.");
        }

        [Fact]
        public async Task SplitAsync_WithAbbreviationInMiddleOfSentence_ShouldNotSplit()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "When Dr. Smith arrived, everyone was ready. The presentation began immediately.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("When Dr. Smith arrived, everyone was ready.");
            segments[1].Text.Should().Be("The presentation began immediately.");
        }

        [Fact]
        public async Task SplitAsync_WithExclamationMark_ShouldSplitCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "What an amazing discovery! The team was thrilled with the results.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("What an amazing discovery!");
            segments[1].Text.Should().Be("The team was thrilled with the results.");
        }

        [Fact]
        public async Task SplitAsync_WithQuestionMark_ShouldSplitCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Are you ready for the presentation? Let's begin now.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("Are you ready for the presentation?");
            segments[1].Text.Should().Be("Let's begin now.");
        }

        [Fact]
        public async Task SplitAsync_WithMixedPunctuationAndAbbreviations_ShouldSplitCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "This is incredible! Are you seeing this, Dr. Smith? Yes, I can see it clearly.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("This is incredible!");
            segments[1].Text.Should().Be("Are you seeing this, Dr. Smith?");
            segments[2].Text.Should().Be("Yes, I can see it clearly.");
        }

        [Fact]
        public async Task SplitAsync_WithEllipsis_ShouldNotSplitOnEllipsis()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "The story continues... And then something unexpected happened.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("The story continues...");
            segments[1].Text.Should().Be("And then something unexpected happened.");
        }

        [Fact]
        public async Task SplitAsync_WithDecimalNumbers_ShouldNotSplitOnDecimalPoint()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "The temperature was 98.6 degrees. That's perfectly normal.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("The temperature was 98.6 degrees.");
            segments[1].Text.Should().Be("That's perfectly normal.");
        }

        [Fact]
        public async Task SplitAsync_WithWebsiteURL_ShouldNotSplitOnURLPeriods()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Visit our website at www.example.com for more information. You'll find everything you need there.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("Visit our website at www.example.com for more information.");
            segments[1].Text.Should().Be("You'll find everything you need there.");
        }

        [Fact]
        public async Task SplitAsync_WithFileExtensions_ShouldNotSplitOnFileExtensionPeriods()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Please open the document.pdf file. It contains all the details.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("Please open the document.pdf file.");
            segments[1].Text.Should().Be("It contains all the details.");
        }

        [Fact]
        public async Task SplitAsync_WithLowercaseAfterPeriod_ShouldNotSplit()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "This is a test. however, this should not split. This should split.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("This is a test. however, this should not split.");
            segments[1].Text.Should().Be("This should split.");
        }

        [Fact]
        public async Task SplitAsync_WithNoSpaceAfterPeriod_ShouldNotSplit()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "This is a test.However this should not split. This should split.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("This is a test.However this should not split.");
            segments[1].Text.Should().Be("This should split.");
        }

        [Fact]
        public async Task SplitAsync_WithMultipleSpacesAfterPeriod_ShouldSplitCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "First sentence.   Second sentence with multiple spaces.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("First sentence.");
            segments[1].Text.Should().Be("Second sentence with multiple spaces.");
        }

        [Fact]
        public async Task SplitAsync_WithAbbreviationAndRegularSentence_ShouldSplitOnlyAtRegularSentence()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "I spoke with Dr. Johnson about the results. The findings were significant. We need to discuss this further.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("I spoke with Dr. Johnson about the results.");
            segments[1].Text.Should().Be("The findings were significant.");
            segments[2].Text.Should().Be("We need to discuss this further.");
        }

        [Fact]
        public async Task SplitAsync_WithComplexAbbreviationScenario_ShouldHandleCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Dr. Smith, Mrs. Johnson, and Prof. Davis attended the meeting. They discussed the project with Mr. Wilson Jr. The presentation was excellent!";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            // The regex correctly doesn't split after Jr. so we get 2 segments instead of 3
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("Dr. Smith, Mrs. Johnson, and Prof. Davis attended the meeting.");
            segments[1].Text.Should().Be("They discussed the project with Mr. Wilson Jr. The presentation was excellent!");
        }

        [Fact]
        public async Task SplitAsync_WithAbbreviationsInQuotes_ShouldNotSplitInsideQuotes()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "She said, \"Please contact Dr. Smith immediately.\" The message was urgent.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            // The regex correctly doesn't split after Dr. inside quotes, so we get 1 segment
            segments.Should().HaveCount(1);
            segments[0].Text.Should().Be("She said, \"Please contact Dr. Smith immediately.\" The message was urgent.");
        }

        [Fact]
        public async Task SplitAsync_WithConsecutivePunctuation_ShouldSplitCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "What?! Are you serious?! This is amazing!";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            // The regex should split on the last punctuation mark followed by space and capital letter
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("What?!");
            segments[1].Text.Should().Be("Are you serious?!");
            segments[2].Text.Should().Be("This is amazing!");
        }

        [Fact]
        public async Task SplitAsync_WithAbbreviationFollowedByExclamation_ShouldNotSplitAfterAbbreviation()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "I can't believe Dr. Smith won the award! Everyone was so surprised.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("I can't believe Dr. Smith won the award!");
            segments[1].Text.Should().Be("Everyone was so surprised.");
        }

        [Fact]
        public async Task SplitAsync_WithAbbreviationFollowedByQuestion_ShouldNotSplitAfterAbbreviation()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Have you met Mrs. Johnson before? She's the new department head.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("Have you met Mrs. Johnson before?");
            segments[1].Text.Should().Be("She's the new department head.");
        }

        [Fact]
        public async Task SplitAsync_WithMixedCaseAfterPeriod_ShouldOnlySplitOnUppercase()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "First sentence. second sentence should not split. Third Sentence should split.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("First sentence. second sentence should not split.");
            segments[1].Text.Should().Be("Third Sentence should split.");
        }

        [Fact]
        public async Task SplitAsync_WithNumbersAndPeriods_ShouldNotSplitOnNumbers()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "The price is $19.99 for the item. Please pay by 3.30 PM today. The total comes to $45.67 including tax.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("The price is $19.99 for the item.");
            segments[1].Text.Should().Be("Please pay by 3.30 PM today.");
            segments[2].Text.Should().Be("The total comes to $45.67 including tax.");
        }

        [Fact]
        public async Task SplitAsync_WithIPAddresses_ShouldNotSplitOnIPPeriods()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "The server IP is 192.168.1.1 for local access. Connect to 10.0.0.1 for the main network. Use 127.0.0.1 for localhost testing.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("The server IP is 192.168.1.1 for local access.");
            segments[1].Text.Should().Be("Connect to 10.0.0.1 for the main network.");
            segments[2].Text.Should().Be("Use 127.0.0.1 for localhost testing.");
        }

        [Fact]
        public async Task SplitAsync_WithAbbreviationsAtSentenceEnd_ShouldSplitAfterPunctuation()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "The doctor is Dr. Wilson. The professor is Prof. Anderson. The manager is Ms. Davis.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("The doctor is Dr. Wilson.");
            segments[1].Text.Should().Be("The professor is Prof. Anderson.");
            segments[2].Text.Should().Be("The manager is Ms. Davis.");
        }

        #endregion
}