using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyWikiPage.Services;
using MyWikiPage.Tests.Helpers;

namespace MyWikiPage.Tests.Services;

public class WikiConfigServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly IServiceProvider _serviceProvider;

    public WikiConfigServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "WikiTests", Guid.NewGuid().ToString());
        TestServiceHelper.CreateTestDirectories(_testDirectory);
        
        var configuration = TestServiceHelper.CreateTestConfiguration(new Dictionary<string, string>
        {
            ["Wiki:MarkdownFolder"] = Path.Combine(_testDirectory, "markdown"),
            ["Wiki:OutputFolder"] = Path.Combine(_testDirectory, "output")
        });
        
        _serviceProvider = TestServiceHelper.CreateTestServiceProvider(configuration);
    }

    [Fact]
    public void MarkdownFolderPath_ShouldReturnConfiguredPath()
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<IWikiConfigService>();

        // Act
        var result = service.MarkdownFolderPath;

        // Assert
        result.Should().Be(Path.Combine(_testDirectory, "markdown"));
    }

    [Fact]
    public void OutputFolderPath_ShouldReturnConfiguredPath()
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<IWikiConfigService>();

        // Act
        var result = service.OutputFolderPath;

        // Assert
        result.Should().Be(Path.Combine(_testDirectory, "output"));
    }

    [Fact]
    public void GetDefaultPage_WhenIndexHtmlExists_ShouldReturnIndexUrl()
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<IWikiConfigService>();
        var outputDir = Path.Combine(_testDirectory, "output");
        var indexFile = Path.Combine(outputDir, "index.html");
        
        Directory.CreateDirectory(outputDir);
        File.WriteAllText(indexFile, "<html><body>Test</body></html>");

        // Act
        var result = service.GetDefaultPage();

        // Assert
        result.Should().Be("/wiki/index.html");
    }

    [Fact]
    public void GetDefaultPage_WhenContentsHtmlExists_ShouldReturnContentsUrl()
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<IWikiConfigService>();
        var outputDir = Path.Combine(_testDirectory, "output");
        var contentsFile = Path.Combine(outputDir, "contents.html");
        
        Directory.CreateDirectory(outputDir);
        File.WriteAllText(contentsFile, "<html><body>Contents</body></html>");

        // Act
        var result = service.GetDefaultPage();

        // Assert
        result.Should().Be("/wiki/contents.html");
    }

    [Fact]
    public void GetDefaultPage_WhenNoDefaultFilesExist_ShouldReturnNull()
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<IWikiConfigService>();

        // Act
        var result = service.GetDefaultPage();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetDefaultPage_WhenOutputDirectoryDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var configuration = TestServiceHelper.CreateTestConfiguration(new Dictionary<string, string>
        {
            ["Wiki:MarkdownFolder"] = Path.Combine(_testDirectory, "markdown"),
            ["Wiki:OutputFolder"] = Path.Combine(_testDirectory, "nonexistent")
        });
        
        var serviceProvider = TestServiceHelper.CreateTestServiceProvider(configuration);
        var service = serviceProvider.GetRequiredService<IWikiConfigService>();

        // Act
        var result = service.GetDefaultPage();

        // Assert
        result.Should().BeNull();
    }

    public void Dispose()
    {
        TestServiceHelper.CleanupTestDirectories(_testDirectory);
        _serviceProvider.GetService<IServiceScope>()?.Dispose();
    }
}