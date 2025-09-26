namespace WebGrabber.Services;

public class WebGrabSettings
{
    public string UserAgent { get; set; } = "WebGrabberBot/1.0";
    public bool AllowExternalImages { get; set; } = false;
    public string MarkdownFolder { get; set; } = "GrabbedPages";
    public string BaseUrl { get; set; } = "/";
    public int MaxPages { get; set; } = 20;
}
