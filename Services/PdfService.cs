using System.Text.RegularExpressions;
using DinkToPdf;
using DinkToPdf.Contracts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using PdfGenerationApi.Models;

namespace PdfGenerationApi.Services;

public class PdfService
{
    private readonly IConverter _converter;
    private readonly string _outputPath;
    private readonly ILogger<PdfService> _logger;
    private static readonly HttpClient _imageClient = new();

    public PdfService(IConverter converter, IConfiguration config, ILogger<PdfService> logger)
    {
        _converter = converter;
        _logger    = logger;

        _outputPath = config["PdfOutputPath"]
            ?? throw new InvalidOperationException("PdfOutputPath is not configured in appsettings.json.");

        if (!Directory.Exists(_outputPath))
        {
            Directory.CreateDirectory(_outputPath);
            _logger.LogInformation("Created PDF output directory: {Path}", _outputPath);
        }
    }

    public async Task<PdfResponse> GenerateAsync(PdfRequest request)
    {
        var fileId   = Guid.NewGuid().ToString();
        var baseName = string.IsNullOrWhiteSpace(request.FileNameOverride)
                           ? fileId
                           : SanitizeFileName(request.FileNameOverride);
        var fileName = $"{baseName}.pdf";
        var filePath = Path.Combine(_outputPath, fileName);

        var orientation = request.Orientation?.ToLower() == "landscape"
                              ? Orientation.Landscape
                              : Orientation.Portrait;

        var paperSize = ParsePaperSize(request.PaperSize);

        // Strip interactive elements, then embed remote images as base64
        var sanitizedHtml = SanitizeForPdf(request.HtmlContent);
        var embeddedHtml  = await EmbedRemoteImagesAsync(sanitizedHtml);

        var doc = new HtmlToPdfDocument
        {
            GlobalSettings =
            {
                ColorMode   = ColorMode.Color,
                Orientation = orientation,
                PaperSize   = paperSize,
                Out         = filePath,
                DPI         = 300
            },
            Objects =
            {
                new ObjectSettings
                {
                    HtmlContent = embeddedHtml,
                    WebSettings =
                    {
                        DefaultEncoding            = "utf-8",
                        EnableIntelligentShrinking = true,
                        LoadImages                 = true,
                        EnableJavascript           = false,
                        PrintMediaType             = false
                    },
                    LoadSettings =
                    {
                        LoadErrorHandling = ContentErrorHandling.Ignore
                    },
                    HeaderSettings = { FontSize = 0 },
                    FooterSettings = { FontSize = 0 }
                }
            }
        };

        _logger.LogInformation("Generating PDF: {FileName}", fileName);
        _converter.Convert(doc);
        _logger.LogInformation("PDF saved: {FilePath}", filePath);

        return new PdfResponse
        {
            FileId    = fileId,
            FileName  = fileName,
            FilePath  = filePath,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ── Image Embedding ───────────────────────────────────────────────────────

    /// <summary>
    /// Finds every src="https://..." attribute in the HTML, downloads the image,
    /// converts it to JPEG (handles WebP, PNG, GIF etc.), and replaces the src
    /// with an inline base64 data URI. wkhtmltopdf then never needs to fetch anything.
    /// </summary>
    private async Task<string> EmbedRemoteImagesAsync(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return html;

        // Match src="https://..." or src='https://...' or src=https://...
        var srcRegex = new Regex(
            @"src\s*=\s*['""]?(https?://[^\s'"">\]]+)['""]?",
            RegexOptions.IgnoreCase);

        var matches = srcRegex.Matches(html);

        // Build a unique URL → data URI map (avoid downloading the same image twice)
        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in matches)
        {
            var url = match.Groups[1].Value;
            if (replacements.ContainsKey(url)) continue;

            try
            {
                var bytes = await _imageClient.GetByteArrayAsync(url);

                using var image = Image.Load(bytes);
                using var ms    = new MemoryStream();
                await image.SaveAsJpegAsync(ms, new JpegEncoder { Quality = 85 });

                var base64  = Convert.ToBase64String(ms.ToArray());
                var dataUri = $"data:image/jpeg;base64,{base64}";

                replacements[url] = dataUri;
                _logger.LogInformation("Embedded image: {Url}", url);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not embed image {Url}: {Message}", url, ex.Message);
                // Leave the original src — wkhtmltopdf will skip it gracefully
            }
        }

        // Replace each matched src="<url>" with src="<dataUri>"
        html = srcRegex.Replace(html, match =>
        {
            var url = match.Groups[1].Value;
            return replacements.TryGetValue(url, out var dataUri)
                ? $"src=\"{dataUri}\""
                : match.Value;
        });

        return html;
    }

    // ── HTML Sanitisation ─────────────────────────────────────────────────────

    private static string SanitizeForPdf(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return html;

        // Remove <script> blocks entirely
        html = Regex.Replace(html, @"<script[\s\S]*?</script>", string.Empty, RegexOptions.IgnoreCase);

        // Remove <button> elements entirely
        html = Regex.Replace(html, @"<button[\s\S]*?</button>", string.Empty, RegexOptions.IgnoreCase);

        // Remove form controls
        html = Regex.Replace(html, @"<input[^>]*>",             string.Empty, RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<select[\s\S]*?</select>", string.Empty, RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<textarea[\s\S]*?</textarea>", string.Empty, RegexOptions.IgnoreCase);

        // Strip <form> tags, keep inner content
        html = Regex.Replace(html, @"</?form[^>]*>", string.Empty, RegexOptions.IgnoreCase);

        // Remove <a> tags styled as buttons (background-color + padding = button heuristic)
        html = Regex.Replace(
            html,
            @"<a\s[^>]*style\s*=\s*[""'][^""']*background-color[^""']*padding[^""']*[""'][^>]*>[\s\S]*?</a>",
            string.Empty,
            RegexOptions.IgnoreCase);

        // Strip remaining <a> tags, keep visible text
        html = Regex.Replace(html, @"<a\s[^>]*>([\s\S]*?)</a>", "$1", RegexOptions.IgnoreCase);

        return html;
    }

    // ── Misc Helpers ──────────────────────────────────────────────────────────

    private static PaperKind ParsePaperSize(string? size) =>
        size?.ToUpperInvariant() switch
        {
            "LETTER" => PaperKind.Letter,
            "LEGAL"  => PaperKind.Legal,
            "A3"     => PaperKind.A3,
            "A5"     => PaperKind.A5,
            _        => PaperKind.A4
        };

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
    }
}
