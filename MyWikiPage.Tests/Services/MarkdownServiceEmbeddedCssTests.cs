using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyWikiPage.Services;
using MyWikiPage.Tests.Helpers;

namespace MyWikiPage.Tests.Services;

public class MarkdownServiceEmbeddedCssTests : IDisposable
{
    private readonly IMarkdownService _markdownService;
    private readonly ServiceProvider _serviceProvider;
    private readonly string _testDirectory;

    public MarkdownServiceEmbeddedCssTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "MarkdownServiceEmbeddedCssTests", Guid.NewGuid().ToString());
        
        var configuration = TestServiceHelper.CreateTestConfiguration();
        _serviceProvider = TestServiceHelper.CreateTestServiceProvider(configuration, _testDirectory);
        _markdownService = _serviceProvider.GetRequiredService<IMarkdownService>();
        
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public async Task GenerateHtmlFromMarkdownFolderAsync_ShouldCreateHtmlWithEmbeddedCss()
    {
        // Arrange
        var markdownDir = Path.Combine(_testDirectory, "markdown");
        var outputDir = Path.Combine(_testDirectory, "output");
        Directory.CreateDirectory(markdownDir);
        Directory.CreateDirectory(outputDir);

        var testMarkdown = @"# Test Page

This is a test with **bold** and *italic* text.

## Code Example

```csharp
public class Test
{
    public string Name { get; set; } = ""Hello"";
}
```

## Table

| Column 1 | Column 2 |
|----------|----------|
| Data 1   | Data 2   |

> This is a blockquote

- List item 1
- List item 2";

        await File.WriteAllTextAsync(Path.Combine(markdownDir, "test.md"), testMarkdown);

        // Act
        var result = await _markdownService.GenerateHtmlFromMarkdownFolderAsync(markdownDir, outputDir);

        // Assert
        result.Should().BeTrue();
        
        var htmlFile = Path.Combine(outputDir, "test.html");
        File.Exists(htmlFile).Should().BeTrue();
        
        var htmlContent = await File.ReadAllTextAsync(htmlFile);
        
        // Verify HTML structure
        htmlContent.Should().Contain("<!DOCTYPE html>");
        htmlContent.Should().Contain("<html lang=\"en\">");
        htmlContent.Should().Contain("<head>");
        htmlContent.Should().Contain("</head>");
        htmlContent.Should().Contain("<body>");
        htmlContent.Should().Contain("</body>");
        htmlContent.Should().Contain("</html>");
    }

    [Fact]
    public async Task GeneratedHtml_ShouldContainEmbeddedCssWithThemeVariables()
    {
        // Arrange
        var markdownDir = Path.Combine(_testDirectory, "markdown");
        var outputDir = Path.Combine(_testDirectory, "output");
        Directory.CreateDirectory(markdownDir);
        Directory.CreateDirectory(outputDir);

        var simpleMarkdown = "# Simple Test";
        await File.WriteAllTextAsync(Path.Combine(markdownDir, "simple.md"), simpleMarkdown);

        // Act
        await _markdownService.GenerateHtmlFromMarkdownFolderAsync(markdownDir, outputDir);

        // Assert
        var htmlContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "simple.html"));
        
        // Check for embedded CSS with theme variables
        htmlContent.Should().Contain("<style>");
        htmlContent.Should().Contain(":root {");
        htmlContent.Should().Contain("--bs-body-bg: #ffffff;");
        htmlContent.Should().Contain("--bs-body-color: #212529;");
        htmlContent.Should().Contain("[data-theme=\"dark\"] {");
        htmlContent.Should().Contain("--bs-body-bg: #121212;");
        htmlContent.Should().Contain("--bs-body-color: #e9ecef;");
        htmlContent.Should().Contain("</style>");
    }

    [Fact]
    public async Task GeneratedHtml_ShouldContainThemeToggleButton()
    {
        // Arrange
        var markdownDir = Path.Combine(_testDirectory, "markdown");
        var outputDir = Path.Combine(_testDirectory, "output");
        Directory.CreateDirectory(markdownDir);
        Directory.CreateDirectory(outputDir);

        var markdown = "# Theme Test Page";
        await File.WriteAllTextAsync(Path.Combine(markdownDir, "theme-test.md"), markdown);

        // Act
        await _markdownService.GenerateHtmlFromMarkdownFolderAsync(markdownDir, outputDir);

        // Assert
        var htmlContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "theme-test.html"));
        
        // Check for theme toggle button
        htmlContent.Should().Contain("class=\"theme-toggle\"");
        htmlContent.Should().Contain("onclick=\"toggleTheme()\"");
        htmlContent.Should().Contain("Toggle Dark/Light Mode");
        htmlContent.Should().Contain("id=\"theme-icon\"");
        htmlContent.Should().Contain("bi bi-moon");
    }

    [Fact]
    public async Task GeneratedHtml_ShouldContainBootstrapIcons()
    {
        // Arrange
        var markdownDir = Path.Combine(_testDirectory, "markdown");
        var outputDir = Path.Combine(_testDirectory, "output");
        Directory.CreateDirectory(markdownDir);
        Directory.CreateDirectory(outputDir);

        var markdown = "# Icons Test";
        await File.WriteAllTextAsync(Path.Combine(markdownDir, "icons.md"), markdown);

        // Act
        await _markdownService.GenerateHtmlFromMarkdownFolderAsync(markdownDir, outputDir);

        // Assert
        var htmlContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "icons.html"));
        
        // Check for Bootstrap Icons CDN link
        htmlContent.Should().Contain("https://cdn.jsdelivr.net/npm/bootstrap-icons");
        
        // Check for navigation icons
        htmlContent.Should().Contain("bi bi-house");
        htmlContent.Should().Contain("bi bi-list");
        htmlContent.Should().Contain("bi bi-arrow-left");
    }

    [Fact]
    public async Task GeneratedHtml_ShouldContainResponsiveNavigation()
    {
        // Arrange
        var markdownDir = Path.Combine(_testDirectory, "markdown");
        var outputDir = Path.Combine(_testDirectory, "output");
        Directory.CreateDirectory(markdownDir);
        Directory.CreateDirectory(outputDir);

        var markdown = "# Navigation Test";
        await File.WriteAllTextAsync(Path.Combine(markdownDir, "nav.md"), markdown);

        // Act
        await _markdownService.GenerateHtmlFromMarkdownFolderAsync(markdownDir, outputDir);

        // Assert
        var htmlContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "nav.html"));
        
        // Check for navigation structure
        htmlContent.Should().Contain("<div class=\"nav\">");
        htmlContent.Should().Contain("href=\"index.html\"");
        htmlContent.Should().Contain("href=\"contents.html\"");
        htmlContent.Should().Contain("href=\"/wiki\"");
        
        // Check for responsive CSS
        htmlContent.Should().Contain("@media (max-width: 768px)");
        htmlContent.Should().Contain("flex-direction: column");
    }

    [Fact]
    public async Task GeneratedHtml_ShouldContainThemeManagementJavaScript()
    {
        // Arrange
        var markdownDir = Path.Combine(_testDirectory, "markdown");
        var outputDir = Path.Combine(_testDirectory, "output");
        Directory.CreateDirectory(markdownDir);
        Directory.CreateDirectory(outputDir);

        var markdown = "# JavaScript Test";
        await File.WriteAllTextAsync(Path.Combine(markdownDir, "js.md"), markdown);

        // Act
        await _markdownService.GenerateHtmlFromMarkdownFolderAsync(markdownDir, outputDir);

        // Assert
        var htmlContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "js.html"));
        
        // Check for JavaScript functions
        htmlContent.Should().Contain("<script>");
        htmlContent.Should().Contain("function getTheme()");
        htmlContent.Should().Contain("function setTheme(theme)");
        htmlContent.Should().Contain("function toggleTheme()");
        htmlContent.Should().Contain("localStorage.getItem('theme')");
        htmlContent.Should().Contain("localStorage.setItem('theme', theme)");
        htmlContent.Should().Contain("document.documentElement.setAttribute('data-theme', theme)");
        htmlContent.Should().Contain("</script>");
    }

    [Fact]
    public async Task GeneratedHtml_ShouldHaveProperStylingForAllElements()
    {
        // Arrange
        var markdownDir = Path.Combine(_testDirectory, "markdown");
        var outputDir = Path.Combine(_testDirectory, "output");
        Directory.CreateDirectory(markdownDir);
        Directory.CreateDirectory(outputDir);

        var complexMarkdown = @"# Main Heading

## Sub Heading

### Smaller Heading

This is a paragraph with **bold** and *italic* text.

```javascript
function test() {
    console.log('Hello World');
}
```

| Header 1 | Header 2 |
|----------|----------|
| Cell 1   | Cell 2   |

> This is a blockquote

1. Ordered list item 1
2. Ordered list item 2

- Unordered list item 1
- Unordered list item 2

[Link to something](https://example.com)";

        await File.WriteAllTextAsync(Path.Combine(markdownDir, "complex.md"), complexMarkdown);

        // Act
        await _markdownService.GenerateHtmlFromMarkdownFolderAsync(markdownDir, outputDir);

        // Assert
        var htmlContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "complex.html"));
        
        // Check for proper CSS styling for all elements
        htmlContent.Should().Contain("h1, h2, h3, h4, h5, h6 {");
        htmlContent.Should().Contain("pre {");
        htmlContent.Should().Contain("code {");
        htmlContent.Should().Contain("table {");
        htmlContent.Should().Contain("th, td {");
        htmlContent.Should().Contain("blockquote {");
        htmlContent.Should().Contain("ul, ol {");
        htmlContent.Should().Contain("a {");
        
        // Check for dark mode styles
        htmlContent.Should().Contain("[data-theme=\"dark\"] a {");
        htmlContent.Should().Contain("color: #86b7fe;");
    }

    [Fact]
    public async Task GeneratedHtml_ShouldBeValidHtml5()
    {
        // Arrange
        var markdownDir = Path.Combine(_testDirectory, "markdown");
        var outputDir = Path.Combine(_testDirectory, "output");
        Directory.CreateDirectory(markdownDir);
        Directory.CreateDirectory(outputDir);

        var markdown = "# HTML5 Validation Test\n\nThis tests HTML5 validity.";
        await File.WriteAllTextAsync(Path.Combine(markdownDir, "html5.md"), markdown);

        // Act
        await _markdownService.GenerateHtmlFromMarkdownFolderAsync(markdownDir, outputDir);

        // Assert
        var htmlContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "html5.html"));
        
        // Check for HTML5 doctype and structure
        htmlContent.Should().StartWith("<!DOCTYPE html>");
        htmlContent.Should().Contain("lang=\"en\"");
        htmlContent.Should().Contain("charset=\"UTF-8\"");
        htmlContent.Should().Contain("name=\"viewport\"");
        htmlContent.Should().Contain("content=\"width=device-width, initial-scale=1.0\"");
        
        // Ensure proper nesting
        htmlContent.Should().Match("*<html*>*<head>*</head>*<body>*</body>*</html>*");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
        _serviceProvider?.Dispose();
    }
}