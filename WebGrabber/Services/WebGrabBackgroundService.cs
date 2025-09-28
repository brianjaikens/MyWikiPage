using Microsoft.Extensions.Hosting;

namespace WebGrabber.Services;

public class WebGrabBackgroundService : BackgroundService
{
    private readonly IBackgroundJobQueue _queue;
    private readonly IServiceProvider _services;
    private readonly ILogger<WebGrabBackgroundService> _logger;
    private readonly SseService _sse;
    private readonly JobStateService _jobState;

    public WebGrabBackgroundService(IBackgroundJobQueue queue, IServiceProvider services, ILogger<WebGrabBackgroundService> logger, SseService sse, JobStateService jobState)
    {
        _queue = queue;
        _services = services;
        _logger = logger;
        _sse = sse;
        _jobState = jobState;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WebGrabBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_queue.TryDequeue(out var config) && config is not null)
            {
                if (!_jobState.TryBeginJob())
                {
                    _logger.LogWarning("Job skipped because another job is running");
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

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

                    // If this was a background discovery-only job, attempt to parse and persist pages found
                    if (config.DiscoverOnly)
                    {
                        var last = result.Log?.LastOrDefault(l => l.Contains("Pages found:"));
                        if (last != null)
                        {
                            var parts = last.Split(':');
                            if (int.TryParse(parts.Last().Trim(), out var v))
                            {
                                _jobState.SetLastDiscovery(v, config.StartUrl);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running background grab");
                    try { _sse.Broadcast($"Error: {ex.Message}"); } catch { }
                }
                finally
                {
                    _jobState.EndJob();
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
