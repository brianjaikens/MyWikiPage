using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyWikiPage.Services;
using MyWikiPage.Tests.Helpers;

namespace MyWikiPage.Tests.Services;

public class MarkdownServiceDemoTests : IDisposable
{
    private readonly IMarkdownService _markdownService;
    private readonly ServiceProvider _serviceProvider;
    private readonly string _testDirectory;

    public MarkdownServiceDemoTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "MarkdownServiceDemo", Guid.NewGuid().ToString());
        
        var configuration = TestServiceHelper.CreateTestConfiguration();
        _serviceProvider = TestServiceHelper.CreateTestServiceProvider(configuration, _testDirectory);
        _markdownService = _serviceProvider.GetRequiredService<IMarkdownService>();
        
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public async Task GenerateCompleteHtmlExample_WithAllFeatures()
    {
        // Arrange
        var markdownDir = Path.Combine(_testDirectory, "markdown");
        var outputDir = Path.Combine(_testDirectory, "output");
        Directory.CreateDirectory(markdownDir);
        Directory.CreateDirectory(outputDir);

        var demoMarkdown = @"# MyWikiPage Demo

Welcome to the **enhanced** MyWikiPage with *embedded CSS* and theme support!

## Key Features

### ?? Theme Toggle
Click the theme button in the top-right corner to switch between light and dark modes.

### ?? Responsive Design
- Mobile-friendly navigation
- Responsive tables and layouts
- Touch-friendly controls

### ?? Modern Styling
- Clean typography
- Professional color scheme
- Smooth animations

## Code Examples

### C# Example
```csharp
public class WikiPage
{
    public string Title { get; set; } = ""Default"";
    public string Content { get; set; } = string.Empty;
    
    public void Render()
    {
        Console.WriteLine($""Rendering: {Title}"");
    }
}
```

### JavaScript Theme Toggle
```javascript
function toggleTheme() {
    const current = getTheme();
    const newTheme = current === 'dark' ? 'light' : 'dark';
    setTheme(newTheme);
}
```

## Data Table

| Feature | Status | Browser Support |
|---------|--------|-----------------|
| Theme Toggle | ? Complete | All Modern |
| Responsive | ? Complete | All Modern |
| CSS Variables | ? Complete | IE 11+ |
| Icons | ? Complete | All Modern |

## Advanced Features

### Lists
1. **Ordered lists** work perfectly
2. With proper spacing
3. And clean styling

- Unordered lists too
- Multiple levels supported
  - Nested items
  - With proper indentation

### Quotes
> This is a blockquote demonstrating the enhanced styling.
> 
> It supports multiple paragraphs and looks great in both light and dark modes.

### Links
- [Internal Link](contents.md) - Links to other wiki pages
- [External Link](https://github.com) - External links work too
- [Email Link](mailto:test@example.com) - Even email links

## Navigation
Use the navigation bar above to move between pages:
- ?? **Home** - Return to the main page
- ?? **Contents** - View the table of contents  
- ? **Back to Wiki** - Return to the wiki management

---

*This page demonstrates all the new features of MyWikiPage with embedded CSS and theme support!*";

        await File.WriteAllTextAsync(Path.Combine(markdownDir, "demo.md"), demoMarkdown);

        // Act
        var result = await _markdownService.GenerateHtmlFromMarkdownFolderAsync(markdownDir, outputDir);

        // Assert
        result.Should().BeTrue();
        
        var htmlFile = Path.Combine(outputDir, "demo.html");
        File.Exists(htmlFile).Should().BeTrue();
        
        var htmlContent = await File.ReadAllTextAsync(htmlFile);
        
        // Copy the generated file to a location where we can easily view it
        var demoOutputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MyWikiPage-Demo.html");
        await File.WriteAllTextAsync(demoOutputPath, htmlContent);
        
        // Verify the content has all our expected features
        htmlContent.Should().Contain("MyWikiPage Demo");
        htmlContent.Should().Contain("Theme Toggle");
        htmlContent.Should().Contain("class=\"theme-toggle\"");
        htmlContent.Should().Contain("function toggleTheme()");
        htmlContent.Should().Contain(":root {");
        htmlContent.Should().Contain("[data-theme=\"dark\"] {");
        htmlContent.Should().Contain("https://cdn.jsdelivr.net/npm/bootstrap-icons");
        
        // Log the path for easy access
        Console.WriteLine($"Demo HTML file created at: {demoOutputPath}");
        Console.WriteLine("Open this file in a browser to see the new embedded CSS features!");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
        _serviceProvider?.Dispose();
    }
}