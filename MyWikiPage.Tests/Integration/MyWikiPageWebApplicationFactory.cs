using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyWikiPage.Services;

namespace MyWikiPage.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory for integration tests
/// </summary>
public class MyWikiPageWebApplicationFactory : WebApplicationFactory<Program>
{
    public string TestDirectory { get; private set; } = string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        TestDirectory = Path.Combine(Path.GetTempPath(), "IntegrationTests", Guid.NewGuid().ToString());
        
        builder.ConfigureServices(services =>
        {
            // Override configuration for tests
            services.Configure<Dictionary<string, string>>(options =>
            {
                options["Wiki:MarkdownFolder"] = Path.Combine(TestDirectory, "markdown");
                options["Wiki:OutputFolder"] = Path.Combine(TestDirectory, "output");
            });

            // Add test logging
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        });

        builder.UseEnvironment("Testing");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && Directory.Exists(TestDirectory))
        {
            try
            {
                Directory.Delete(TestDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
        
        base.Dispose(disposing);
    }

    public void CreateTestDirectories()
    {
        Directory.CreateDirectory(Path.Combine(TestDirectory, "markdown"));
        Directory.CreateDirectory(Path.Combine(TestDirectory, "output"));
    }

    public async Task CreateTestMarkdownFile(string filename, string content)
    {
        var filePath = Path.Combine(TestDirectory, "markdown", filename);
        await File.WriteAllTextAsync(filePath, content);
    }

    public async Task CreateTestHtmlFile(string filename, string content)
    {
        var filePath = Path.Combine(TestDirectory, "output", filename);
        await File.WriteAllTextAsync(filePath, content);
    }
}