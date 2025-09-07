using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyWikiPage.Services;
using System.Collections.ObjectModel;

namespace MyWikiPage.Pages
{
    [IgnoreAntiforgeryToken]
    public sealed class WikiModel : PageModel
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

        private readonly List<string> _generatedFiles = [];
        public ReadOnlyCollection<string> GeneratedFiles => _generatedFiles.AsReadOnly();
        public string? DefaultPageUrl { get; set; }
        public bool HasGeneratedContent { get; set; }
        public string MarkdownFolderPath { get; set; } = string.Empty;
        public string OutputFolderPath { get; set; } = string.Empty;
        public string? Message { get; set; }

        public void OnGet()
        {
            MarkdownFolderPath = _wikiConfig.MarkdownFolderPath;
            OutputFolderPath = _wikiConfig.OutputFolderPath;
            LoadGeneratedFiles();
            DefaultPageUrl = _wikiConfig.GetDefaultPage();
        }

        public async Task<IActionResult> OnPostRefreshAsync()
        {
            try
            {
                var success = await _markdownService.GenerateHtmlFromMarkdownFolderAsync(
                    _wikiConfig.MarkdownFolderPath, 
                    _wikiConfig.OutputFolderPath).ConfigureAwait(false);

                if (success)
                {
                    Message = "Wiki pages have been successfully generated!";
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("Wiki refresh completed successfully");
                    }
                }
                else
                {
                    Message = "Failed to generate wiki pages. Check if the markdown folder exists.";
                    if (_logger.IsEnabled(LogLevel.Warning))
                    {
                        _logger.LogWarning("Wiki refresh failed");
                    }
                }
            }
            catch (IOException ex)
            {
                Message = "I/O error occurred during wiki generation.";
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex, "I/O error during wiki refresh");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Message = "Access denied when generating wiki pages.";
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex, "Access denied during wiki refresh");
                }
            }

            LoadGeneratedFiles();
            DefaultPageUrl = _wikiConfig.GetDefaultPage();
            return Page();
        }

        // New AJAX endpoint for background refresh - anti-forgery disabled for class
        public async Task<IActionResult> OnPostRefreshAjaxAsync()
        {
            _logger.LogInformation("RefreshAjax endpoint called");
            _logger.LogInformation("Markdown folder path: {MarkdownPath}", _wikiConfig.MarkdownFolderPath);
            _logger.LogInformation("Output folder path: {OutputPath}", _wikiConfig.OutputFolderPath);
            
            try
            {
                // Check if markdown folder exists
                if (!Directory.Exists(_wikiConfig.MarkdownFolderPath))
                {
                    _logger.LogWarning("Markdown folder does not exist: {MarkdownPath}", _wikiConfig.MarkdownFolderPath);
                    return new JsonResult(new { 
                        success = false, 
                        message = $"Markdown folder does not exist: {_wikiConfig.MarkdownFolderPath}" 
                    });
                }

                // Check if there are any markdown files
                var markdownFiles = Directory.GetFiles(_wikiConfig.MarkdownFolderPath, "*.md", SearchOption.AllDirectories);
                _logger.LogInformation("Found {FileCount} markdown files in {MarkdownPath}", markdownFiles.Length, _wikiConfig.MarkdownFolderPath);
                
                if (markdownFiles.Length == 0)
                {
                    return new JsonResult(new { 
                        success = false, 
                        message = "No markdown files found in the markdown folder." 
                    });
                }

                var success = await _markdownService.GenerateHtmlFromMarkdownFolderAsync(
                    _wikiConfig.MarkdownFolderPath, 
                    _wikiConfig.OutputFolderPath).ConfigureAwait(false);

                if (success)
                {
                    _logger.LogInformation("Wiki refresh completed successfully via AJAX");
                    return new JsonResult(new { 
                        success = true, 
                        message = $"Wiki pages have been successfully generated! Processed {markdownFiles.Length} files." 
                    });
                }
                else
                {
                    _logger.LogWarning("Wiki refresh failed via AJAX");
                    return new JsonResult(new { 
                        success = false, 
                        message = "Failed to generate wiki pages. Check the application logs for details." 
                    });
                }
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "I/O error during wiki refresh via AJAX");
                return new JsonResult(new { 
                    success = false, 
                    message = $"I/O error occurred: {ex.Message}" 
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied during wiki refresh via AJAX");
                return new JsonResult(new { 
                    success = false, 
                    message = $"Access denied: {ex.Message}" 
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument during wiki refresh via AJAX");
                return new JsonResult(new { 
                    success = false, 
                    message = $"Configuration error: {ex.Message}" 
                });
            }
        }

        private void LoadGeneratedFiles()
        {
            _generatedFiles.Clear();
            
            if (Directory.Exists(_wikiConfig.OutputFolderPath))
            {
                var htmlFiles = Directory.GetFiles(_wikiConfig.OutputFolderPath, "*.html", SearchOption.AllDirectories);
                _generatedFiles.AddRange(htmlFiles
                    .Select(f => _wikiConfig.GetWebPath(f))
                    .OrderBy(f => f));
                
                HasGeneratedContent = _generatedFiles.Count > 0;
            }
        }
    }
}