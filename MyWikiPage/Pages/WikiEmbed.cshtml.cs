using Microsoft.AspNetCore.Mvc.RazorPages;
using MyWikiPage.Services;
using System.Text.RegularExpressions;

namespace MyWikiPage.Pages
{
    public sealed class WikiEmbedModel : PageModel
    {
        private readonly IWikiConfigService _wikiConfig;
        private readonly ILogger<WikiEmbedModel> _logger;

        public WikiEmbedModel(IWikiConfigService wikiConfig, ILogger<WikiEmbedModel> logger)
        {
            _wikiConfig = wikiConfig;
            _logger = logger;
        }

        public new string Content { get; set; } = string.Empty;
        public string PageTitle { get; set; } = "Wiki";
        public bool HasContent { get; set; }
        public string? DefaultPageUrl { get; set; }
        public string Theme { get; set; } = "light";
        public string? RequestedPage { get; set; }

        public void OnGet(string? theme = null, string? page = null)
        {
            Theme = theme ?? "light";
            RequestedPage = page;
            LoadWikiContent();
        }

        private void LoadWikiContent()
        {
            try
            {
                DefaultPageUrl = _wikiConfig.GetDefaultPage();
                string? targetFilePath = null;

                // If a specific page is requested, try to load it
                if (!string.IsNullOrEmpty(RequestedPage))
                {
                    var requestedFile = Path.Combine(_wikiConfig.OutputFolderPath, RequestedPage.TrimStart('/'));
                    if (System.IO.File.Exists(requestedFile))
                    {
                        targetFilePath = requestedFile;
                    }
                }
                
                // If no specific page or page not found, load default page
                if (targetFilePath == null && !string.IsNullOrEmpty(DefaultPageUrl))
                {
                    // Convert web path back to file path
                    var webPath = DefaultPageUrl.TrimStart('/');
                    if (webPath.StartsWith("wiki/", StringComparison.OrdinalIgnoreCase))
                    {
                        targetFilePath = Path.Combine(_wikiConfig.OutputFolderPath, 
                            webPath.Substring("wiki/".Length));
                    }
                    else
                    {
                        // If the path doesn't start with "wiki/", use it as-is
                        targetFilePath = Path.Combine(_wikiConfig.OutputFolderPath, webPath);
                    }
                }

                if (targetFilePath != null && System.IO.File.Exists(targetFilePath))
                {
                    var htmlContent = System.IO.File.ReadAllText(targetFilePath);
                    
                    // Extract content between body tags
                    var bodyMatch = Regex.Match(htmlContent, @"<body[^>]*>(.*?)</body>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    if (bodyMatch.Success)
                    {
                        Content = bodyMatch.Groups[1].Value;
                        
                        // Remove any existing navigation divs from the generated content
                        Content = Regex.Replace(Content, @"<div class=""nav""[^>]*>.*?</div>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                        
                        // Simple server-side link rewriting for iframe use
                        Content = RewriteLinksForIframe(Content);
                        
                        // Extract title from content or use filename
                        var titleMatch = Regex.Match(Content, @"<h1[^>]*>(.*?)</h1>", RegexOptions.IgnoreCase);
                        if (titleMatch.Success)
                        {
                            PageTitle = Regex.Replace(titleMatch.Groups[1].Value, @"<[^>]*>", "").Trim();
                        }
                        else
                        {
                            PageTitle = Path.GetFileNameWithoutExtension(targetFilePath)
                                .Replace("-", " ", StringComparison.Ordinal)
                                .Replace("_", " ", StringComparison.Ordinal);
                        }
                        
                        HasContent = !string.IsNullOrWhiteSpace(Content);
                    }
                }
            }
            catch (IOException ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex, "I/O error loading wiki content for embed view");
                }
                HasContent = false;
            }
            catch (UnauthorizedAccessException ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex, "Access denied loading wiki content for embed view");
                }
                HasContent = false;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex, "Invalid path format when loading wiki content for embed view");
                }
                HasContent = false;
            }
        }

        private string RewriteLinksForIframe(string content)
        {
            // Simple approach: rewrite all .html links to use WikiEmbed
            return Regex.Replace(content, @"href\s*=\s*[""']([^""']*\.html)[""']", match =>
            {
                var href = match.Groups[1].Value;
                
                // Skip external links (http/https) and absolute paths starting with /
                if (href.StartsWith("http", StringComparison.OrdinalIgnoreCase) || href.StartsWith('/'))
                {
                    return match.Value;
                }
                
                // Convert to WikiEmbed link
                var newHref = $"/wiki-embed?theme={Theme}&page={Uri.EscapeDataString(href)}";
                return $"href=\"{newHref}\"";
            }, RegexOptions.IgnoreCase);
        }
    }
}