# Features

This markdown wiki system provides several useful features:

## Core Features

### Markdown Processing
- Full markdown support using Markdig
- Advanced extensions enabled
- Code syntax highlighting
- Tables, lists, and formatting

### Link Processing
- Automatic conversion of `.md` links to `.html`
- Internal navigation between pages
- External links work as expected

### Web Interface
- Clean, responsive design
- Easy refresh functionality
- File listing and management
- Bootstrap-based styling

## File Management

### Input
- Markdown files in the `markdown` folder
- Supports subdirectories
- Standard `.md` file extension

### Output
- HTML files generated in `wwwroot/wiki`
- Maintains folder structure
- Includes navigation and styling

## Navigation Features

### Default Pages
The system automatically looks for and redirects to:
1. `index.html` (if it exists)
2. `contents.html` (if it exists)
3. Falls back to the wiki file listing

### Page Structure
Each generated page includes:
- Proper HTML document structure
- CSS styling for readability
- Navigation links
- Responsive design

## Technical Details

### Technologies Used
- ASP.NET Core 8 with Razor Pages
- Markdig for markdown processing
- Bootstrap for styling
- File system monitoring

### Configuration
- Configurable markdown source folder
- Configurable output folder
- Web-accessible generated content

## Getting Back

- [Getting Started](getting-started.md)
- [Contents](contents.md)
- [Index](index.md)