using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyWikiPage.Services;

namespace MyWikiPage.Pages
{
    [IgnoreAntiforgeryToken]
    public class WikiModel : PageModel
    {
        private readonly IMarkdownService _markdownService;
        private readonly IWikiConfigService _wikiConfig;
        private readonly ILogger<WikiModel> _logger;

        public WikiModel(IMarkdownService markdownService, IWikiConfigService wikiConfig, ILogger<WikiModel> logger)
        {
            _markdownService = markdownService;
            _wikiConfig = wikiConfig;
            _logger = logger;
        }

        public List<string> GeneratedFiles { get; set; } = new();
        public string? DefaultPageUrl { get; set; }
        public bool HasGeneratedContent { get; set; }
        public string MarkdownFolderPath => _wikiConfig.MarkdownFolderPath;
        public string OutputFolderPath => _wikiConfig.OutputFolderPath;

        [TempData]
        public string? Message { get; set; }

        public void OnGet()
        {
            LoadGeneratedFiles();
            DefaultPageUrl = _wikiConfig.GetDefaultPage();
        }

        public async Task<IActionResult> OnPostRefreshAsync()
        {
            try
            {
                var success = await _markdownService.GenerateHtmlFromMarkdownFolderAsync(
                    _wikiConfig.MarkdownFolderPath, 
                    _wikiConfig.OutputFolderPath);

                if (success)
                {
                    Message = "Wiki pages have been successfully generated!";
                    _logger.LogInformation("Wiki refresh completed successfully");
                }
                else
                {
                    Message = "Failed to generate wiki pages. Check if the markdown folder exists.";
                    _logger.LogWarning("Wiki refresh failed");
                }
            }
            catch (Exception ex)
            {
                Message = $"Error during refresh: {ex.Message}";
                _logger.LogError(ex, "Error during wiki refresh");
            }

            LoadGeneratedFiles();
            DefaultPageUrl = _wikiConfig.GetDefaultPage();
            return Page();
        }

        // New AJAX endpoint for background refresh - anti-forgery disabled for class
        public async Task<IActionResult> OnPostRefreshAjaxAsync()
        {
            _logger.LogInformation("RefreshAjax endpoint called");
            
            try
            {
                var success = await _markdownService.GenerateHtmlFromMarkdownFolderAsync(
                    _wikiConfig.MarkdownFolderPath, 
                    _wikiConfig.OutputFolderPath);

                if (success)
                {
                    _logger.LogInformation("Wiki refresh completed successfully via AJAX");
                    return new JsonResult(new { 
                        success = true, 
                        message = "Wiki pages have been successfully generated!" 
                    });
                }
                else
                {
                    _logger.LogWarning("Wiki refresh failed via AJAX");
                    return new JsonResult(new { 
                        success = false, 
                        message = "Failed to generate wiki pages. Check if the markdown folder exists." 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during wiki refresh via AJAX: {Error}", ex.Message);
                return new JsonResult(new { 
                    success = false, 
                    message = $"Error during refresh: {ex.Message}" 
                });
            }
        }

        private void LoadGeneratedFiles()
        {
            GeneratedFiles.Clear();
            
            if (Directory.Exists(_wikiConfig.OutputFolderPath))
            {
                var htmlFiles = Directory.GetFiles(_wikiConfig.OutputFolderPath, "*.html", SearchOption.AllDirectories);
                GeneratedFiles = htmlFiles
                    .Select(f => _wikiConfig.GetWebPath(f))
                    .OrderBy(f => f)
                    .ToList();
                
                HasGeneratedContent = GeneratedFiles.Any();
            }
        }
    }
}