namespace MyWikiPage.Tests.TestData;

/// <summary>
/// Sample markdown content for testing
/// </summary>
public static class TestMarkdownContent
{
    public const string SimpleMarkdown = @"# Test Page

This is a test page with some content.

## Section 1

Some content here.

- List item 1
- List item 2

## Section 2

More content with a [link](other-page.md).";

    public const string MarkdownWithLinks = @"# Home Page

Welcome to the test wiki.

## Navigation

- [Contents](contents.md)
- [Getting Started](getting-started.md)
- [Features](features.md)

## External Links

Visit [GitHub](https://github.com) for more information.";

    public const string ContentsMarkdown = @"# Contents

This is the contents page.

## Pages

1. [Home](index.md)
2. [Getting Started](getting-started.md)
3. [Features](features.md)

## External Resources

- [Markdown Guide](https://www.markdownguide.org/)";

    public const string InvalidMarkdown = @"# Invalid Page

This page has some invalid content <script>alert('test')</script>

And some [broken link](nonexistent.md).";
}