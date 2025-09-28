using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebGrabber.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHttpClient();

// config persistence
builder.Services.Configure<WebGrabSettings>(builder.Configuration.GetSection("WebGrab"));

// background job queue and hosted service
builder.Services.AddSingleton<IBackgroundJobQueue, BackgroundJobQueue>();
builder.Services.AddSingleton<SseService>();
builder.Services.AddHostedService<WebGrabBackgroundService>();

builder.Services.AddScoped<IWebGrabService, WebGrabService>();
// job state for concurrency control and last discovery persistence
builder.Services.AddSingleton<JobStateService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();

// SSE endpoint
app.MapGet("/sse/logs", async (SseService sse, HttpContext ctx) =>
{
    ctx.Response.Headers.Append("Content-Type", "text/event-stream");
    ctx.Response.Headers.Append("Cache-Control", "no-cache");

    var channel = sse.Subscribe();
    try
    {
        await using var cancellation = ctx.RequestAborted.Register(() => sse.Unsubscribe(channel));

        await foreach (var msg in channel.Reader.ReadAllAsync(ctx.RequestAborted))
        {
            var data = msg.Replace("\n", "\\n");
            await ctx.Response.WriteAsync($"data: {data}\n\n");
            await ctx.Response.Body.FlushAsync(ctx.RequestAborted);
        }
    }
    catch (OperationCanceledException) { }

    return Results.Ok();
});

// Discover endpoint: runs discovery-only job synchronously up to a short timeout and returns pages found count
app.MapPost("/discover", async (HttpContext ctx, IBackgroundJobQueue queue, IServiceProvider services, SseService sse, JobStateService jobState, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("/discover");

    // Prevent concurrent jobs
    if (!jobState.TryBeginJob())
    {
        logger.LogInformation("Rejecting discover request because a job is already running");
        return Results.Json(new { success = false, message = "Another job is already running" });
    }

    try
    {
        // read form
        var form = await ctx.Request.ReadFormAsync();

        var startUrl = form["StartUrl"].FirstOrDefault() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(startUrl) || !Uri.IsWellFormedUriString(startUrl, UriKind.Absolute))
        {
            logger.LogWarning("Invalid StartUrl received: '{StartUrl}'", startUrl);
            return Results.Json(new { success = false, message = "Invalid or missing StartUrl" });
        }

        var maxPages = 0;
        if (!string.IsNullOrWhiteSpace(form["MaxPages"].FirstOrDefault()))
        {
            if (!int.TryParse(form["MaxPages"].FirstOrDefault(), out maxPages))
            {
                logger.LogWarning("Invalid MaxPages value: '{Value}'", form["MaxPages"].FirstOrDefault());
                return Results.Json(new { success = false, message = "Invalid MaxPages value" });
            }
        }

        var crawlLimit = 500;
        var crawlVal = form["CrawlLimit"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(crawlVal))
        {
            if (!int.TryParse(crawlVal, out crawlLimit))
            {
                logger.LogWarning("Invalid CrawlLimit value: '{Value}'", crawlVal);
                return Results.Json(new { success = false, message = "Invalid CrawlLimit value" });
            }
        }

        var config = new WebGrabConfig
        {
            StartUrl = startUrl,
            MaxPages = maxPages,
            MarkdownFolder = form["MarkdownFolder"].FirstOrDefault() ?? string.Empty,
            BaseUrl = form["BaseUrl"].FirstOrDefault() ?? string.Empty,
            UserAgent = form["UserAgent"].FirstOrDefault() ?? string.Empty,
            AllowExternalImages = string.Equals(form["AllowExternalImages"].FirstOrDefault(), "true", StringComparison.OrdinalIgnoreCase),
            CrawlLimit = crawlLimit,
            DiscoverOnly = true
        };

        logger.LogInformation("Starting discovery attempt for {StartUrl} (MaxPages={MaxPages}, CrawlLimit={CrawlLimit})", config.StartUrl, config.MaxPages, config.CrawlLimit);

        // Try to run discovery quickly in a scoped service with a short timeout so the UI gets immediate feedback.
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var scope = services.CreateScope();
            var grabService = scope.ServiceProvider.GetRequiredService<IWebGrabService>();
            var pagesFound = 0;
            var logs = new List<string>();
            var progress = new Progress<string>(m =>
            {
                logs.Add(m);
                try { sse.Broadcast(m); } catch (Exception ex) { logger.LogDebug(ex, "SSE broadcast failed"); }
                if (m != null && m.Contains("Pages found:"))
                {
                    var parts = m.Split(':');
                    if (int.TryParse(parts.Last().Trim(), out var v)) pagesFound = v;
                }
            });

            var task = grabService.GrabSiteAsync(config, progress, cts.Token);

            // Log any unobserved fault on the task and await the continuation to avoid CS4014
            var continuation = task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    logger.LogError(t.Exception, "Discovery task faulted");
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
            await continuation;

            var completed = await Task.WhenAny(task, Task.Delay(14000, cts.Token));
            if (completed == task)
            {
                try
                {
                    var res = await task; // exceptions from task will be caught below

                    // try to parse pages found from logs if available
                    var last = logs.LastOrDefault(l => l.Contains("Pages found:"));
                    if (last != null)
                    {
                        var parts = last.Split(':');
                        if (int.TryParse(parts.Last().Trim(), out var v)) pagesFound = v;
                    }

                    // persist discovery result
                    try
                    {
                        jobState.SetLastDiscovery(pagesFound, config.StartUrl);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to persist last discovery");
                    }

                    logger.LogInformation("Discovery completed synchronously. PagesFound={PagesFound}", pagesFound);

                    return Results.Json(new { success = true, pagesFound, message = res.Message });
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Discovery task was cancelled while awaiting result");
                    return Results.Json(new { success = false, message = "Discovery cancelled" });
                }
                catch (Exception ex)
                {
                    // Log and return the error plus any gathered logs to help debugging
                    logger.LogError(ex, "Discovery task threw an exception while awaiting result");
                    var errMsg = ex.Message ?? "Discovery failed with exception";
                    var full = ex.ToString();
                    return Results.Json(new { success = false, message = errMsg, details = full, logs });
                }
            }
            else
            {
                // timed out; enqueue a discover job instead so background worker will complete and broadcast
                logger.LogInformation("Discovery did not complete within timeout; enqueuing background job");
                queue.Enqueue(config);
                return Results.Json(new { success = true, pagesFound = (int?)null, message = "Discovery started in background" });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Discovery run failed before task execution");
            return Results.Json(new { success = false, message = ex.Message, details = ex.ToString() });
        }
    }
    catch (Exception ex)
    {
        // Generic form/read error
        var msg = ex.Message ?? "Unknown error reading request";
        logger.LogError(ex, "Failed to handle /discover request");
        return Results.Json(new { success = false, message = msg, details = ex.ToString() });
    }
    finally
    {
        jobState.EndJob();
    }
});

app.Run();