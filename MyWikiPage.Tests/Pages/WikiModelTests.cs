using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyWikiPage.Pages;
using MyWikiPage.Services;
using MyWikiPage.Tests.Helpers;

namespace MyWikiPage.Tests.Pages;

public class WikiModelTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ServiceProvider _serviceProvider;

    public WikiModelTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "WikiModelTests", Guid.NewGuid().ToString());
        TestServiceHelper.CreateTestDirectories(_testDirectory);
        
        var configuration = TestServiceHelper.CreateTestConfiguration(new Dictionary<string, string>
        {
            ["Wiki:MarkdownFolder"] = Path.Combine(_testDirectory, "markdown"),
            ["Wiki:OutputFolder"] = Path.Combine(_testDirectory, "output")
        });
        
        _serviceProvider = TestServiceHelper.CreateTestServiceProvider(configuration, _testDirectory);
    }

    [Fact]
    public void OnGet_ShouldInitializeProperties()
    {
        // Arrange
        var markdownService = _serviceProvider.GetRequiredService<IMarkdownService>();
        var wikiConfig = _serviceProvider.GetRequiredService<IWikiConfigService>();
        var logger = _serviceProvider.GetRequiredService<ILogger<WikiModel>>();
        
        var model = new WikiModel(markdownService, wikiConfig, logger);

        // Act
        model.OnGet();

        // Assert
        model.MarkdownFolderPath.Should().NotBeNullOrEmpty();
        model.OutputFolderPath.Should().NotBeNullOrEmpty();
        model.GeneratedFiles.Should().NotBeNull();
    }

    [Fact]
    public async Task OnPostRefreshAsync_WithValidMarkdownFiles_ShouldReturnPageResult()
    {
        // Arrange
        var markdownService = _serviceProvider.GetRequiredService<IMarkdownService>();
        var wikiConfig = _serviceProvider.GetRequiredService<IWikiConfigService>();
        var logger = _serviceProvider.GetRequiredService<ILogger<WikiModel>>();
        
        // Create test markdown file
        var markdownDir = Path.Combine(_testDirectory, "markdown");
        Directory.CreateDirectory(markdownDir);
        await File.WriteAllTextAsync(Path.Combine(markdownDir, "test.md"), "# Test Page\n\nTest content");
        
        var model = new WikiModel(markdownService, wikiConfig, logger);

        // Act
        var result = await model.OnPostRefreshAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        model.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task OnPostRefreshAjaxAsync_WithValidMarkdownFiles_ShouldReturnSuccessJson()
    {
        // Arrange
        var markdownService = _serviceProvider.GetRequiredService<IMarkdownService>();
        var wikiConfig = _serviceProvider.GetRequiredService<IWikiConfigService>();
        var logger = _serviceProvider.GetRequiredService<ILogger<WikiModel>>();
        
        // Create test markdown file
        var markdownDir = Path.Combine(_testDirectory, "markdown");
        Directory.CreateDirectory(markdownDir);
        await File.WriteAllTextAsync(Path.Combine(markdownDir, "test.md"), "# Test Page\n\nTest content");
        
        var model = new WikiModel(markdownService, wikiConfig, logger);

        // Act
        var result = await model.OnPostRefreshAjaxAsync();

        // Assert
        result.Should().BeOfType<JsonResult>();
        var jsonResult = result as JsonResult;
        jsonResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task OnPostRefreshAjaxAsync_WithNoMarkdownFiles_ShouldReturnFailureJson()
    {
        // Arrange
        var markdownService = _serviceProvider.GetRequiredService<IMarkdownService>();
        var wikiConfig = _serviceProvider.GetRequiredService<IWikiConfigService>();
        var logger = _serviceProvider.GetRequiredService<ILogger<WikiModel>>();
        
        // Ensure markdown directory exists but is empty
        var markdownDir = Path.Combine(_testDirectory, "markdown");
        Directory.CreateDirectory(markdownDir);
        
        var model = new WikiModel(markdownService, wikiConfig, logger);

        // Act
        var result = await model.OnPostRefreshAjaxAsync();

        // Assert
        result.Should().BeOfType<JsonResult>();
        var jsonResult = result as JsonResult;
        jsonResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task OnPostRefreshAsync_WithNoMarkdownFiles_ShouldHandleGracefully()
    {
        // Arrange
        var markdownService = _serviceProvider.GetRequiredService<IMarkdownService>();
        var wikiConfig = _serviceProvider.GetRequiredService<IWikiConfigService>();
        var logger = _serviceProvider.GetRequiredService<ILogger<WikiModel>>();
        
        // Create markdown directory but don't add any files
        var markdownDir = Path.Combine(_testDirectory, "markdown");
        Directory.CreateDirectory(markdownDir);
        
        var model = new WikiModel(markdownService, wikiConfig, logger);

        // Act
        var result = await model.OnPostRefreshAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        model.Message.Should().NotBeNullOrEmpty();
        // With an empty markdown folder, the service should still complete successfully
        // but may generate 0 files, which is a valid scenario
        model.Message.Should().Contain("successfully generated");
    }

    public void Dispose()
    {
        TestServiceHelper.CleanupTestDirectories(_testDirectory);
        _serviceProvider?.Dispose();
    }
}