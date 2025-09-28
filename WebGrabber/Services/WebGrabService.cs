using HtmlAgilityPack;
using ReverseMarkdown;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace WebGrabber.Services;

public class WebGrabService : IWebGrabService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebGrabService> _logger;
    private readonly List<string> _log = new();
    private readonly HashSet<string> _visited = new(StringComparer.OrdinalIgnoreCase);

    public WebGrabService(IHttpClientFactory httpClientFactory, ILogger<WebGrabService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async System.Threading.Tasks.Task<WebGrabResult> GrabSiteAsync(WebGrabConfig config, IProgress<string>? progress = null, System.Threading.CancellationToken cancellationToken = default)
    {
        _log.Clear();
        _visited.Clear();

        _logger.LogDebug("Starting GrabSiteAsync. DiscoverOnly={DiscoverOnly}, StartUrl={StartUrl}, MaxPages={MaxPages}, CrawlLimit={CrawlLimit}", config.DiscoverOnly, config.StartUrl, config.MaxPages, config.CrawlLimit);

        if (!Uri.TryCreate(config.StartUrl, UriKind.Absolute, out var startUri))
        {
            _logger.LogWarning("Invalid start URL provided: {StartUrl}", config.StartUrl);
            return new WebGrabResult(false, "Invalid start URL", _log);
        }

        // For discovery-only runs we don't create folders or save files
        var requestedMarkdownFolder = config.MarkdownFolder ?? string.Empty;
        string markdownFolder = requestedMarkdownFolder;
        if (!config.DiscoverOnly)
        {
            if (!Path.IsPathRooted(markdownFolder))
            {
                markdownFolder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, markdownFolder ?? string.Empty));
            }

            Directory.CreateDirectory(markdownFolder);
            Directory.CreateDirectory(Path.Combine(markdownFolder, "images"));
        }

        var toVisit = new Queue<Uri>();
        toVisit.Enqueue(startUri);
        _visited.Add(startUri.AbsoluteUri);

        var client = _httpClientFactory.CreateClient();
        try
        {
            if (!string.IsNullOrEmpty(config.UserAgent))
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(config.UserAgent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to set User-Agent header: {Agent}", config.UserAgent);
        }

        var converter = new Converter(new Config());

        // Counters to number images per page (keyed by page file name)
        var imageCounters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        // Map of original image URL (or data URI) -> saved relative path to avoid duplicate downloads
        var imageMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Determine base URL prefix to decide which links to keep
        Uri baseUri;
        var baseUrlCandidate = config.BaseUrl ?? string.Empty;
        if (!Uri.TryCreate(baseUrlCandidate, UriKind.Absolute, out var parsed) || parsed == null)
        {
            // fallback to site root from startUri
            baseUri = new Uri(startUri.GetLeftPart(UriPartial.Authority));
        }
        else
        {
            baseUri = parsed;
        }
        var basePrefix = baseUri.AbsoluteUri.TrimEnd('/');

        _logger.LogDebug("BasePrefix set to {BasePrefix}", basePrefix);

        // Helper to map a saved relative image path to a URL under the configured BaseUrl
        static string MapSavedImageToUrl(string candidate, string basePrefix)
        {
            if (string.IsNullOrWhiteSpace(candidate)) return candidate;
            var rel = candidate.Replace('\\', '/').TrimStart('/');
            return rel;
        }

        // Discovery-only mode: lightweight crawl that only counts pages and does not save content
        if (config.DiscoverOnly)
        {
            _logger.LogInformation("Running discovery-only crawl for {StartUrl}", startUri);
            int pagesFound = _visited.Count;
            progress?.Report($"Pages found: {pagesFound}");

            while (toVisit.Count > 0 && _visited.Count < config.CrawlLimit)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var uri = toVisit.Dequeue();
                _log.Add($"Visiting: {uri}");
                progress?.Report($"Visiting: {uri}");
                _logger.LogDebug("Discovery visiting {Uri}", uri);

                try
                {
                    var resp = await client.GetAsync(uri, cancellationToken);
                    _logger.LogDebug("HTTP GET {Uri} -> {StatusCode}", uri, resp.StatusCode);
                    if (!resp.IsSuccessStatusCode)
                    {
                        _log.Add($"Failed to get {uri}: {resp.StatusCode}");
                        progress?.Report($"Failed to get {resp.StatusCode}");
                        continue;
                    }

                    var html = await resp.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogTrace("Fetched {Bytes} bytes from {Uri}", html?.Length ?? 0, uri);
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);

                    // Find links to other pages within same base URL
                    var anchors = doc.DocumentNode.SelectNodes("//a[@href]") ?? Enumerable.Empty<HtmlNode>();
                    foreach (var a in anchors.ToList())
                    {
                        var href = a.GetAttributeValue("href", "");
                        if (string.IsNullOrEmpty(href)) continue;
                        if (href.StartsWith("#")) continue;

                        if (!Uri.TryCreate(uri, href, out var link)) continue;

                        var linkAbs = link.AbsoluteUri;

                        // Only allow non-image links that are under the configured BaseUrl
                        var linkAbsTrim = linkAbs.TrimEnd('/');
                        if (!(string.Equals(linkAbsTrim, basePrefix, StringComparison.OrdinalIgnoreCase) ||
                              linkAbsTrim.StartsWith(basePrefix + "/", StringComparison.OrdinalIgnoreCase)))
                        {
                            _logger.LogTrace("Skipping external link {Link}", linkAbs);
                            continue;
                        }

                        if (!_visited.Contains(link.AbsoluteUri) && _visited.Count < config.CrawlLimit)
                        {
                            _visited.Add(link.AbsoluteUri);
                            toVisit.Enqueue(link);
                            pagesFound = _visited.Count;
                            progress?.Report($"Pages found: {pagesFound}");
                            _logger.LogDebug("Discovered internal page {Link} (total {Count})", linkAbs, pagesFound);
                        }
                    }

                    _log.Add($"Discovered page: {uri}");
                    progress?.Report($"Discovered page: {uri}");
                }
                catch (OperationCanceledException)
                {
                    _log.Add("Operation cancelled");
                    _logger.LogInformation("Discovery cancelled for {Uri}", uri);
                    return new WebGrabResult(false, "Cancelled", _log);
                }
                catch (Exception ex)
                {
                    _log.Add($"Error visiting {uri}: {ex.Message}");
                    progress?.Report($"Error visiting {uri}: {ex.Message}");
                    _logger.LogWarning(ex, "Error while discovering {Uri}", uri);
                }
            }

            progress?.Report($"Pages found: {_visited.Count}");
            _logger.LogInformation("Discovery completed. Pages found: {Count}", _visited.Count);
            return new WebGrabResult(true, "Discovery completed", _log);
        }

        // Full grab mode: process pages, save images and markdown
        while (toVisit.Count > 0 && _visited.Count <= config.MaxPages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var uri = toVisit.Dequeue();
            _log.Add($"Visiting: {uri}");
            progress?.Report($"Visiting: {uri}");
            _logger.LogInformation("Processing page {Uri}", uri);

            try
            {
                var resp = await client.GetAsync(uri, cancellationToken);
                _logger.LogDebug("HTTP GET {Uri} -> {StatusCode}", uri, resp.StatusCode);
                if (!resp.IsSuccessStatusCode)
                {
                    _log.Add($"Failed to get {uri}: {resp.StatusCode}");
                    progress?.Report($"Failed to get {resp.StatusCode}");
                    continue;
                }

                var html = await resp.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogTrace("Fetched {Bytes} bytes from {Uri}", html?.Length ?? 0, uri);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Remove script and style elements to avoid saving JavaScript into markdown
                var scriptsAndStyles = doc.DocumentNode.SelectNodes("//script|//style") ?? Enumerable.Empty<HtmlNode>();
                foreach (var node in scriptsAndStyles.ToList())
                {
                    try { node.Remove(); } catch (Exception ex) { _logger.LogDebug(ex, "Failed to remove script/style node"); }
                }

                // Remove inline event handler attributes (onclick, onmouseover, etc.) and javascript: hrefs
                var allNodes = doc.DocumentNode.SelectNodes("//*") ?? Enumerable.Empty<HtmlNode>();
                foreach (var node in allNodes)
                {
                    var attrsToRemove = node.Attributes.Where(a => a.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase)).Select(a => a.Name).ToList();
                    foreach (var name in attrsToRemove)
                    {
                        try { node.Attributes.Remove(name); } catch (Exception ex) { _logger.LogDebug(ex, "Failed to remove attribute {Attr} on node {Node}", name, node.Name); }
                    }

                    var hrefAttr = node.Attributes["href"];
                    if (hrefAttr != null && !string.IsNullOrWhiteSpace(hrefAttr.Value) && hrefAttr.Value.TrimStart().StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                    {
                        try { node.Attributes.Remove("href"); } catch (Exception ex) { _logger.LogDebug(ex, "Failed to remove javascript: href attribute"); }
                    }

                    var srcAttr = node.Attributes["src"];
                    if (srcAttr != null && !string.IsNullOrWhiteSpace(srcAttr.Value) && srcAttr.Value.TrimStart().StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                    {
                        try { node.Attributes.Remove("src"); } catch (Exception ex) { _logger.LogDebug(ex, "Failed to remove javascript: src attribute"); }
                    }
                }

                // Derive a base name for this page to use in image filenames
                var pageFileName = MakeFileNameForUri(uri); // e.g. path-with-dashes.md
                var pageBaseName = Path.GetFileNameWithoutExtension(pageFileName);
                if (string.IsNullOrEmpty(pageBaseName)) pageBaseName = "page";
                if (!imageCounters.ContainsKey(pageBaseName)) imageCounters[pageBaseName] = 0;

                // Save images (handle src, data-src and srcset)
                var images = doc.DocumentNode.SelectNodes("//img") ?? Enumerable.Empty<HtmlNode>();
                foreach (var img in images)
                {
                    // collect candidate urls from src, data-src and srcset
                    var src = img.GetAttributeValue("src", "");
                    var dataSrc = img.GetAttributeValue("data-src", "");
                    var srcset = img.GetAttributeValue("srcset", "");

                    // If any of the image sources are data URIs (embedded/base64), skip the entire image element
                    bool HasDataUriInSrcset(string s)
                    {
                        if (string.IsNullOrEmpty(s)) return false;
                        var parts = s.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0);
                        foreach (var part in parts)
                        {
                            var spaceIdx = part.IndexOf(' ');
                            var urlPart = spaceIdx > 0 ? part.Substring(0, spaceIdx) : part;
                            if (urlPart.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return true;
                        }
                        return false;
                    }

                    if (!string.IsNullOrEmpty(src) && src.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
                        || !string.IsNullOrEmpty(dataSrc) && dataSrc.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
                        || HasDataUriInSrcset(srcset))
                    {
                        // remove the image element entirely and do not convert it to markdown
                        try { img.Remove(); } catch (Exception ex) { _logger.LogDebug(ex, "Failed to remove data-uri image node"); }
                        _logger.LogDebug("Skipped embedded data-uri image on {Page}", uri);
                        continue;
                    }

                    // attempt to get helpful metadata for filename
                    var altText = img.GetAttributeValue("alt", "").Trim();
                    var titleText = img.GetAttributeValue("title", "").Trim();

                    // Helper to sanitize a candidate name
                    static string SanitizeName(string name)
                    {
                        if (string.IsNullOrEmpty(name)) return string.Empty;
                        // replace invalid chars
                        foreach (var c in Path.GetInvalidFileNameChars())
                            name = name.Replace(c, '-');
                        // collapse spaces and multiple dashes
                        name = Regex.Replace(name, "\\s+", "-").Trim('-');
                        name = Regex.Replace(name, "-+", "-");
                        // limit length
                        if (name.Length > 80) name = name.Substring(0, 80).Trim('-');
                        return name;
                    }

                    // Helper to save single image url and return rewritten relative path or null
                    async Task<string?> SaveImageUrlAsync(string imgUrl)
                    {
                        if (string.IsNullOrWhiteSpace(imgUrl)) return null;

                        // Do not attempt to save embedded/data URI images; skip them
                        if (imgUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                        {
                            _log.Add("Skipping embedded data-uri image");
                            _logger.LogDebug("Skipping embedded data-uri image on {Page}", uri);
                            return null;
                        }

                        // Fix protocol-relative URLs (//example.com/foo)
                        if (imgUrl.StartsWith("//"))
                        {
                            imgUrl = startUri.Scheme + ":" + imgUrl;
                        }

                        if (!Uri.TryCreate(uri, imgUrl, out var imgUri))
                        {
                            _log.Add($"Invalid image URL: {imgUrl}");
                            _logger.LogWarning("Invalid image URL on {Page}: {ImgUrl}", uri, imgUrl);
                            return null;
                        }

                        // Use absolute URI as key to dedupe
                        var absolute = imgUri.AbsoluteUri;
                        if (imageMap.TryGetValue(absolute, out var existing))
                        {
                            _logger.LogDebug("Reusing previously saved image for {Img}", absolute);
                            return existing;
                        }

                        try
                        {
                            using var req = new HttpRequestMessage(HttpMethod.Get, imgUri);
                            // set Referer to the page uri to help servers that require it
                            req.Headers.Referrer = uri;
                            req.Headers.Accept.ParseAdd("*/*");

                            using var response = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                            _logger.LogDebug("Downloading image {Img} -> {Status}", imgUri, response.StatusCode);
                            if (!response.IsSuccessStatusCode)
                            {
                                _log.Add($"Failed to download image {imgUri}: {response.StatusCode} {response.ReasonPhrase}");
                                _logger.LogWarning("Failed to download image {Img}: {Status} {Reason}", imgUri, response.StatusCode, response.ReasonPhrase);
                                return null;
                            }

                            var imgBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

                            // determine extension from content-type
                            var contentType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
                            string? extFromContent = contentType switch
                            {
                                "image/png" => ".png",
                                "image/jpeg" => ".jpg",
                                "image/jpg" => ".jpg",
                                "image/gif" => ".gif",
                                "image/webp" => ".webp",
                                "image/svg+xml" => ".svg",
                                "image/x-icon" => ".ico",
                                "image/vnd.microsoft.icon" => ".ico",
                                _ => null
                            };

                            // get filename from URL path and sanitize
                            var rawName = Path.GetFileName(imgUri.LocalPath);
                            rawName = Uri.UnescapeDataString(rawName ?? string.Empty);

                            // if rawName is empty, try to infer from query parameters (e.g. ?file=name.jpg or filename=...)
                            if (string.IsNullOrEmpty(rawName) && !string.IsNullOrEmpty(imgUri.Query))
                            {
                                var q = imgUri.Query.TrimStart('?');
                                var parts = q.Split('&', StringSplitOptions.RemoveEmptyEntries);
                                foreach (var part in parts)
                                {
                                    var kv = part.Split('=', 2);
                                    if (kv.Length == 2)
                                    {
                                        var key = kv[0].ToLowerInvariant();
                                        var val = Uri.UnescapeDataString(kv[1]);
                                        if (key.Contains("file") || key.Contains("name") || key.Contains("filename"))
                                        {
                                            rawName = val;
                                            break;
                                        }
                                    }
                                }
                            }

                            string fileName;

                            // Ensure there's a meaningful base name for the page
                            var safePageBase = pageBaseName;

                            if (string.IsNullOrEmpty(rawName))
                            {
                                // Try using alt/title text as a hint
                                var hint = SanitizeName(!string.IsNullOrEmpty(altText) ? altText : titleText);
                                if (!string.IsNullOrEmpty(hint))
                                {
                                    // include page base for context
                                    fileName = safePageBase + "-" + hint + (extFromContent ?? ".bin");
                                }
                                else
                                {
                                    // No filename from URL - use page-based numbering
                                    imageCounters[safePageBase]++;
                                    var idx = imageCounters[safePageBase];
                                    fileName = $"{safePageBase}-image-{idx}" + (extFromContent ?? ".bin");
                                }
                            }
                            else
                            {
                                // Use original filename from URL but prepend page name for context
                                var nameOnly = Path.GetFileNameWithoutExtension(rawName);
                                var currentExt = Path.GetExtension(rawName);

                                // Choose extension: prefer content-type derived extension when provided
                                var finalExt = currentExt;
                                if (string.IsNullOrEmpty(finalExt)) finalExt = extFromContent ?? ".bin";
                                else if (extFromContent != null && !string.Equals(currentExt, extFromContent, StringComparison.OrdinalIgnoreCase))
                                {
                                    // Replace mismatched extension with content-type extension
                                    finalExt = extFromContent;
                                }

                                // sanitize nameOnly
                                nameOnly = SanitizeName(nameOnly);

                                // if nameOnly is still empty, fallback to alt/title or numbering
                                if (string.IsNullOrEmpty(nameOnly))
                                {
                                    var hint = SanitizeName(!string.IsNullOrEmpty(altText) ? altText : titleText);
                                    if (!string.IsNullOrEmpty(hint))
                                    {
                                        fileName = safePageBase + "-" + hint + finalExt;
                                    }
                                    else
                                    {
                                        imageCounters[safePageBase]++;
                                        var idx = imageCounters[safePageBase];
                                        fileName = $"{safePageBase}-image-{idx}" + finalExt;
                                    }
                                }
                                else
                                {
                                    // Prepend page base name for context, but avoid duplication
                                    if (nameOnly.StartsWith(safePageBase + "-", StringComparison.OrdinalIgnoreCase) || nameOnly.Equals(safePageBase, StringComparison.OrdinalIgnoreCase))
                                    {
                                        fileName = nameOnly + finalExt;
                                    }
                                    else
                                    {
                                        fileName = safePageBase + "-" + nameOnly + finalExt;
                                    }
                                }
                            }

                            // sanitize filename to remove any remaining invalid chars and shorten
                            fileName = SanitizeName(fileName);

                            var unique = MakeUniqueFile(Path.Combine(markdownFolder, "images", fileName));
                            await File.WriteAllBytesAsync(unique, imgBytes, cancellationToken);

                            var rel = Path.GetRelativePath(markdownFolder, unique).Replace('\\','/');

                            // store mapping so we don't save duplicates later
                            imageMap[absolute] = rel;

                            _log.Add($"Saved image: {imgUri} -> {unique}");
                            progress?.Report($"Saved image: {imgUri}");
                            _logger.LogInformation("Saved image {Img} -> {File}", imgUri, unique);
                            return rel;
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            _log.Add($"Failed to save image {imgUri}: {ex.GetType().Name} {ex.Message}");
                            progress?.Report($"Failed to save image {imgUri}: {ex.Message}");
                            _logger.LogWarning(ex, "Failed to save image {ImgUri}", imgUri);
                            return null;
                        }
                    }

                    // Helper to map a found saved relative path to the BaseUrl (local function uses outer basePrefix)
                    string ToMappedUrl(string rel) => MapSavedImageToUrl(rel, basePrefix);

                    // Process src
                    if (!string.IsNullOrEmpty(src))
                    {
                        var newSrc = await SaveImageUrlAsync(src);
                        if (!string.IsNullOrEmpty(newSrc))
                        {
                            img.SetAttributeValue("src", ToMappedUrl(newSrc));
                        }
                    }

                    // Process data-src (lazy-loaded images)
                    if (!string.IsNullOrEmpty(dataSrc))
                    {
                        var newData = await SaveImageUrlAsync(dataSrc);
                        if (!string.IsNullOrEmpty(newData))
                        {
                            img.SetAttributeValue("data-src", ToMappedUrl(newData));
                            // also set src if empty
                            if (string.IsNullOrEmpty(img.GetAttributeValue("src", "")))
                            {
                                img.SetAttributeValue("src", ToMappedUrl(newData));
                            }
                        }
                    }

                    // Process srcset
                    if (!string.IsNullOrEmpty(srcset))
                    {
                        // srcset entries: "url 1x, url2 2x" etc.
                        var parts = srcset.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
                        var rewritten = new List<string>();
                        foreach (var part in parts)
                        {
                            // split by whitespace to separate url and descriptor
                            var spaceIdx = part.IndexOf(' ');
                            string urlPart = spaceIdx > 0 ? part.Substring(0, spaceIdx) : part;
                            var descriptor = spaceIdx > 0 ? part.Substring(spaceIdx + 1) : "";
                            var newUrl = await SaveImageUrlAsync(urlPart);
                            if (!string.IsNullOrEmpty(newUrl))
                            {
                                var mapped = ToMappedUrl(newUrl);
                                rewritten.Add(string.IsNullOrEmpty(descriptor) ? mapped : (mapped + " " + descriptor));
                            }
                        }

                        if (rewritten.Count > 0)
                        {
                            img.SetAttributeValue("srcset", string.Join(", ", rewritten));
                            // if src was empty, set first src
                            if (string.IsNullOrEmpty(img.GetAttributeValue("src", "")))
                            {
                                var first = rewritten.First().Split(' ')[0];
                                img.SetAttributeValue("src", first);
                            }
                        }
                    }
                }

                // Remove duplicate image occurrences across the document.
                // Build a list of image occurrences (plain <img> and <a> wrapping an <img>) in document order,
                // then for each set of occurrences with the same rewritten src keep only one.
                var bodyNode = doc.DocumentNode.SelectSingleNode("//body") ?? doc.DocumentNode;
                var occurrences = new List<(HtmlNode containerNode, HtmlNode imgNode, bool wrapped, string src)>();
                var handledImgs = new HashSet<HtmlNode>();

                foreach (var node in bodyNode.Descendants())
                {
                    if (node.NodeType != HtmlNodeType.Element) continue;

                    if (string.Equals(node.Name, "a", StringComparison.OrdinalIgnoreCase))
                    {
                        var innerImg = node.Descendants("img").FirstOrDefault();
                        if (innerImg != null)
                        {
                            var src = innerImg.GetAttributeValue("src", "").Trim();
                            occurrences.Add((node, innerImg, true, src));
                            handledImgs.Add(innerImg);
                        }
                    }
                    else if (string.Equals(node.Name, "img", StringComparison.OrdinalIgnoreCase))
                    {
                        if (handledImgs.Contains(node)) continue; // already accounted for as part of an anchor
                        var src = node.GetAttributeValue("src", "").Trim();
                        occurrences.Add((node, node, false, src));
                    }
                }

                var groups = occurrences.Where(o => !string.IsNullOrEmpty(o.src)).GroupBy(o => o.src, StringComparer.OrdinalIgnoreCase);
                foreach (var g in groups)
                {
                    var list = g.ToList();
                    if (list.Count <= 1) continue;

                    // Prefer the first plain <img> occurrence; otherwise keep the first occurrence.
                    int keepIndex = list.FindIndex(x => !x.wrapped);
                    if (keepIndex == -1) keepIndex = 0;
                    var keep = list[keepIndex];

                    for (int i = 0; i < list.Count; i++)
                    {
                        if (i == keepIndex) continue;
                        var occ = list[i];
                        try { occ.containerNode.Remove(); } catch (Exception ex) { _logger.LogDebug(ex, "Failed to remove duplicate image occurrence"); }
                    }
                }

                // Find links to other pages within same base URL
                var anchors = doc.DocumentNode.SelectNodes("//a[@href]") ?? Enumerable.Empty<HtmlNode>();
                foreach (var a in anchors.ToList())
                {
                    var href = a.GetAttributeValue("href", "");
                    if (string.IsNullOrEmpty(href)) continue;
                    if (href.StartsWith("#")) continue;

                    // If the anchor points to a data URI image and we have it mapped, rewrite to saved image
                    if (href.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    {
                        if (imageMap.TryGetValue(href, out var mappedRel))
                        {
                            a.SetAttributeValue("href", MapSavedImageToUrl(mappedRel, basePrefix));
                            continue;
                        }
                        else
                        {
                            // no mapping for data URI image, remove the anchor
                            try { a.Remove(); } catch (Exception ex) { _logger.LogDebug(ex, "Failed to remove anchor referencing data URI"); }
                            continue;
                        }
                    }

                    if (!Uri.TryCreate(uri, href, out var link)) continue;

                    // If anchor points to an image URL we saved (even if external), rewrite to saved image
                    var linkAbs = link.AbsoluteUri;
                    if (imageMap.TryGetValue(linkAbs, out var imgRel))
                    {
                        a.SetAttributeValue("href", MapSavedImageToUrl(imgRel, basePrefix));
                        continue;
                    }

                    // Only allow non-image links that are under the configured BaseUrl
                    var linkAbsTrim = linkAbs.TrimEnd('/');
                    if (!(string.Equals(linkAbsTrim, basePrefix, StringComparison.OrdinalIgnoreCase) ||
                          linkAbsTrim.StartsWith(basePrefix + "/", StringComparison.OrdinalIgnoreCase)))
                    {
                        // remove external link and its label (do not include)
                        try { a.Remove(); } catch (Exception ex) { _logger.LogDebug(ex, "Failed to remove external anchor"); }
                        continue;
                    }

                    // At this point the link is accepted as internal under BaseUrl
                    if (!_visited.Contains(link.AbsoluteUri) && _visited.Count < config.MaxPages)
                    {
                        _visited.Add(link.AbsoluteUri);
                        toVisit.Enqueue(link);
                        _logger.LogDebug("Enqueued linked page {Link}", link.AbsoluteUri);
                    }

                    // rewrite links to point to markdown files under configured BaseUrl
                    var mdName = MakeFileNameForUri(link);
                    // Use relative link (just the markdown filename)
                    a.SetAttributeValue("href", mdName.Replace('\\','/'));
                }

                // Convert to markdown using ReverseMarkdown
                var body = doc.DocumentNode.SelectSingleNode("//body") ?? doc.DocumentNode;
                var htmlFragment = body.InnerHtml;
                string markdown = "";
                try
                {
                    markdown = converter.Convert(htmlFragment);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ReverseMarkdown converter failed for {Uri}", uri);
                }

                // If conversion produced no output or looks like raw HTML (converter failed or returned HTML), fall back to a simple converter
                if (string.IsNullOrWhiteSpace(markdown) || Regex.IsMatch(markdown, "<\\/?[a-zA-Z]"))
                {
                    _log.Add("ReverseMarkdown output appears invalid or empty. Using fallback converter.");
                    progress?.Report("Using fallback markdown converter");
                    _logger.LogDebug("Using fallback converter for {Uri}", uri);
                    try
                    {
                        var fallback = ConvertHtmlToMarkdown(htmlFragment);
                        if (!string.IsNullOrWhiteSpace(fallback))
                        {
                            markdown = fallback;
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Add($"Fallback converter failed: {ex.Message}");
                        _logger.LogWarning(ex, "Fallback converter failed for {Uri}", uri);
                    }
                }

                // Post-process markdown to remove duplicate adjacent image entries that refer to the same src.
                // run replacements until stable.
                if (!string.IsNullOrWhiteSpace(markdown))
                {
                    var changed = true;
                    while (changed)
                    {
                        var before = markdown;

                        // plain then linked -> keep plain
                        markdown = Regex.Replace(markdown,
                            @"(?<first>!\[[^\]]*\]\((?<src>[^)]+)\))\s*(?<second>\[!\[[^\]]*\]\(\k<src>\)\]\([^)]+\))",
                            "${first}", RegexOptions.IgnoreCase);

                        // linked then plain -> keep plain
                        markdown = Regex.Replace(markdown,
                            @"(?<first>\[!\[[^\]]*\]\((?<src>[^)]+)\)\]\([^)]+\))\s*(?<second>!\[[^\]]*\]\(\k<src>\))",
                            "${second}", RegexOptions.IgnoreCase);

                        // plain then plain -> keep first
                        markdown = Regex.Replace(markdown,
                            @"(?<first>!\[[^\]]*\]\((?<src>[^)]+)\))\s*(?<second>!\[[^\]]*\]\(\k<src>\))",
                            "${first}", RegexOptions.IgnoreCase);

                        // linked then linked -> keep first
                        markdown = Regex.Replace(markdown,
                            @"(?<first>\[!\[[^\]]*\]\((?<src>[^)]+)\)\]\([^)]+\))\s*(?<second>\[!\[[^\]]*\]\(\k<src>\)\]\([^)]+\))",
                            "${first}", RegexOptions.IgnoreCase);

                        changed = !string.Equals(before, markdown, StringComparison.Ordinal);
                    }
                }

                var fileNameForPage = MakeUniqueFile(Path.Combine(markdownFolder, MakeFileNameForUri(uri)));
                await File.WriteAllTextAsync(fileNameForPage, markdown, cancellationToken);
                _log.Add($"Saved page: {uri} -> {fileNameForPage}");
                progress?.Report($"Saved page: {uri}");
                _logger.LogInformation("Saved page {Uri} -> {File}", uri, fileNameForPage);
            }
            catch (OperationCanceledException)
            {
                _log.Add("Operation cancelled");
                _logger.LogInformation("Operation cancelled while processing pages");
                return new WebGrabResult(false, "Cancelled", _log);
            }
            catch (Exception ex)
            {
                _log.Add($"Error visiting {uri}: {ex.Message}");
                progress?.Report($"Error visiting {uri}: {ex.Message}");
                _logger.LogError(ex, "Error visiting {Uri}", uri);
            }
        }

        _logger.LogInformation("Grab completed. Pages processed: {Count}", _visited.Count);
        return new WebGrabResult(true, "Completed", _log);
    }

    private static string MakeFileNameForUri(Uri uri)
    {
        var path = uri.AbsolutePath.Trim('/');
        if (string.IsNullOrEmpty(path)) path = "index";
        path = Regex.Replace(path, "[^a-zA-Z0-9-_]", "-");
        return path + ".md";
    }

    private static string MakeUniqueFile(string path)
    {
        var dir = Path.GetDirectoryName(path) ?? ".";
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        var candidate = Path.Combine(dir, name + ext);
        int i = 1;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(dir, name + "-" + i + ext);
            i++;
        }
        return candidate;
    }

    // Minimal fallback HTML -> Markdown converter for cases where ReverseMarkdown didn't produce markdown
    private static string ConvertHtmlToMarkdown(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var sb = new StringBuilder();

        void ProcessNode(HtmlNode node, int listLevel = 0)
        {
            if (node.NodeType == HtmlNodeType.Text)
            {
                var txt = HtmlEntity.DeEntitize(node.InnerText);
                if (!string.IsNullOrWhiteSpace(txt))
                {
                    sb.Append(txt);
                }
                return;
            }

            switch (node.Name.ToLowerInvariant())
            {
                case "h1":
                case "h2":
                case "h3":
                case "h4":
                case "h5":
                case "h6":
                    var level = int.Parse(node.Name.Substring(1));
                    sb.AppendLine();
                    sb.Append(new string('#', level)).Append(' ');
                    foreach (var c in node.ChildNodes) ProcessNode(c, listLevel);
                    sb.AppendLine().AppendLine();
                    break;
                case "p":
                    sb.AppendLine();
                    foreach (var c in node.ChildNodes) ProcessNode(c, listLevel);
                    sb.AppendLine().AppendLine();
                    break;
                case "br":
                    sb.AppendLine();
                    break;
                case "ul":
                    foreach (var li in node.SelectNodes("li") ?? Enumerable.Empty<HtmlNode>())
                    {
                        sb.Append(new string(' ', listLevel * 2)).Append("- ");
                        foreach (var c in li.ChildNodes) ProcessNode(c, listLevel + 1);
                        sb.AppendLine();
                    }
                    sb.AppendLine();
                    break;
                case "ol":
                    int idx = 1;
                    foreach (var li in node.SelectNodes("li") ?? Enumerable.Empty<HtmlNode>())
                    {
                        sb.Append(new string(' ', listLevel * 2)).Append(idx.ToString()).Append(". ");
                        foreach (var c in li.ChildNodes) ProcessNode(c, listLevel + 1);
                        sb.AppendLine();
                        idx++;
                    }
                    sb.AppendLine();
                    break;
                case "a":
                    var href = node.GetAttributeValue("href", string.Empty);
                    var inner = new StringBuilder();
                    foreach (var c in node.ChildNodes) { var before = sb.Length; ProcessNode(c, listLevel); var after = sb.Length; inner.Append(sb.ToString().Substring(before, after - before)); }
                    var text = inner.ToString().Trim();
                    if (string.IsNullOrEmpty(text)) text = href;
                    sb.Append('[').Append(text).Append(']').Append('(').Append(href).Append(')');
                    break;
                case "img":
                    var src = node.GetAttributeValue("src", string.Empty);
                    var alt = node.GetAttributeValue("alt", string.Empty);
                    sb.Append("![").Append(alt).Append("](").Append(src).Append(")");
                    break;
                case "strong":
                case "b":
                    sb.Append("**");
                    foreach (var c in node.ChildNodes) ProcessNode(c, listLevel);
                    sb.Append("**");
                    break;
                case "em":
                case "i":
                    sb.Append("*");
                    foreach (var c in node.ChildNodes) ProcessNode(c, listLevel);
                    sb.Append("*");
                    break;
                case "pre":
                    sb.AppendLine().AppendLine("```");
                    var codeText = node.InnerText;
                    sb.AppendLine(codeText.TrimEnd()).AppendLine("```").AppendLine();
                    break;
                case "code":
                    sb.Append('`').Append(node.InnerText).Append('`');
                    break;
                default:
                    foreach (var c in node.ChildNodes) ProcessNode(c, listLevel);
                    break;
            }
        }

        foreach (var n in doc.DocumentNode.ChildNodes)
        {
            ProcessNode(n);
        }

        var result = sb.ToString();
        // normalize whitespace
        result = Regex.Replace(result, "\r\n{3,}", "\r\n\r\n");
        return result.Trim();
    }
}
