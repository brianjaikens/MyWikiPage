using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyWikiPage.Pages;
using MyWikiPage.Services;
using MyWikiPage.Tests.Helpers;
using Xunit;

namespace MyWikiPage.Tests.Pages;

public class WikiEmbedModelTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ServiceProvider _serviceProvider;

    public WikiEmbedModelTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "WikiEmbedTests", Guid.NewGuid().ToString());
        TestServiceHelper.CreateTestDirectories(_testDirectory);
        
        var configuration = TestServiceHelper.CreateTestConfiguration(new Dictionary<string, string>
        {
            ["Wiki:MarkdownFolder"] = Path.Combine(_testDirectory, "markdown"),
            ["Wiki:OutputFolder"] = Path.Combine(_testDirectory, "output")
        });
        
        _serviceProvider = TestServiceHelper.CreateTestServiceProvider(configuration, _testDirectory);
    }

    [Fact]
    public void OnGet_WithValidDefaultPage_ShouldLoadContent()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<WikiEmbedModel>>();
        var wikiConfig = _serviceProvider.GetRequiredService<IWikiConfigService>();
        
        // Create test HTML file
        var outputDir = Path.Combine(_testDirectory, "output");
        Directory.CreateDirectory(outputDir);
        var testHtml = @"<html><body><h1>Test Page</h1><p>Test content with <a href=""other.html"">link</a></p></body></html>";
        File.WriteAllText(Path.Combine(outputDir, "index.html"), testHtml);
        
        var model = new WikiEmbedModel(wikiConfig, logger);

        // Act
        model.OnGet(theme: "light");

        // Assert
        model.HasContent.Should().BeTrue();
        model.Content.Should().Contain("Test Page");
        model.Content.Should().Contain("Test content");
        model.PageTitle.Should().Be("Test Page");
        model.Theme.Should().Be("light");
    }

    [Fact]
    public void OnGet_WithSpecificPage_ShouldLoadRequestedPage()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<WikiEmbedModel>>();
        var wikiConfig = _serviceProvider.GetRequiredService<IWikiConfigService>();
        
        // Create test HTML files
        var outputDir = Path.Combine(_testDirectory, "output");
        Directory.CreateDirectory(outputDir);
        
        var indexHtml = @"<html><body><h1>Index</h1></body></html>";
        var contentsHtml = @"<html><body><h1>Contents Page</h1><p>This is contents</p></body></html>";
        
        File.WriteAllText(Path.Combine(outputDir, "index.html"), indexHtml);
        File.WriteAllText(Path.Combine(outputDir, "contents.html"), contentsHtml);
        
        var model = new WikiEmbedModel(wikiConfig, logger);

        // Act
        model.OnGet(theme: "dark", page: "contents.html");

        // Assert
        model.HasContent.Should().BeTrue();
        model.Content.Should().Contain("Contents Page");
        model.Content.Should().Contain("This is contents");
        model.PageTitle.Should().Be("Contents Page");
        model.Theme.Should().Be("dark");
        model.RequestedPage.Should().Be("contents.html");
    }

    [Fact]
    public void OnGet_WithNonExistentPage_ShouldFallbackToDefault()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<WikiEmbedModel>>();
        var wikiConfig = _serviceProvider.GetRequiredService<IWikiConfigService>();
        
        // Create only index file
        var outputDir = Path.Combine(_testDirectory, "output");
        Directory.CreateDirectory(outputDir);
        var indexHtml = @"<html><body><h1>Index</h1></body></html>";
        File.WriteAllText(Path.Combine(outputDir, "index.html"), indexHtml);
        
        var model = new WikiEmbedModel(wikiConfig, logger);

        // Act
        model.OnGet(theme: "light", page: "nonexistent.html");

        // Assert
        model.HasContent.Should().BeTrue();
        model.Content.Should().Contain("Index");
        model.RequestedPage.Should().Be("nonexistent.html");
    }

    [Fact]
    public void OnGet_WithLinksInContent_ShouldRewriteLinksForIframe()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<WikiEmbedModel>>();
        var wikiConfig = _serviceProvider.GetRequiredService<IWikiConfigService>();
        
        // Create test HTML file with internal links
        var outputDir = Path.Combine(_testDirectory, "output");
        Directory.CreateDirectory(outputDir);
        var testHtml = @"<html><body>
            <h1>Test Page</h1>
            <p>Check out <a href=""contents.html"">Contents</a> and <a href=""features.html"">Features</a></p>
            <p>External: <a href=""https://example.com"">Example</a></p>
        </body></html>";
        File.WriteAllText(Path.Combine(outputDir, "index.html"), testHtml);
        
        var model = new WikiEmbedModel(wikiConfig, logger);

        // Act
        model.OnGet(theme: "light");

        // Assert
        model.HasContent.Should().BeTrue();
        
        // Internal links should be rewritten to WikiEmbed URLs
        model.Content.Should().Contain("/wiki-embed?theme=light&page=contents.html");
        model.Content.Should().Contain("/wiki-embed?theme=light&page=features.html");
        
        // External links should remain unchanged
        model.Content.Should().Contain("https://example.com");
    }

    [Fact]
    public void OnGet_WithNoContent_ShouldSetHasContentFalse()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<WikiEmbedModel>>();
        var wikiConfig = _serviceProvider.GetRequiredService<IWikiConfigService>();
        
        var model = new WikiEmbedModel(wikiConfig, logger);

        // Act
        model.OnGet();

        // Assert
        model.HasContent.Should().BeFalse();
        model.Content.Should().BeEmpty();
        model.Theme.Should().Be("light"); // Default theme
    }

    [Fact]
    public void OnGet_WithDefaultTheme_ShouldUseLightTheme()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<WikiEmbedModel>>();
        var wikiConfig = _serviceProvider.GetRequiredService<IWikiConfigService>();
        
        var model = new WikiEmbedModel(wikiConfig, logger);

        // Act
        model.OnGet(); // No theme specified

        // Assert
        model.Theme.Should().Be("light");
    }

    [Fact]
    public void OnGet_ShouldRemoveNavigationDivsFromContent()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<WikiEmbedModel>>();
        var wikiConfig = _serviceProvider.GetRequiredService<IWikiConfigService>();
        
        // Create test HTML file with navigation div
        var outputDir = Path.Combine(_testDirectory, "output");
        Directory.CreateDirectory(outputDir);
        var testHtml = @"<html><body>
            <div class=""nav"">
                <a href=""index.html"">Home</a>
                <a href=""contents.html"">Contents</a>
            </div>
            <h1>Test Page</h1>
            <p>Content here</p>
        </body></html>";
        File.WriteAllText(Path.Combine(outputDir, "index.html"), testHtml);
        
        var model = new WikiEmbedModel(wikiConfig, logger);

        // Act
        model.OnGet(theme: "light");

        // Assert
        model.HasContent.Should().BeTrue();
        model.Content.Should().NotContain("class=\"nav\"");
        model.Content.Should().Contain("Test Page");
        model.Content.Should().Contain("Content here");
    }

    public void Dispose()
    {
        TestServiceHelper.CleanupTestDirectories(_testDirectory);
        _serviceProvider?.Dispose();
    }
}