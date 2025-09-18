# MyWikiPage - Markdown to HTML Wiki Generator

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Bootstrap](https://img.shields.io/badge/Bootstrap-7952B3?style=flat-square&logo=bootstrap&logoColor=white)
![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)

A modern ASP.NET Core Razor Pages application that seamlessly converts markdown files into a navigable HTML wiki with live content updates.

I feel the need to apologise for this read me. I didn't write it. It was Copilot. It's just a little too much for my tastes. My fault for not restraining it more. But this whole project was an AI experiment. There are better alternatives to creating sites from Markdown, like Jeckyl.

## Features

- **Markdown Processing**: Converts .md files to HTML using Markdig with advanced extensions
- **Internal Link Resolution**: Automatically converts links between markdown files to work in HTML
- **Responsive Design**: Clean, Bootstrap-based UI with dark/light theme support
- **Live Content Updates**: Instant wiki refresh without full page reloads
- **Smart Refresh**: Intelligent content updates with smooth transitions and loading states
- **Wiki-First Experience**: Generated content displays directly as the main page interface
- **Modern UX**: App-like experience with visual feedback and animations

## How to Use

### Quick Start
1. **Start the Application**: Run `dotnet run` from the MyWikiPage project directory
2. **Access Your Wiki**: Navigate to the homepage - it will display your wiki content directly
3. **Add Content**: Place your .md files in the `markdown` folder
4. **Live Update**: Click the refresh button in the navigation to instantly update content
5. **Manage**: Use the "Manage" link for advanced wiki administration

### Enhanced User Experience Flow
```
1. Edit Markdown Files
2. Click Refresh Button (in navigation)
3. Smooth loading animation appears
4. Content regenerates and updates instantly
5. Success notification shows briefly
6. See your changes immediately (no page reload!)
```

## Folder Structure

```
markdown/                    # Source markdown files
  |-- index.md              # Main page (auto-detected)
  |-- contents.md           # Navigation page
  |-- *.md                  # Your content files

wwwroot/wiki/               # Generated HTML files (auto-created)
  |-- index.html            # Generated from index.md
  |-- contents.html         # Generated from contents.md
  |-- *.html                # Other generated HTML files

MyWikiPage/                 # Application source code
  |-- Pages/                # Razor Pages
  |-- Services/             # Business logic
  |-- wwwroot/              # Static files
  |-- Program.cs            # Application entry point
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

- **Internal Links**: Use `[text](filename.md)` - automatically converted to .html
- **External Links**: Standard syntax `[text](https://example.com)` works normally
- **Rich Content**: Full markdown support including tables, code blocks, images
- **Navigation**: Create index.md or contents.md for automatic homepage detection

## User Experience Highlights

### Homepage Integration
- **Wiki-First Design**: Your markdown content IS the homepage
- **Seamless Navigation**: Clean interface focused on your content
- **Smart Fallbacks**: Helpful getting-started guide when no content exists

### Live Updates
- **Instant Refresh**: Content updates without losing your place
- **Visual Feedback**: Loading animations and success notifications
- **Smooth Transitions**: Content fades in/out elegantly during updates
- **Error Handling**: Clear error messages with helpful suggestions

### Modern Interface
- **Dark/Light Themes**: Automatic theme switching with preferences saved
- **Responsive Design**: Perfect on desktop, tablet, and mobile
- **Professional Styling**: Clean, readable typography and layout
- **Intuitive Controls**: Obvious refresh and management options

## Sample Content

The project includes professionally crafted sample markdown files:
- **Getting Started Guide**: Complete instructions for new users
- **Feature Documentation**: Comprehensive feature overview
- **Navigation Examples**: Demonstration of internal linking
- **Markdown Syntax**: Reference for formatting options

## Getting Started

### First Time Setup
1. **Clone/Download**: Get the project files
2. **Install Dependencies**: Run `dotnet restore`
3. **Build**: Run `dotnet build`
4. **Launch**: Run `dotnet run`
5. **Access**: Navigate to `https://localhost:7xxx`

### Content Creation Workflow
1. **Add Files**: Create .md files in the `markdown` folder
2. **Write Content**: Use standard markdown syntax
3. **Link Pages**: Reference other files with `[text](file.md)`
4. **Update**: Click the refresh button to see changes instantly
5. **Share**: Your wiki is immediately accessible to others

## Tech Stack

![C#](https://img.shields.io/badge/C%23-239120?style=flat-square&logo=c-sharp&logoColor=white)
![.NET 8](https://img.shields.io/badge/.NET%208-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Razor Pages](https://img.shields.io/badge/Razor%20Pages-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Bootstrap](https://img.shields.io/badge/Bootstrap%205-7952B3?style=flat-square&logo=bootstrap&logoColor=white)
![Markdig](https://img.shields.io/badge/Markdig-FF6B6B?style=flat-square&logo=markdown&logoColor=white)

### Architecture Highlights
- **Razor Pages**: Clean, page-focused architecture
- **Dependency Injection**: Proper service registration and lifecycle management
- **Configuration**: Flexible settings with appsettings.json
- **Logging**: Comprehensive logging with NLog integration
- **Error Handling**: Graceful error handling with user-friendly messages

## Advanced Features

### Smart Content Management
- **Automatic Detection**: Finds index.md or contents.md as homepage
- **Link Processing**: Converts .md links to .html automatically
- **File Monitoring**: Detects new files and updates navigation
- **Batch Processing**: Handles multiple file updates efficiently

### Theme System
- **Persistent Preferences**: Theme choice saved in localStorage
- **Synchronized Styling**: All components respect theme selection
- **Smooth Transitions**: Theme switching with elegant animations
- **System Integration**: Respects user's OS theme preferences

### Performance Optimizations
- **Partial Updates**: Only refreshes changed content, not entire page
- **Caching**: Efficient file reading and processing
- **Async Operations**: Non-blocking I/O for better responsiveness
- **Resource Management**: Proper disposal and cleanup

## Project Stats

![GitHub stars](https://img.shields.io/github/stars/brianjaikens/MyWikiPage?style=flat-square)
![GitHub forks](https://img.shields.io/github/forks/brianjaikens/MyWikiPage?style=flat-square)
![GitHub issues](https://img.shields.io/github/issues/brianjaikens/MyWikiPage?style=flat-square)
![GitHub pull requests](https://img.shields.io/github/issues-pr/brianjaikens/MyWikiPage?style=flat-square)
![GitHub last commit](https://img.shields.io/github/last-commit/brianjaikens/MyWikiPage?style=flat-square)
![GitHub repo size](https://img.shields.io/github/repo-size/brianjaikens/MyWikiPage?style=flat-square)

---

### Perfect For
- **Personal Wikis**: Knowledge bases, notes, documentation
- **Team Documentation**: Shared knowledge repositories
- **Project Documentation**: Technical specs, guides, references
- **Learning Resources**: Course materials, tutorials, references
- **Content Management**: Quick and easy content publishing

**Experience the future of markdown-based wikis with MyWikiPage!**
