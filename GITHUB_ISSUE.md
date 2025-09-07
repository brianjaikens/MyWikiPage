# GitHub Issue: Refresh button triggers page reload

**Title:** Refresh button triggers page reload

**Labels:** bug, high-priority

**Body:**

## Problem Description

The refresh button in the navigation bar is triggering a full page reload instead of only refreshing the iframe content as intended.

## Expected Behavior
- Click refresh button
- Only the iframe content should reload with the updated wiki content
- No full page reload should occur
- User should remain in the same position on the page

## Actual Behavior
- Click refresh button
- Entire page reloads instead of just the iframe content
- User loses their current position and context

## Technical Details
- The issue occurs despite implementing iframe-based partial refresh system
- The `window.refreshIframeContent` function may not be executing properly
- Possible timing issues between wiki generation and iframe refresh

## Steps to Reproduce
1. Navigate to the wiki homepage
2. Click the refresh button in the navigation bar
3. Observe that the entire page reloads instead of just the iframe content

## Environment
- .NET 8
- ASP.NET Core Razor Pages
- Bootstrap 5
- Chrome/Edge browser

## Acceptance Criteria
- [ ] Refresh button only updates iframe content
- [ ] No full page reload occurs
- [ ] Visual loading indicators work correctly
- [ ] Success/error notifications display properly
- [ ] User maintains their current page context

## Related Files
- `Pages/Shared/_Layout.cshtml` - Contains refresh button logic
- `Pages/Index.cshtml` - Contains iframe and refresh function
- `Pages/WikiEmbed.cshtml.cs` - Handles iframe content generation

## Priority
High - This affects the core user experience of the application.

---

**Instructions:**
1. Go to https://github.com/brianjaikens/MyWikiPage/issues/new
2. Copy the title above
3. Copy the body content above
4. Add labels: "bug" and "high-priority"
5. Submit the issue