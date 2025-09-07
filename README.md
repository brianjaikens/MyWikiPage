# MyWikiPage - Markdown to HTML Wiki Generator

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Bootstrap](https://img.shields.io/badge/Bootstrap-7952B3?style=flat-square&logo=bootstrap&logoColor=white)
![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)

A simple ASP.NET Core Razor Pages application that converts markdown files into a navigable HTML wiki.

## ? Features

- ?? **Markdown Processing**: Converts .md files to HTML using Markdig
- ?? **Internal Link Resolution**: Automatically converts links between markdown files to work in HTML
- ?? **Responsive Design**: Clean, Bootstrap-based UI
- ? **Easy Refresh**: One-click regeneration of all HTML pages
- ?? **Auto-Navigation**: Automatically redirects to index.html or contents.html if available

## ?? How to Use

1. **Start the Application**: Run `dotnet run` from the MyWikiPage project directory
2. **Navigate to Wiki**: Go to `/Wiki` page or click "Wiki" in the navigation
3. **Add Markdown Files**: Place your .md files in the `markdown` folder
4. **Generate HTML**: Click "Refresh Wiki" to convert markdown to HTML
5. **View Content**: Click "Open Default Page" or browse generated files

## ?? Folder Structure

```
markdown/              # Source markdown files
??? index.md          # Will become the default page
??? contents.md       # Alternative default page
??? *.md              # Other markdown files

wwwroot/wiki/         # Generated HTML files (auto-created)
??? index.html        # Generated from index.md
??? contents.html     # Generated from contents.md
??? *.html            # Other generated HTML files

MyWikiPage/           # Application source code
??? Pages/            # Razor Pages
??? Services/         # Business logic
??? wwwroot/          # Static files
??? Program.cs        # Application entry point
```

## ?? Configuration

Edit `appsettings.json` to customize paths:

```json
{
  "Wiki": {
    "MarkdownFolder": "markdown",
    "OutputFolder": "wwwroot/wiki"
  }
}
```

## ?? Markdown Tips

- Use `[text](filename.md)` for internal links
- The system automatically converts .md links to .html in generated pages
- Standard markdown syntax is supported (headers, lists, code blocks, tables, etc.)
- External links work normally: `[text](https://example.com)`

## ?? Sample Files

The project includes sample markdown files demonstrating:
- Basic markdown syntax
- Internal linking between pages
- Navigation structure

## ?? Getting Started

1. Clone/download the project
2. Run `dotnet restore` and `dotnet build`
3. Run `dotnet run`
4. Navigate to `https://localhost:7xxx/Wiki`
5. Click "Refresh Wiki" to generate sample pages
6. Edit markdown files and refresh to see changes

## ??? Tech Stack

![C#](https://img.shields.io/badge/C%23-239120?style=flat-square&logo=c-sharp&logoColor=white)
![.NET 8](https://img.shields.io/badge/.NET%208-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Razor Pages](https://img.shields.io/badge/Razor%20Pages-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Bootstrap](https://img.shields.io/badge/Bootstrap%205-7952B3?style=flat-square&logo=bootstrap&logoColor=white)
![Markdig](https://img.shields.io/badge/Markdig-FF6B6B?style=flat-square&logo=markdown&logoColor=white)

## ?? Project Stats

![GitHub stars](https://img.shields.io/github/stars/brianjaikens/MyWikiPage?style=flat-square)
![GitHub forks](https://img.shields.io/github/forks/brianjaikens/MyWikiPage?style=flat-square)
![GitHub issues](https://img.shields.io/github/issues/brianjaikens/MyWikiPage?style=flat-square)
![GitHub pull requests](https://img.shields.io/github/issues-pr/brianjaikens/MyWikiPage?style=flat-square)
![GitHub last commit](https://img.shields.io/github/last-commit/brianjaikens/MyWikiPage?style=flat-square)
![GitHub repo size](https://img.shields.io/github/repo-size/brianjaikens/MyWikiPage?style=flat-square)