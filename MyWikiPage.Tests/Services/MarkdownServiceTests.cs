using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyWikiPage.Services;
using MyWikiPage.Tests.Helpers;

namespace MyWikiPage.Tests.Services;

public class MarkdownServiceTests : IDisposable
{
    private readonly IMarkdownService _markdownService;
    private readonly ServiceProvider _serviceProvider;

    public MarkdownServiceTests()
    {
        _serviceProvider = TestServiceHelper.CreateTestServiceProvider();
        _markdownService = _serviceProvider.GetRequiredService<IMarkdownService>();
    }

    [Fact]
    public async Task ConvertMarkdownToHtmlAsync_WithValidMarkdown_ShouldReturnHtml()
    {
        // Arrange
        var markdown = "# Test Header\n\nThis is a test paragraph.";
        var baseDirectory = string.Empty;

        // Act
        var result = await _markdownService.ConvertMarkdownToHtmlAsync(markdown, baseDirectory);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("id=\"test-header\"");
        result.Should().Contain("Test Header");
        result.Should().Contain("<p>");
        result.Should().Contain("This is a test paragraph.");
    }

    [Fact]
    public async Task ConvertMarkdownToHtmlAsync_WithMarkdownLinks_ShouldReturnHtml()
    {
        // Arrange
        var markdown = "Check out [this link](example.com) and [another](test.md).";
        var baseDirectory = string.Empty;

        // Act
        var result = await _markdownService.ConvertMarkdownToHtmlAsync(markdown, baseDirectory);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("<a href=\"example.com\">this link</a>");
        result.Should().Contain("<a href=\"test.md\">another</a>");
    }

    [Fact]
    public async Task ConvertMarkdownToHtmlAsync_WithComplexMarkdown_ShouldReturnFormattedHtml()
    {
        // Arrange
        var markdown = "# Header\n\n- List item 1\n- List item 2\n\n```csharp\nvar x = 1;\n```";
        var baseDirectory = string.Empty;

        // Act
        var result = await _markdownService.ConvertMarkdownToHtmlAsync(markdown, baseDirectory);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("id=\"header\"");
        result.Should().Contain("<ul>");
        result.Should().Contain("<li>");
        result.Should().Contain("language-csharp");
    }

    [Fact]
    public async Task ConvertMarkdownToHtmlAsync_WithNullMarkdown_ShouldHandleGracefully()
    {
        // Arrange
        string markdown = null!;
        var baseDirectory = string.Empty;

        // Act & Assert
        var result = await _markdownService.ConvertMarkdownToHtmlAsync(markdown, baseDirectory);
        
        // The service should handle null gracefully and return some form of error content
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateHtmlFromMarkdownFolderAsync_WithValidMarkdownFiles_ShouldReturnTrue()
    {
        // Arrange
        var testDirectory = Path.Combine(Path.GetTempPath(), "MarkdownServiceTests", Guid.NewGuid().ToString());
        var markdownDir = Path.Combine(testDirectory, "markdown");
        var outputDir = Path.Combine(testDirectory, "output");

        try
        {
            Directory.CreateDirectory(markdownDir);
            Directory.CreateDirectory(outputDir);

            var testMarkdown = "# Test Page\n\nThis is a test page.";
            await File.WriteAllTextAsync(Path.Combine(markdownDir, "test.md"), testMarkdown);

            // Act
            var result = await _markdownService.GenerateHtmlFromMarkdownFolderAsync(markdownDir, outputDir);

            // Assert
            result.Should().BeTrue();
            var htmlFile = Path.Combine(outputDir, "test.html");
            File.Exists(htmlFile).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }
    }

    [Fact]
    public async Task GenerateHtmlFromMarkdownFolderAsync_WithNonExistentDirectory_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), "NonExistent", Guid.NewGuid().ToString());
        var outputDir = Path.Combine(Path.GetTempPath(), "Output", Guid.NewGuid().ToString());

        // Act
        var result = await _markdownService.GenerateHtmlFromMarkdownFolderAsync(nonExistentDir, outputDir);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateHtmlFromMarkdownFolderAsync_WithEmptyDirectory_ShouldReturnTrue()
    {
        // Arrange
        var testDirectory = Path.Combine(Path.GetTempPath(), "MarkdownServiceTests", Guid.NewGuid().ToString());
        var markdownDir = Path.Combine(testDirectory, "markdown");
        var outputDir = Path.Combine(testDirectory, "output");

        try
        {
            Directory.CreateDirectory(markdownDir);
            Directory.CreateDirectory(outputDir);

            // Act
            var result = await _markdownService.GenerateHtmlFromMarkdownFolderAsync(markdownDir, outputDir);

            // Assert
            result.Should().BeTrue(); // Should succeed even with no files
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }
    }

    [Fact]
    public void ProcessInternalLinks_WithMarkdownLinks_ShouldConvertToHtmlLinks()
    {
        // Arrange
        var html = @"<p>Check out <a href=""test.md"">this page</a> and <a href=""other.md"">another</a>.</p>";
        var baseDirectory = "/base";
        var outputDirectory = "/output";

        // Act
        var result = _markdownService.ProcessInternalLinks(html, baseDirectory, outputDirectory);

        // Assert
        result.Should().Contain(@"href=""test.html""");
        result.Should().Contain(@"href=""other.html""");
        result.Should().NotContain(".md");
    }

    [Fact]
    public void ProcessInternalLinks_WithExternalLinks_ShouldLeaveUnchanged()
    {
        // Arrange
        var html = @"<p>Check out <a href=""https://example.com"">this site</a> and <a href=""mailto:test@example.com"">email</a>.</p>";
        var baseDirectory = "/base";
        var outputDirectory = "/output";

        // Act
        var result = _markdownService.ProcessInternalLinks(html, baseDirectory, outputDirectory);

        // Assert
        result.Should().Contain(@"href=""https://example.com""");
        result.Should().Contain(@"href=""mailto:test@example.com""");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}