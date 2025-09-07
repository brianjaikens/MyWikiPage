using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MyWikiPage.Pages;
using MyWikiPage.Services;
using MyWikiPage.Tests.Helpers;
using MyWikiPage.Tests.TestData;

namespace MyWikiPage.Tests.Pages;

public class IndexModelTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly IServiceProvider _serviceProvider;

    public IndexModelTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "IndexTests", Guid.NewGuid().ToString());
        TestServiceHelper.CreateTestDirectories(_testDirectory);
        
        var configuration = TestServiceHelper.CreateTestConfiguration(new Dictionary<string, string>
        {
            ["Wiki:MarkdownFolder"] = Path.Combine(_testDirectory, "markdown"),
            ["Wiki:OutputFolder"] = Path.Combine(_testDirectory, "output")
        });
        
        _serviceProvider = TestServiceHelper.CreateTestServiceProvider(configuration);
    }

    [Fact]
    public void OnGet_WithGeneratedContent_ShouldSetHasGeneratedContentTrue()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<IndexModel>>();
        var wikiConfig = _serviceProvider.GetRequiredService<IWikiConfigService>();
        
        // Create a test HTML file
        var outputDir = Path.Combine(_testDirectory, "output");
        Directory.CreateDirectory(outputDir);
        File.WriteAllText(Path.Combine(outputDir, "index.html"), "<html><body>Test</body></html>");
        
        var model = new IndexModel(logger, wikiConfig);

        // Act
        model.OnGet();

        // Assert
        model.HasGeneratedContent.Should().BeTrue();
        model.DefaultPageUrl.Should().Be("/wiki/index.html");
    }

    [Fact]
    public void OnGet_WithoutGeneratedContent_ShouldSetHasGeneratedContentFalse()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<IndexModel>>();
        var wikiConfig = _serviceProvider.GetRequiredService<IWikiConfigService>();
        
        var model = new IndexModel(logger, wikiConfig);

        // Act
        model.OnGet();

        // Assert
        model.HasGeneratedContent.Should().BeFalse();
        model.WikiContent.Should().NotBeNullOrEmpty();
        model.WikiContent.Should().Contain("Welcome to MyWikiPage");
    }

    [Fact]
    public void OnGet_WithContentsFile_ShouldPreferContentsOverIndex()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<IndexModel>>();
        var wikiConfig = _serviceProvider.GetRequiredService<IWikiConfigService>();
        
        // Create both index and contents files
        var outputDir = Path.Combine(_testDirectory, "output");
        Directory.CreateDirectory(outputDir);
        File.WriteAllText(Path.Combine(outputDir, "index.html"), "<html><body>Index</body></html>");
        File.WriteAllText(Path.Combine(outputDir, "contents.html"), "<html><body>Contents</body></html>");
        
        var model = new IndexModel(logger, wikiConfig);

        // Act
        model.OnGet();

        // Assert
        model.HasGeneratedContent.Should().BeTrue();
        model.DefaultPageUrl.Should().Be("/wiki/contents.html");
    }

    [Fact]
    public void PageTitle_DefaultValue_ShouldBeCorrect()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<IndexModel>>();
        var wikiConfig = _serviceProvider.GetRequiredService<IWikiConfigService>();
        
        var model = new IndexModel(logger, wikiConfig);

        // Act & Assert
        model.PageTitle.Should().Be("Welcome to MyWikiPage");
    }

    [Fact]
    public void MarkdownFolderPath_ShouldReturnConfiguredPath()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<IndexModel>>();
        var wikiConfig = _serviceProvider.GetRequiredService<IWikiConfigService>();
        
        var model = new IndexModel(logger, wikiConfig);

        // Act
        model.OnGet();

        // Assert
        model.MarkdownFolderPath.Should().Be(Path.Combine(_testDirectory, "markdown"));
    }

    public void Dispose()
    {
        TestServiceHelper.CleanupTestDirectories(_testDirectory);
        _serviceProvider.GetService<IServiceScope>()?.Dispose();
    }
}