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
    <link rel=""stylesheet"" href=""/css/site.css"">
    <link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.0/font/bootstrap-icons.css"">
</head>
<body class=""generated-wiki-page"">
    <div class=""nav"">
        <a href=""index.html"">Home</a>
        <a href=""contents.html"">Contents</a>
        <a href=""/wiki"">Back to Wiki</a>
    </div>
    {bodyContent}
</body>
</html>";
        }
    }
}