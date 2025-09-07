using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyWikiPage.Services;

namespace MyWikiPage.Pages
{
    [IgnoreAntiforgeryToken]
    public sealed class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IWikiConfigService _wikiConfig;

        public IndexModel(ILogger<IndexModel> logger, IWikiConfigService wikiConfig)
        {
            _logger = logger;
            _wikiConfig = wikiConfig;
        }

        public string? DefaultPageUrl { get; set; }
        public bool HasGeneratedContent { get; set; }
        public string MarkdownFolderPath { get; set; } = string.Empty;
        public string WikiContent { get; set; } = string.Empty;
        public string PageTitle { get; set; } = "Welcome to MyWikiPage";

        public void OnGet()
        {
            MarkdownFolderPath = _wikiConfig.MarkdownFolderPath;
            CheckForGeneratedContent();
        }

        private void CheckForGeneratedContent()
        {
            try
            {
                DefaultPageUrl = _wikiConfig.GetDefaultPage();
                
                // Check if we have any generated HTML files
                if (Directory.Exists(_wikiConfig.OutputFolderPath))
                {
                    var htmlFiles = Directory.GetFiles(_wikiConfig.OutputFolderPath, "*.html", SearchOption.AllDirectories);
                    HasGeneratedContent = htmlFiles.Length > 0;
                }

                if (!HasGeneratedContent)
                {
                    // Fallback content when no wiki is generated
                    WikiContent = GenerateFallbackContent();
                }
            }
            catch (IOException ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex, "I/O error checking for generated content");
                }
                WikiContent = GenerateFallbackContent();
                HasGeneratedContent = false;
            }
            catch (UnauthorizedAccessException ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex, "Access denied checking for generated content");
                }
                WikiContent = GenerateFallbackContent();
                HasGeneratedContent = false;
            }
        }

        private string GenerateFallbackContent()
        {
            return $@"
<div class=""text-center py-5"">
    <h1 class=""display-4"">Welcome to MyWikiPage</h1>
    <p class=""lead"">A simple markdown-to-HTML wiki generator built with ASP.NET Core Razor Pages.</p>
    
    <div class=""alert alert-info mt-4"">
        <h5><i class=""bi bi-info-circle""></i> Getting Started</h5>
        <p>No wiki content has been generated yet. Follow these steps to get started:</p>
        <ol class=""text-start"">
            <li>Add markdown files (.md) to: <code>{MarkdownFolderPath}</code></li>
            <li>Go to the <a href=""/Wiki"" class=""alert-link"">Wiki Management</a> page</li>
            <li>Click ""Refresh Wiki"" to generate HTML pages</li>
            <li>Return to this page to see your content</li>
        </ol>
    </div>
    
    <div class=""mt-4"">
        <a class=""btn btn-primary btn-lg me-2"" href=""/Wiki"">
            <i class=""bi bi-gear""></i> Manage Wiki
        </a>
        <a class=""btn btn-outline-secondary btn-lg"" href=""https://learn.microsoft.com/aspnet/core"" target=""_blank"">
            <i class=""bi bi-book""></i> Learn More
        </a>
    </div>
</div>";
        }
    }
}
