using HtmlAgilityPack;
using ReverseMarkdown;
using System.Text;
using System.Text.RegularExpressions;

namespace WebGrabber.Services;

public class WebGrabService : IWebGrabService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly List<string> _log = new();
    private readonly HashSet<string> _visited = new(StringComparer.OrdinalIgnoreCase);

    public WebGrabService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async System.Threading.Tasks.Task<WebGrabResult> GrabSiteAsync(WebGrabConfig config, IProgress<string>? progress = null, System.Threading.CancellationToken cancellationToken = default)
    {
        _log.Clear();
        _visited.Clear();

        if (!Uri.TryCreate(config.StartUrl, UriKind.Absolute, out var startUri))
        {
            return new WebGrabResult(false, "Invalid start URL", _log);
        }

        // Ensure markdown folder is relative to app if not absolute
        var markdownFolder = config.MarkdownFolder;
        if (!Path.IsPathRooted(markdownFolder))
        {
            markdownFolder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, markdownFolder));
        }

        Directory.CreateDirectory(markdownFolder);
        Directory.CreateDirectory(Path.Combine(markdownFolder, "images"));

        var toVisit = new Queue<Uri>();
        toVisit.Enqueue(startUri);
        _visited.Add(startUri.AbsoluteUri);

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(config.UserAgent);

        var converter = new Converter(new Config());

        // Counters to number images per page (keyed by page file name)
        var imageCounters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        while (toVisit.Count > 0 && _visited.Count <= config.MaxPages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var uri = toVisit.Dequeue();
            _log.Add($"Visiting: {uri}");
            progress?.Report($"Visiting: {uri}");

            try
            {
                var resp = await client.GetAsync(uri, cancellationToken);
                if (!resp.IsSuccessStatusCode)
                {
                    _log.Add($"Failed to get {uri}: {resp.StatusCode}");
                    progress?.Report($"Failed to get {resp.StatusCode}");
                    continue;
                }

                var html = await resp.Content.ReadAsStringAsync(cancellationToken);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Remove script and style elements to avoid saving JavaScript into markdown
                var scriptsAndStyles = doc.DocumentNode.SelectNodes("//script|//style") ?? Enumerable.Empty<HtmlNode>();
                foreach (var node in scriptsAndStyles.ToList())
                {
                    try { node.Remove(); } catch { }
                }

                // Remove inline event handler attributes (onclick, onmouseover, etc.) and javascript: hrefs
                var allNodes = doc.DocumentNode.SelectNodes("//*") ?? Enumerable.Empty<HtmlNode>();
                foreach (var node in allNodes)
                {
                    var attrsToRemove = node.Attributes.Where(a => a.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase)).Select(a => a.Name).ToList();
                    foreach (var name in attrsToRemove)
                    {
                        try { node.Attributes.Remove(name); } catch { }
                    }

                    var hrefAttr = node.Attributes["href"];
                    if (hrefAttr != null && !string.IsNullOrWhiteSpace(hrefAttr.Value) && hrefAttr.Value.TrimStart().StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                    {
                        try { node.Attributes.Remove("href"); } catch { }
                    }

                    var srcAttr = node.Attributes["src"];
                    if (srcAttr != null && !string.IsNullOrWhiteSpace(srcAttr.Value) && srcAttr.Value.TrimStart().StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                    {
                        try { node.Attributes.Remove("src"); } catch { }
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

                        // Handle data URIs (data:image/png;base64,...)
                        if (imgUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                var comma = imgUrl.IndexOf(',');
                                if (comma < 0) return null;
                                var meta = imgUrl.Substring(5, comma - 5); // skip "data:"
                                var isBase64 = meta.EndsWith(";base64", StringComparison.OrdinalIgnoreCase);
                                // extract mime type if present (e.g. image/png)
                                var mime = meta.Split(';').FirstOrDefault() ?? string.Empty;

                                // map mime to extension
                                string? extFromMeta = mime.ToLowerInvariant() switch
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

                                var payload = imgUrl.Substring(comma + 1);
                                byte[] imgBytes = isBase64 ? Convert.FromBase64String(payload) : Encoding.UTF8.GetBytes(Uri.UnescapeDataString(payload));

                                // Generate a meaningful filename: pageBaseName-image-N.ext
                                imageCounters[pageBaseName]++;
                                var idx = imageCounters[pageBaseName];
                                var fileName = $"{pageBaseName}-image-{idx}" + (extFromMeta ?? ".bin");

                                // sanitize filename to remove invalid chars
                                fileName = SanitizeName(fileName);

                                var unique = MakeUniqueFile(Path.Combine(markdownFolder, "images", fileName));
                                await File.WriteAllBytesAsync(unique, imgBytes, cancellationToken);
                                var rel = Path.GetRelativePath(markdownFolder, unique).Replace('\\','/');
                                _log.Add($"Saved data-uri image -> {unique}");
                                progress?.Report($"Saved data-uri image");
                                return rel;
                            }
                            catch (Exception ex)
                            {
                                _log.Add($"Failed to save data-uri image: {ex.GetType().Name} {ex.Message}");
                                progress?.Report($"Failed to save data-uri image: {ex.Message}");
                                return null;
                            }
                        }

                        // Fix protocol-relative URLs (//example.com/foo)
                        if (imgUrl.StartsWith("//"))
                        {
                            imgUrl = startUri.Scheme + ":" + imgUrl;
                        }

                        if (!Uri.TryCreate(uri, imgUrl, out var imgUri))
                        {
                            _log.Add($"Invalid image URL: {imgUrl}");
                            return null;
                        }

                        try
                        {
                            using var req = new HttpRequestMessage(HttpMethod.Get, imgUri);
                            // set Referer to the page uri to help servers that require it
                            req.Headers.Referrer = uri;
                            req.Headers.Accept.ParseAdd("*/*");

                            using var response = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                            if (!response.IsSuccessStatusCode)
                            {
                                _log.Add($"Failed to download image {imgUri}: {response.StatusCode} {response.ReasonPhrase}");
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
                            _log.Add($"Saved image: {imgUri} -> {unique}");
                            progress?.Report($"Saved image: {imgUri}");
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
                            return null;
                        }
                    }

                    // Process src
                    if (!string.IsNullOrEmpty(src))
                    {
                        var newSrc = await SaveImageUrlAsync(src);
                        if (!string.IsNullOrEmpty(newSrc))
                        {
                            img.SetAttributeValue("src", newSrc);
                        }
                    }

                    // Process data-src (lazy-loaded images)
                    if (!string.IsNullOrEmpty(dataSrc))
                    {
                        var newData = await SaveImageUrlAsync(dataSrc);
                        if (!string.IsNullOrEmpty(newData))
                        {
                            img.SetAttributeValue("data-src", newData);
                            // also set src if empty
                            if (string.IsNullOrEmpty(img.GetAttributeValue("src", "")))
                            {
                                img.SetAttributeValue("src", newData);
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
                                rewritten.Add(string.IsNullOrEmpty(descriptor) ? newUrl : (newUrl + " " + descriptor));
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

                // Find links to other pages within same domain
                var anchors = doc.DocumentNode.SelectNodes("//a[@href]") ?? Enumerable.Empty<HtmlNode>();
                foreach (var a in anchors)
                {
                    var href = a.GetAttributeValue("href", "");
                    if (string.IsNullOrEmpty(href)) continue;
                    if (href.StartsWith("#")) continue;

                    if (!Uri.TryCreate(uri, href, out var link)) continue;

                    // restrict to same host
                    if (link.Host == startUri.Host)
                    {
                        if (!_visited.Contains(link.AbsoluteUri) && _visited.Count < config.MaxPages)
                        {
                            _visited.Add(link.AbsoluteUri);
                            toVisit.Enqueue(link);
                        }
                        // rewrite links to point to markdown files
                        var mdName = MakeFileNameForUri(link);
                        a.SetAttributeValue("href", Path.Combine(config.BaseUrl.TrimEnd('/'), mdName).Replace('\\','/'));
                    }
                }

                // Convert to markdown using ReverseMarkdown
                var body = doc.DocumentNode.SelectSingleNode("//body") ?? doc.DocumentNode;
                var htmlFragment = body.InnerHtml;
                var markdown = converter.Convert(htmlFragment);

                // If conversion produced no output or looks like raw HTML (converter failed or returned HTML), fall back to a simple converter
                if (string.IsNullOrWhiteSpace(markdown) || Regex.IsMatch(markdown, "<\\/?[a-zA-Z]"))
                {
                    _log.Add("ReverseMarkdown output appears invalid or empty. Using fallback converter.");
                    progress?.Report("Using fallback markdown converter");
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
                    }
                }

                var fileNameForPage = MakeUniqueFile(Path.Combine(markdownFolder, MakeFileNameForUri(uri)));
                await File.WriteAllTextAsync(fileNameForPage, markdown, cancellationToken);
                _log.Add($"Saved page: {uri} -> {fileNameForPage}");
                progress?.Report($"Saved page: {uri}");
            }
            catch (OperationCanceledException)
            {
                _log.Add("Operation cancelled");
                return new WebGrabResult(false, "Cancelled", _log);
            }
            catch (Exception ex)
            {
                _log.Add($"Error visiting {uri}: {ex.Message}");
                progress?.Report($"Error visiting {uri}: {ex.Message}");
            }
        }

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
