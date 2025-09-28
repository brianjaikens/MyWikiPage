using System.Collections.Generic;

namespace WebGrabber.Services;

public record WebGrabConfig
{
    public string StartUrl { get; init; } = string.Empty;
    public int MaxPages { get; init; } = 100;
    public string MarkdownFolder { get; init; } = "grabbed";
    public string BaseUrl { get; init; } = "/";
    public string UserAgent { get; init; } = "WebGrabberBot/1.0";
    public bool AllowExternalImages { get; init; } = false;
    public int CrawlLimit { get; init; } = 500;
    public bool DiscoverOnly { get; init; } = false;
}

public record WebGrabResult(bool Success, string Message, List<string> Log);

public interface IWebGrabService
{
    System.Threading.Tasks.Task<WebGrabResult> GrabSiteAsync(WebGrabConfig config, IProgress<string>? progress = null, System.Threading.CancellationToken cancellationToken = default);
}
