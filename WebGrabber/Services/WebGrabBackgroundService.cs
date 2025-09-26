using Microsoft.Extensions.Hosting;

namespace WebGrabber.Services;

public class WebGrabBackgroundService : BackgroundService
{
    private readonly IBackgroundJobQueue _queue;
    private readonly IServiceProvider _services;
    private readonly ILogger<WebGrabBackgroundService> _logger;
    private readonly SseService _sse;

    public WebGrabBackgroundService(IBackgroundJobQueue queue, IServiceProvider services, ILogger<WebGrabBackgroundService> logger, SseService sse)
    {
        _queue = queue;
        _services = services;
        _logger = logger;
        _sse = sse;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WebGrabBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_queue.TryDequeue(out var config) && config is not null)
            {
                _logger.LogInformation("Dequeued job for {StartUrl}", config.StartUrl);
                try
                {
                    using var scope = _services.CreateScope();
                    var grabService = scope.ServiceProvider.GetRequiredService<IWebGrabService>();
                    var progress = new Progress<string>(m =>
                    {
                        _logger.LogInformation("[Grab] {Message}", m);
                        try { _sse.Broadcast(m); } catch { }
                    });
                    var result = await grabService.GrabSiteAsync(config, progress, stoppingToken);
                    _logger.LogInformation("Grab result: {Success} - {Message}", result.Success, result.Message);
                    try { _sse.Broadcast($"Completed: {result.Message}"); } catch { }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running background grab");
                    try { _sse.Broadcast($"Error: {ex.Message}"); } catch { }
                }
            }
            else
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation("WebGrabBackgroundService stopping");
    }
}
