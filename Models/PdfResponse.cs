namespace PdfGenerationApi.Models;

public class PdfResponse
{
    /// <summary>
    /// Unique identifier for this PDF (UUID). Use this as your DB primary key / foreign key.
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// The file name only, e.g. "3f2a1b4c-....pdf"
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The full absolute path on disk where the PDF was saved.
    /// Pass this directly to your email attachment logic in the VB.NET app.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the PDF was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
