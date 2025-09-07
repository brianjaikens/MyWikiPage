namespace MyWikiPage.Services
{
    public interface IWikiConfigService
    {
        string MarkdownFolderPath { get; }
        string OutputFolderPath { get; }
        string GetWebPath(string filePath);
        string GetDefaultPage();
    }
}