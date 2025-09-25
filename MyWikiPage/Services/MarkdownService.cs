using Markdig;
using System.Text.RegularExpressions;

namespace MyWikiPage.Services
{
    public sealed class MarkdownService : IMarkdownService
    {
        private readonly MarkdownPipeline _pipeline;
        private readonly ILogger<MarkdownService> _logger;

        public MarkdownService(ILogger<MarkdownService> logger)
        {
            _logger = logger;
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();
        }

        public Task<string> ConvertMarkdownToHtmlAsync(string markdownContent, string baseDirectory)
        {
            try
            {
                var html = Markdown.ToHtml(markdownContent, _pipeline);
                return Task.FromResult(html);
            }
            catch (ArgumentException ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex, "Invalid argument when converting markdown to HTML");
                }
                return Task.FromResult($"<p>Error processing markdown: {ex.Message}</p>");
            }
            catch (InvalidOperationException ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex, "Invalid operation when converting markdown to HTML");
                }
                return Task.FromResult($"<p>Error processing markdown: {ex.Message}</p>");
            }
        }

        public Task<List<string>> GetMarkdownFilesAsync(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return Task.FromResult(new List<string>());

            var files = Directory.GetFiles(folderPath, "*.md", SearchOption.AllDirectories).ToList();
            return Task.FromResult(files);
        }

        public async Task<bool> GenerateHtmlFromMarkdownFolderAsync(string markdownFolderPath, string outputFolderPath)
        {
            try
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Starting wiki generation from '{MarkdownPath}' to '{OutputPath}'", markdownFolderPath, outputFolderPath);
                }
                
                if (!Directory.Exists(markdownFolderPath))
                {
                    if (_logger.IsEnabled(LogLevel.Warning))
                    {
                        _logger.LogWarning("Markdown folder does not exist: {MarkdownPath}", markdownFolderPath);
                    }
                    return false;
                }

                // Create output directory if it doesn't exist
                Directory.CreateDirectory(outputFolderPath);

                var markdownFiles = await GetMarkdownFilesAsync(markdownFolderPath).ConfigureAwait(false);
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Found {FileCount} markdown files to process", markdownFiles.Count);
                }
                
                foreach (var markdownFile in markdownFiles)
                {
                    var markdownContent = await File.ReadAllTextAsync(markdownFile).ConfigureAwait(false);
                    var html = await ConvertMarkdownToHtmlAsync(markdownContent, markdownFolderPath).ConfigureAwait(false);
                    
                    // Process internal links to point to generated HTML files
                    html = ProcessInternalLinks(html, markdownFolderPath, outputFolderPath);
                    
                    // Generate the full HTML document
                    var fullHtml = GenerateHtmlDocument(html, Path.GetFileNameWithoutExtension(markdownFile));
                    
                    // Determine output file path
                    var relativePath = Path.GetRelativePath(markdownFolderPath, markdownFile);
                    var outputFile = Path.Combine(outputFolderPath, Path.ChangeExtension(relativePath, ".html"));
                    
                    // Create directory for the output file if needed
                    var outputDir = Path.GetDirectoryName(outputFile);
                    if (!string.IsNullOrEmpty(outputDir))
                        Directory.CreateDirectory(outputDir);
                    
                    await File.WriteAllTextAsync(outputFile, fullHtml).ConfigureAwait(false);
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Generated HTML file: {OutputFile}", outputFile);
                    }
                }

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Wiki generation completed successfully. Generated {FileCount} pages", markdownFiles.Count);
                }
                return true;
            }
            catch (IOException ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex, "I/O error generating HTML from markdown folder");
                }
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex, "Access denied when generating HTML from markdown folder");
                }
                return false;
            }
            catch (ArgumentException ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex, "Invalid argument when generating HTML from markdown folder");
                }
                return false;
            }
        }

        public string ProcessInternalLinks(string html, string baseDirectory, string outputDirectory)
        {
            // Replace markdown links (.md) with HTML links (.html)
            var linkPattern = @"href=""([^""]+\.md)""";
            html = Regex.Replace(html, linkPattern, match =>
            {
                var mdLink = match.Groups[1].Value;
                var htmlLink = Path.ChangeExtension(mdLink, ".html");
                return $"href=\"{htmlLink}\"";
            });

            return html;
        }

        private static string GenerateHtmlDocument(string bodyContent, string title)
        {
            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{title}</title>
    <link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.0/font/bootstrap-icons.css"">
    <style>
        :root {{
            --bs-body-bg: #ffffff;
            --bs-body-color: #212529;
            --bs-secondary-bg: #f8f9fa;
            --bs-tertiary-bg: #ffffff;
            --bs-border-color: #dee2e6;
            --bs-card-bg: #ffffff;
            --bs-card-border-color: #dee2e6;
        }}

        [data-theme=""dark""] {{
            --bs-body-bg: #121212;
            --bs-body-color: #e9ecef;
            --bs-secondary-bg: #1e1e1e;
            --bs-tertiary-bg: #2d2d2d;
            --bs-border-color: #495057;
            --bs-card-bg: #1e1e1e;
            --bs-card-border-color: #495057;
        }}

        body {{
            font-family: Arial, sans-serif;
            max-width: 800px;
            margin: 20px auto;
            padding: 20px;
            line-height: 1.6;
            color: var(--bs-body-color);
            background-color: var(--bs-body-bg);
            transition: background-color 0.3s ease, color 0.3s ease;
        }}

        h1, h2, h3, h4, h5, h6 {{
            color: var(--bs-body-color);
            margin-top: 1.5em;
            font-weight: 600;
        }}

        h1 {{ font-size: 2rem; }}
        h2 {{ font-size: 1.5rem; }}
        h3 {{ font-size: 1.25rem; }}
        h4 {{ font-size: 1.1rem; }}
        h5 {{ font-size: 1rem; }}
        h6 {{ font-size: 0.9rem; }}

        p {{
            margin-bottom: 1rem;
        }}

        pre {{
            background-color: var(--bs-secondary-bg);
            color: var(--bs-body-color);
            padding: 1rem;
            border-radius: 0.375rem;
            overflow-x: auto;
            border: 1px solid var(--bs-border-color);
            margin: 1rem 0;
        }}

        code {{
            background-color: var(--bs-secondary-bg);
            color: var(--bs-body-color);
            padding: 0.25rem 0.5rem;
            border-radius: 0.25rem;
            font-size: 0.875em;
        }}

        pre code {{
            background-color: transparent;
            padding: 0;
            border-radius: 0;
        }}

        blockquote {{
            border-left: 4px solid var(--bs-border-color);
            margin: 1rem 0;
            padding-left: 1rem;
            color: var(--bs-body-color);
            opacity: 0.8;
        }}

        table {{
            border-collapse: collapse;
            width: 100%;
            margin: 1rem 0;
            border: 1px solid var(--bs-border-color);
        }}

        th, td {{
            border: 1px solid var(--bs-border-color);
            padding: 0.75rem;
            text-align: left;
        }}

        th {{
            background-color: var(--bs-secondary-bg);
            font-weight: 600;
        }}

        ul, ol {{
            margin: 1rem 0;
            padding-left: 2rem;
        }}

        li {{
            margin-bottom: 0.5rem;
        }}

        a {{
            color: #0d6efd;
            text-decoration: none;
        }}

        a:hover {{
            color: #0a58ca;
            text-decoration: underline;
        }}

        [data-theme=""dark""] a {{
            color: #86b7fe;
        }}

        [data-theme=""dark""] a:hover {{
            color: #a6d2ff;
        }}

        /* Simple content styling for standalone view */
        .content {{
            padding: 1rem 0;
        }}

/* Responsive adjustments */
@media (max-width: 768px) {{
    body {{
        margin: 10px auto;
        padding: 15px;
        max-width: 95%;
    }}
}}
    </style>
</head>
<body>
    <div class=""content"">
        {bodyContent}
    </div>
</body>
</html>";
        }
    }
}