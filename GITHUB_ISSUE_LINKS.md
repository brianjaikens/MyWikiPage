# GitHub Issue: Internal wiki links not working in iframe

**Title:** Internal wiki links not working in iframe

**Labels:** bug, high-priority

**Body:**

## Problem Description

Internal links between wiki pages are not functioning correctly within the iframe context, leading to broken navigation and poor user experience.

## Expected Behavior
- Click internal wiki links (e.g., "Contents", "Getting Started", "Features")
- Navigate to the linked page within the iframe seamlessly
- Maintain consistent styling and theme
- User can browse through all wiki pages without issues

## Actual Behavior
- Click internal wiki links results in blank pages or navigation failures
- Links may not load properly within iframe context
- User cannot navigate between wiki pages effectively
- Broken user experience for multi-page wikis

## Technical Details
- **Server-side link rewriting**: The `RewriteLinksForIframe()` method in `WikiEmbedModel` may not be working correctly
- **Relative path resolution**: Links like `[Contents](contents.md)` may not resolve properly in iframe context
- **WikiEmbed page loading**: The `page` parameter handling might have issues
- **Theme preservation**: Links may not maintain theme consistency during navigation

## Steps to Reproduce
1. Navigate to the wiki homepage with iframe content
2. Look for internal links in the generated content (e.g., "Contents", "Getting Started")
3. Click on any internal wiki link
4. Observe that navigation fails or results in blank/broken pages

## Environment
- .NET 8
- ASP.NET Core Razor Pages  
- Bootstrap 5
- Iframe-based wiki architecture
- Chrome/Edge browser

## Acceptance Criteria
- [ ] Internal links navigate correctly within iframe
- [ ] All wiki pages load properly when linked
- [ ] Theme is maintained during navigation
- [ ] No blank pages or broken navigation
- [ ] Smooth user experience for multi-page browsing
- [ ] Back/forward navigation works within iframe context

## Related Files
- `Pages/WikiEmbed.cshtml.cs` - Contains `RewriteLinksForIframe()` method
- `Pages/WikiEmbed.cshtml` - Displays iframe content
- `Pages/Index.cshtml` - Contains iframe implementation
- Generated HTML files in `wwwroot/wiki/` - Source content with links

## Technical Investigation Needed
1. **Link rewriting logic**: Verify `RewriteLinksForIframe()` converts `.md` links to proper WikiEmbed URLs
2. **Parameter handling**: Check if `page` parameter is correctly processed in `WikiEmbedModel.OnGet()`
3. **File path resolution**: Ensure requested files are found and loaded correctly
4. **URL construction**: Verify WikiEmbed URLs are built correctly with theme and page parameters

## Impact
**High Priority** - This breaks the core functionality of a multi-page wiki system. Users cannot navigate between pages, making the application essentially unusable for its intended purpose.

## Potential Root Causes
- Incorrect link rewriting in server-side processing
- Missing or malformed `page` parameter handling
- File path resolution issues in `LoadWikiContent()` method
- JavaScript interference with link navigation in iframe

---

**Issue URL:** https://github.com/brianjaikens/MyWikiPage/issues/2