using Microsoft.Extensions.FileProviders;
using System.Globalization;

namespace MyWikiPage.Services
{
    public sealed class WikiConfigService : IWikiConfigService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WikiConfigService> _logger;

        public WikiConfigService(IWebHostEnvironment environment, IConfiguration configuration, ILogger<WikiConfigService> logger)
        {
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
        }

        public string MarkdownFolderPath
        {
            get
            {
                var configPath = _configuration["Wiki:MarkdownFolder"];
                
                if (string.IsNullOrEmpty(configPath))
                {
                    var defaultPath = Path.Combine(_environment.ContentRootPath, "markdown");
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Using default markdown path: {Path}", defaultPath);
                    }
                    return defaultPath;
                }
                
                // If the path is absolute, use it as-is, otherwise make it relative to ContentRootPath
                var resolvedPath = Path.IsPathRooted(configPath) 
                    ? configPath 
                    : Path.Combine(_environment.ContentRootPath, configPath);
                    
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Resolved markdown path: {Path}", resolvedPath);
                }
                
                return resolvedPath;
            }
        }

        public string OutputFolderPath
        {
            get
            {
                var configPath = _configuration["Wiki:OutputFolder"];
                
                if (string.IsNullOrEmpty(configPath))
                {
                    var defaultPath = Path.Combine(_environment.WebRootPath, "wiki");
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Using default output path: {Path}", defaultPath);
                    }
                    return defaultPath;
                }
                
                // If the path is absolute, use it as-is, otherwise make it relative to ContentRootPath
                var resolvedPath = Path.IsPathRooted(configPath) 
                    ? configPath 
                    : Path.Combine(_environment.ContentRootPath, configPath);
                    
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Resolved output path: {Path}", resolvedPath);
                }
                
                return resolvedPath;
            }
        }

        public string GetWebPath(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            
            if (filePath.StartsWith(OutputFolderPath, StringComparison.OrdinalIgnoreCase))
            {
                var relativePath = Path.GetRelativePath(OutputFolderPath, filePath);
                return "/wiki/" + relativePath.Replace('\\', '/');
            }
            return filePath;
        }

        public string GetDefaultPage()
        {
            var indexPath = Path.Combine(OutputFolderPath, "index.html");
            var contentsPath = Path.Combine(OutputFolderPath, "contents.html");

            if (File.Exists(indexPath))
                return GetWebPath(indexPath);
            if (File.Exists(contentsPath))
                return GetWebPath(contentsPath);

            return "/wiki";
        }
    }
}