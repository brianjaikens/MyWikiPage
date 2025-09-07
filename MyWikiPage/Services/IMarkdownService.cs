namespace MyWikiPage.Services
{
    public interface IMarkdownService
    {
        Task<string> ConvertMarkdownToHtmlAsync(string markdownContent, string baseDirectory);
        Task<List<string>> GetMarkdownFilesAsync(string folderPath);
        Task<bool> GenerateHtmlFromMarkdownFolderAsync(string markdownFolderPath, string outputFolderPath);
        string ProcessInternalLinks(string html, string baseDirectory, string outputDirectory);
    }
}