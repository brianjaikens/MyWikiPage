using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using WebGrabber.Services;
using Microsoft.AspNetCore.Hosting;

namespace WebGrabber.Pages;

public class IndexModel : PageModel
{
    private readonly IWebGrabService _grabService;
    private readonly IBackgroundJobQueue _queue;
    private readonly WebGrabSettings _settings;
    private readonly IWebHostEnvironment _env;

    public IndexModel(IWebGrabService grabService, IBackgroundJobQueue queue, IOptions<WebGrabSettings> settings, IWebHostEnvironment env)
    {
        _grabService = grabService;
        _queue = queue;
        _settings = settings.Value;
        _env = env;
    }

    [BindProperty]
    public string StartUrl { get; set; } = string.Empty;
    [BindProperty]
    public int MaxPages { get; set; }
    [BindProperty]
    public string MarkdownFolder { get; set; } = string.Empty;
    [BindProperty]
    public string BaseUrl { get; set; } = string.Empty;
    [BindProperty]
    public string UserAgent { get; set; } = string.Empty;
    [BindProperty]
    public bool AllowExternalImages { get; set; }

    // Presentation properties for the UI (safe defaults)
    public string LastRun { get; set; } = "Never";
    public string InitialLog { get; set; } = string.Empty;
    public int QueuedCount { get; set; } = 0;
    public int RunningCount { get; set; } = 0;
    public int CompletedCount { get; set; } = 0;

    public void OnGet()
    {
        // load persisted defaults
        MaxPages = _settings.MaxPages;
        MarkdownFolder = _settings.MarkdownFolder;
        BaseUrl = _settings.BaseUrl;
        UserAgent = _settings.UserAgent;
        AllowExternalImages = _settings.AllowExternalImages;

        // Optionally populate presentation properties from any background queue/service in future
    }

    public IActionResult OnPost()
    {
        var config = new WebGrabConfig
        {
            StartUrl = StartUrl,
            MaxPages = MaxPages > 0 ? MaxPages : _settings.MaxPages,
            MarkdownFolder = string.IsNullOrWhiteSpace(MarkdownFolder) ? _settings.MarkdownFolder : MarkdownFolder,
            BaseUrl = string.IsNullOrWhiteSpace(BaseUrl) ? _settings.BaseUrl : BaseUrl,
            UserAgent = string.IsNullOrWhiteSpace(UserAgent) ? _settings.UserAgent : UserAgent,
            AllowExternalImages = AllowExternalImages
        };

        // Enqueue background job
        _queue.Enqueue(config);

        // Compute absolute folder path for feedback
        string folderPath;
        try
        {
            folderPath = Path.IsPathRooted(config.MarkdownFolder)
                ? Path.GetFullPath(config.MarkdownFolder)
                : Path.GetFullPath(Path.Combine(_env.ContentRootPath, config.MarkdownFolder));
        }
        catch
        {
            folderPath = config.MarkdownFolder;
        }

        return new JsonResult(new { success = true, message = "Job enqueued", folder = folderPath });
    }
}
