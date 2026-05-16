namespace PdfGenerationApi.Models;

public class PdfRequest
{
    /// <summary>
    /// The full HTML content to convert to PDF.
    /// Include all inline styles or <style> blocks — external CSS is not fetched.
    /// </summary>
    public string HtmlContent { get; set; } = string.Empty;

    /// <summary>
    /// Optional: override the output filename (without extension).
    /// If omitted, a UUID is used.
    /// </summary>
    public string? FileNameOverride { get; set; }

    /// <summary>
    /// Optional page orientation. "Portrait" (default) or "Landscape".
    /// </summary>
    public string Orientation { get; set; } = "Portrait";

    /// <summary>
    /// Optional paper size. Defaults to "A4". Accepts: A4, Letter, Legal, etc.
    /// </summary>
    public string PaperSize { get; set; } = "A4";
}
