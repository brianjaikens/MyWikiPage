using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using MyWikiPage.Tests.TestData;
using System.Net;

namespace MyWikiPage.Tests.Integration;

public class WebApplicationIntegrationTests : IClassFixture<MyWikiPageWebApplicationFactory>
{
    private readonly MyWikiPageWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public WebApplicationIntegrationTests(MyWikiPageWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _factory.CreateTestDirectories();
    }

    [Fact]
    public async Task Get_Index_ReturnsSuccessAndCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType?.ToString().Should().Contain("text/html");
    }

    [Fact]
    public async Task Get_Index_WithoutGeneratedContent_ShowsWelcomeMessage()
    {
        // Act
        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("Welcome to MyWikiPage");
        content.Should().Contain("Go to Wiki");
    }

    [Fact]
    public async Task Get_Index_WithGeneratedContent_ShowsIframe()
    {
        // Arrange
        await _factory.CreateTestHtmlFile("index.html", "<html><body><h1>Test Wiki</h1></body></html>");

        // Act
        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("wiki-iframe");
        content.Should().Contain("/wiki-embed");
    }

    [Fact]
    public async Task Get_Wiki_ReturnsSuccessAndCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/Wiki");

        // Assert
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType?.ToString().Should().Contain("text/html");
    }

    [Fact]
    public async Task Get_Wiki_ShowsManagementInterface()
    {
        // Act
        var response = await _client.GetAsync("/Wiki");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("Markdown Wiki");
        content.Should().Contain("Wiki Configuration");
    }

    [Fact]
    public async Task Get_WikiEmbed_WithoutContent_ReturnsEmptyResponse()
    {
        // Act
        var response = await _client.GetAsync("/wiki-embed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_WikiEmbed_WithContent_ReturnsWikiContent()
    {
        // Arrange
        var testHtml = @"<html><body><h1>Test Wiki Content</h1><p>This is test content</p></body></html>";
        await _factory.CreateTestHtmlFile("index.html", testHtml);

        // Act
        var response = await _client.GetAsync("/wiki-embed?theme=light");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("Test Wiki Content");
        content.Should().Contain("This is test content");
    }

    [Fact]
    public async Task Get_WikiEmbed_WithSpecificPage_ReturnsRequestedPage()
    {
        // Arrange
        var indexHtml = @"<html><body><h1>Index</h1></body></html>";
        var contentsHtml = @"<html><body><h1>Contents Page</h1><p>Table of contents</p></body></html>";
        
        await _factory.CreateTestHtmlFile("index.html", indexHtml);
        await _factory.CreateTestHtmlFile("contents.html", contentsHtml);

        // Act
        var response = await _client.GetAsync("/wiki-embed?theme=light&page=contents.html");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("Contents Page");
        content.Should().Contain("Table of contents");
        content.Should().NotContain("Index");
    }

    [Fact]
    public async Task Post_WikiRefreshAjax_ReturnsJsonSuccess()
    {
        // Arrange
        await _factory.CreateTestMarkdownFile("index.md", TestMarkdownContent.SimpleMarkdown);

        // Act
        var response = await _client.PostAsync("/Wiki?handler=RefreshAjax", new StringContent(""));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.ToString().Should().Contain("application/json");
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"success\":true");
        content.Should().Contain("successfully generated");
    }

    [Fact]
    public async Task Get_Error_ReturnsErrorPage()
    {
        // Act
        var response = await _client.GetAsync("/Error");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Error");
        content.Should().Contain("An error occurred");
    }

    [Fact]
    public async Task Get_NonExistentPage_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/NonExistentPage");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Wiki")]
    [InlineData("/wiki-embed")]
    [InlineData("/Error")]
    public async Task Get_Pages_ReturnsSuccessStatusCodes(string url)
    {
        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect);
    }
}