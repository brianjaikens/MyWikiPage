namespace MyWikiPage.Services
{
    public interface IMarkdownService
    {
        Task<string> ConvertMarkdownToHtmlAsync(string markdownContent, string baseDirectory);
        Task<bool> GenerateHtmlFromMarkdownFolderAsync(string markdownFolderPath, string outputFolderPath);
        Task<List<string>> GetMarkdownFilesAsync(string folderPath);
        string ProcessInternalLinks(string html, string baseDirectory, string outputDirectory);
    }
}