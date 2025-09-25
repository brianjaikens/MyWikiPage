# Link Test Page

This page tests various types of links to ensure they work correctly with our new direct rendering approach.

## Internal Wiki Links

These links should work with AJAX navigation:

- [Go to Index](index.md) - Main wiki page
- [View Contents](contents.md) - Table of contents
- [Getting Started Guide](getting-started.md) - How to use the wiki

## External Links

These should open normally:

- [External Site](https://www.example.com) - Opens in same tab
- [GitHub](https://github.com) - External repository
- [Microsoft Docs](https://docs.microsoft.com) - Documentation

## Mixed Content

Here's a paragraph with both [internal links](features.md) and [external links](https://stackoverflow.com).

### Test List

1. Click on internal links above - they should load content via AJAX
2. Check that the URL updates when navigating
3. Use browser back/forward buttons
4. Verify external links work normally
5. Test in both light and dark themes

## Code Example

```markdown
[Internal Link](page.md) -> Converts to AJAX navigation
[External Link](https://example.com) -> Opens normally
```

That's it! Test the navigation thoroughly.