using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyWikiPage.Services;
using System.Text.RegularExpressions;
using System.IO;
using System.Globalization;

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
        public string? RequestedPage { get; set; }
        public bool IsSpecificPageRequested => !string.IsNullOrEmpty(RequestedPage);
        public bool ContentLoadedSuccessfully { get; private set; }

        public void OnGet()
        {
            // Explicitly read the wikipage parameter from query string to avoid route confusion
            var wikipageParam = HttpContext.Request.Query["wikipage"].FirstOrDefault();
            
            // Console output for immediate debugging
#pragma warning disable CA1303 // Do not pass literals as localized parameters - Debug output only
            Console.WriteLine($"?? === OnGet ENTRY === Query string wikipage parameter: '{wikipageParam ?? "null"}'");
            Console.WriteLine($"Request URL: {HttpContext.Request.Path}{HttpContext.Request.QueryString}");
            Console.WriteLine($"Request Method: {HttpContext.Request.Method}");
            Console.WriteLine($"All Query Parameters: {string.Join(", ", HttpContext.Request.Query.Select(q => $"{q.Key}={q.Value}"))}");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            
            _logger.LogInformation("?? === OnGet ENTRY === Query string wikipage parameter: '{WikiPage}'", wikipageParam ?? "null");
            _logger.LogInformation("Request URL: {Url}", HttpContext.Request.Path + HttpContext.Request.QueryString);
            _logger.LogInformation("Request Method: {Method}", HttpContext.Request.Method);
            
            RequestedPage = wikipageParam;
            MarkdownFolderPath = _wikiConfig.MarkdownFolderPath;
            
#pragma warning disable CA1303 // Do not pass literals as localized parameters - Debug output only
            Console.WriteLine($"Set RequestedPage to: '{RequestedPage ?? "null"}'");
            Console.WriteLine($"MarkdownFolderPath: '{MarkdownFolderPath}'");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            
            _logger.LogInformation("Set RequestedPage to: '{RequestedPage}'", RequestedPage ?? "null");
            _logger.LogInformation("MarkdownFolderPath: '{MarkdownFolderPath}'", MarkdownFolderPath);
            
            CheckForGeneratedContent();
            
#pragma warning disable CA1303 // Do not pass literals as localized parameters - Debug output only
            Console.WriteLine($"HasGeneratedContent: {HasGeneratedContent}");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            _logger.LogInformation("HasGeneratedContent: {HasGeneratedContent}", HasGeneratedContent);
            
            if (HasGeneratedContent)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters - Debug output only
                Console.WriteLine("Calling LoadWikiContent...");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                _logger.LogInformation("Calling LoadWikiContent...");
                LoadWikiContent();
#pragma warning disable CA1303 // Do not pass literals as localized parameters - Debug output only
                Console.WriteLine($"LoadWikiContent completed. ContentLoadedSuccessfully: {ContentLoadedSuccessfully}");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                _logger.LogInformation("LoadWikiContent completed. ContentLoadedSuccessfully: {ContentLoadedSuccessfully}", ContentLoadedSuccessfully);
            }
            else
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters - Debug output only
                Console.WriteLine("No generated content available, using fallback");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                _logger.LogWarning("No generated content available, using fallback");
            }
            
#pragma warning disable CA1303 // Do not pass literals as localized parameters - Debug output only
            Console.WriteLine($"?? === OnGet EXIT === Final PageTitle: '{PageTitle}', Content Length: {WikiContent?.Length ?? 0}");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            _logger.LogInformation("?? === OnGet EXIT === Final PageTitle: '{PageTitle}', Content Length: {ContentLength}", 
                PageTitle, WikiContent?.Length ?? 0);
        }

        // AJAX endpoint for loading wiki content
        public IActionResult OnGetContent()
        {
            try
            {
                // Read wikipage parameter from query string for AJAX requests
                var wikipageParam = HttpContext.Request.Query["wikipage"].FirstOrDefault();
                
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("OnGetContent called with wikipage: '{WikiPage}'", wikipageParam ?? "null");
                }
                
                RequestedPage = wikipageParam;
                CheckForGeneratedContent();
                
                if (HasGeneratedContent)
                {
                    LoadWikiContent();
                    
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("Returning content for wikipage: '{WikiPage}', title: '{Title}', content length: {Length}", 
                            wikipageParam ?? "null", PageTitle, WikiContent?.Length ?? 0);
                    }
                    
                    return new JsonResult(new 
                    { 
                        success = true, 
                        content = WikiContent, 
                        title = PageTitle,
                        hasContent = HasGeneratedContent,
                        requestedPage = wikipageParam,
                        timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC", CultureInfo.InvariantCulture)
                    });
                }
                
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning("No generated content available for wikipage: '{WikiPage}'", wikipageParam ?? "null");
                }
                
                return new JsonResult(new 
                { 
                    success = false, 
                    content = GenerateFallbackContent(),
                    title = "Welcome to MyWikiPage",
                    hasContent = false,
                    message = "No generated content available",
                    requestedPage = wikipageParam,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC", CultureInfo.InvariantCulture)
                });
            }
            catch (IOException ex)
            {
                var wikipageParam = HttpContext.Request.Query["wikipage"].FirstOrDefault();
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex, "I/O error in OnGetContent for wikipage: '{WikiPage}'", wikipageParam ?? "null");
                }
                
                return new JsonResult(new 
                { 
                    success = false, 
                    content = "<div class=\"alert alert-danger\">An I/O error occurred while loading the content.</div>",
                    title = "Error",
                    hasContent = false,
                    message = $"I/O Error: {ex.Message}",
                    requestedPage = wikipageParam,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC", CultureInfo.InvariantCulture)
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                var wikipageParam = HttpContext.Request.Query["wikipage"].FirstOrDefault();
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex, "Access denied in OnGetContent for wikipage: '{WikiPage}'", wikipageParam ?? "null");
                }
                
                return new JsonResult(new 
                { 
                    success = false, 
                    content = "<div class=\"alert alert-danger\">Access denied while loading the content.</div>",
                    title = "Error",
                    hasContent = false,
                    message = $"Access Error: {ex.Message}",
                    requestedPage = wikipageParam,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC", CultureInfo.InvariantCulture)
                });
            }
        }

        // Diagnostic endpoint to check wiki status
        public IActionResult OnGetDiagnostics()
        {
            try
            {
                var outputPath = _wikiConfig.OutputFolderPath;
                var markdownPath = _wikiConfig.MarkdownFolderPath;
                
                var diagnostics = new
                {
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC", CultureInfo.InvariantCulture),
                    markdownFolderPath = markdownPath,
                    markdownFolderExists = Directory.Exists(markdownPath),
                    outputFolderPath = outputPath,
                    outputFolderExists = Directory.Exists(outputPath),
                    markdownFiles = Directory.Exists(markdownPath) 
                        ? Directory.GetFiles(markdownPath, "*.md", SearchOption.AllDirectories)
                            .Select(f => Path.GetRelativePath(markdownPath, f)).ToArray()
                        : Array.Empty<string>(),
                    htmlFiles = Directory.Exists(outputPath)
                        ? Directory.GetFiles(outputPath, "*.html", SearchOption.AllDirectories)
                            .Select(f => Path.GetRelativePath(outputPath, f)).ToArray()
                        : Array.Empty<string>(),
                    defaultPageUrl = _wikiConfig.GetDefaultPage(),
                    hasGeneratedContent = HasGeneratedContent
                };
                
                return new JsonResult(diagnostics);
            }
            catch (IOException ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex, "I/O error in diagnostics");
                }
                return new JsonResult(new { error = $"I/O Error: {ex.Message}", timestamp = DateTime.UtcNow });
            }
            catch (UnauthorizedAccessException ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex, "Access error in diagnostics");
                }
                return new JsonResult(new { error = $"Access Error: {ex.Message}", timestamp = DateTime.UtcNow });
            }
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

        private void LoadWikiContent()
        {
            try
            {
                string? targetFilePath = null;

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("LoadWikiContent called. RequestedPage: '{RequestedPage}', DefaultPageUrl: '{DefaultPageUrl}', OutputFolderPath: '{OutputFolderPath}'", 
                        RequestedPage ?? "null", DefaultPageUrl ?? "null", _wikiConfig.OutputFolderPath);
                }

                // If a specific page is requested, try to load it
                if (!string.IsNullOrEmpty(RequestedPage))
                {
                    var requestedFile = Path.Combine(_wikiConfig.OutputFolderPath, RequestedPage.TrimStart('/'));
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("Checking requested file: '{RequestedFile}', exists: {Exists}", 
                            requestedFile, System.IO.File.Exists(requestedFile));
                    }
                    
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
                    
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("Using default page file: '{TargetFilePath}', exists: {Exists}", 
                            targetFilePath ?? "null", targetFilePath != null && System.IO.File.Exists(targetFilePath));
                    }
                }

                if (targetFilePath != null && System.IO.File.Exists(targetFilePath))
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("Loading content from file: '{TargetFilePath}'", targetFilePath);
                    }
                    
                    var htmlContent = System.IO.File.ReadAllText(targetFilePath);
                    
                    // Extract content between body tags
                    var bodyMatch = Regex.Match(htmlContent, @"<body[^>]*>(.*?)</body>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    if (bodyMatch.Success)
                    {
                        WikiContent = bodyMatch.Groups[1].Value;
                        
                        // Remove the theme toggle button and nav from generated content
                        WikiContent = Regex.Replace(WikiContent, @"<button[^>]*class=""theme-toggle""[^>]*>.*?</button>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                        WikiContent = Regex.Replace(WikiContent, @"<div class=""nav""[^>]*>.*?</div>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                        WikiContent = Regex.Replace(WikiContent, @"<script[^>]*>.*?</script>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                        
                        // Process links for AJAX navigation
                        WikiContent = ProcessLinksForAjaxNavigation(WikiContent);
                        
                        // Extract title from content or use filename
                        var titleMatch = Regex.Match(WikiContent, @"<h1[^>]*>(.*?)</h1>", RegexOptions.IgnoreCase);
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
                        
                        ContentLoadedSuccessfully = true;
                        
                        if (_logger.IsEnabled(LogLevel.Information))
                        {
                            _logger.LogInformation("Successfully loaded content. Title: '{PageTitle}', Content length: {ContentLength}", 
                                PageTitle, WikiContent?.Length ?? 0);
                        }
                    }
                    else
                    {
                        if (_logger.IsEnabled(LogLevel.Warning))
                        {
                            _logger.LogWarning("Could not extract body content from file: '{TargetFilePath}'", targetFilePath);
                        }
                        WikiContent = GenerateFallbackContent();
                        HasGeneratedContent = false;
                        ContentLoadedSuccessfully = false;
                    }
                }
                else
                {
                    if (_logger.IsEnabled(LogLevel.Warning))
                    {
                        _logger.LogWarning("Target file not found or is null. TargetFilePath: '{TargetFilePath}'", targetFilePath ?? "null");
                    }
                    WikiContent = GenerateFallbackContent();
                    HasGeneratedContent = false;
                    ContentLoadedSuccessfully = false;
                }
            }
            catch (IOException ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex, "I/O error loading wiki content");
                }
                WikiContent = GenerateFallbackContent();
                HasGeneratedContent = false;
            }
            catch (UnauthorizedAccessException ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex, "Access denied loading wiki content");
                }
                WikiContent = GenerateFallbackContent();
                HasGeneratedContent = false;
            }
        }

        private static string ProcessLinksForAjaxNavigation(string content)
        {
            // Convert internal HTML links to use AJAX navigation with wikipage parameter
            return Regex.Replace(content, @"href\s*=\s*[""']([^""']*\.html)[""']", match =>
            {
                var href = match.Groups[1].Value;
                
                // Skip external links (http/https) and absolute paths starting with /
                if (href.StartsWith("http", StringComparison.OrdinalIgnoreCase) || href.StartsWith('/'))
                {
                    return match.Value;
                }
                
                // Convert to both query string navigation AND AJAX data attribute using wikipage parameter
                var queryStringHref = $"/?wikipage={Uri.EscapeDataString(href)}";
                return $"href=\"{queryStringHref}\" data-wiki-page=\"{Uri.EscapeDataString(href)}\"";
            }, RegexOptions.IgnoreCase);
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
