using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using MyWikiPage.Services;

namespace MyWikiPage.Tests.Helpers;

/// <summary>
/// Test helper for creating service instances with test configuration
/// </summary>
public static class TestServiceHelper
{
    /// <summary>
    /// Creates a test configuration with in-memory values
    /// </summary>
    public static IConfiguration CreateTestConfiguration(Dictionary<string, string>? configValues = null)
    {
        var defaultConfig = new Dictionary<string, string>
        {
            ["Wiki:MarkdownFolder"] = "TestData/markdown",
            ["Wiki:OutputFolder"] = "TestData/output"
        };

        if (configValues != null)
        {
            foreach (var kvp in configValues)
            {
                defaultConfig[kvp.Key] = kvp.Value;
            }
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(defaultConfig!)
            .Build();
    }

    /// <summary>
    /// Creates a test service provider with required services
    /// </summary>
    public static ServiceProvider CreateTestServiceProvider(IConfiguration? configuration = null, string? testDirectory = null)
    {
        var services = new ServiceCollection();
        
        configuration ??= CreateTestConfiguration();
        services.AddSingleton(configuration);
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add mock web host environment
        var webHostEnvironment = new TestWebHostEnvironment(testDirectory ?? Path.GetTempPath());
        services.AddSingleton<IWebHostEnvironment>(webHostEnvironment);
        
        // Add application services
        services.AddScoped<IWikiConfigService, WikiConfigService>();
        services.AddScoped<IMarkdownService, MarkdownService>();
        
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Creates a test directory structure
    /// </summary>
    public static void CreateTestDirectories(string baseDirectory)
    {
        var markdownDir = Path.Combine(baseDirectory, "markdown");
        var outputDir = Path.Combine(baseDirectory, "output");
        
        Directory.CreateDirectory(markdownDir);
        Directory.CreateDirectory(outputDir);
    }

    /// <summary>
    /// Cleans up test directories
    /// </summary>
    public static void CleanupTestDirectories(string baseDirectory)
    {
        if (Directory.Exists(baseDirectory))
        {
            try
            {
                Directory.Delete(baseDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    private class TestWebHostEnvironment : IWebHostEnvironment
    {
        public TestWebHostEnvironment(string contentRoot)
        {
            ContentRootPath = contentRoot;
            WebRootPath = Path.Combine(contentRoot, "wwwroot");
            EnvironmentName = "Test";
            ApplicationName = "MyWikiPage.Tests";
        }

        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; }
        public string EnvironmentName { get; set; }
    }
}