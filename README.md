# MyWikiPage - Markdown to HTML Wiki Generator

A simple ASP.NET Core Razor Pages application that converts markdown files into a navigable HTML wiki.

## Features

- **Markdown Processing**: Converts .md files to HTML using Markdig
- **Internal Link Resolution**: Automatically converts links between markdown files to work in HTML
- **Responsive Design**: Clean, Bootstrap-based UI
- **Easy Refresh**: One-click regeneration of all HTML pages
- **Auto-Navigation**: Automatically redirects to index.html or contents.html if available

## How to Use

1. **Start the Application**: Run `dotnet run` from the MyWikiPage project directory
2. **Navigate to Wiki**: Go to `/Wiki` page or click "Wiki" in the navigation
3. **Add Markdown Files**: Place your .md files in the `markdown` folder
4. **Generate HTML**: Click "Refresh Wiki" to convert markdown to HTML
5. **View Content**: Click "Open Default Page" or browse generated files

## Folder Structure

```
??? markdown/           # Source markdown files
?   ??? index.md       # Will become the default page
?   ??? contents.md    # Alternative default page
?   ??? *.md          # Other markdown files
??? wwwroot/wiki/      # Generated HTML files (auto-created)
??? MyWikiPage/        # Application source code
```

## Configuration

Edit `appsettings.json` to customize paths:

```json
{
  "Wiki": {
    "MarkdownFolder": "markdown",
    "OutputFolder": "wwwroot/wiki"
  }
}
```

## Markdown Tips

- Use `[text](filename.md)` for internal links
- The system automatically converts .md links to .html in generated pages
- Standard markdown syntax is supported (headers, lists, code blocks, tables, etc.)
- External links work normally: `[text](https://example.com)`

## Sample Files

The project includes sample markdown files demonstrating:
- Basic markdown syntax
- Internal linking between pages
- Navigation structure

## Getting Started

1. Clone/download the project
2. Run `dotnet restore` and `dotnet build`
3. Run `dotnet run`
4. Navigate to `https://localhost:7xxx/Wiki`
5. Click "Refresh Wiki" to generate sample pages
6. Edit markdown files and refresh to see changes