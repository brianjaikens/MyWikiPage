using MyWikiPage.Services;
using NLog;
using NLog.Web;

// Early init of NLog to allow startup and exception logging, before host is built
var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // NLog: Setup NLog for Dependency injection
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Add services to the container.
    builder.Services.AddRazorPages();

    // Register wiki services
    builder.Services.AddScoped<IMarkdownService, MarkdownService>();
    builder.Services.AddScoped<IWikiConfigService, WikiConfigService>();

    var app = builder.Build();

    // Ensure the wiki output directory exists before setting up static file provider
    var wikiOutputPath = Path.Combine(app.Environment.WebRootPath, "wiki");
    Directory.CreateDirectory(wikiOutputPath);

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    // Serve static files from the wiki output directory
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(wikiOutputPath),
        RequestPath = "/wiki"
    });

    app.UseRouting();

    app.UseAuthorization();

    app.MapRazorPages();

    logger.Info("Application starting...");
    app.Run();
}
catch (Exception exception)
{
    // NLog: catch setup errors
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    LogManager.Shutdown();
}

// Make Program class public for testing
public partial class Program { }
