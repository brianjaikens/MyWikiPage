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
    ctx.Response.Headers.Add("Content-Type", "text/event-stream");
    ctx.Response.Headers.Add("Cache-Control", "no-cache");

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

app.Run();