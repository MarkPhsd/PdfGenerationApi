using Microsoft.AspNetCore.Mvc;
using PdfGenerationApi.Models;
using PdfGenerationApi.Services;

namespace PdfGenerationApi.Controllers;

[ApiController]
[Route("api/pdf")]
public class PdfController : ControllerBase
{
    private readonly PdfService _pdfService;
    private readonly ILogger<PdfController> _logger;

    public PdfController(PdfService pdfService, ILogger<PdfController> logger)
    {
        _pdfService = pdfService;
        _logger     = logger;
    }

    /// <summary>
    /// POST api/pdf/generate
    /// Accepts HTML content, converts it to a PDF, saves it to disk, and returns
    /// the file path + UUID so the caller can attach it to an email.
    /// </summary>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(PdfResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Generate([FromBody] PdfRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.HtmlContent))
            return BadRequest(new { error = "HtmlContent is required." });

        try
        {
            var result = await _pdfService.GenerateAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF generation failed");
            return StatusCode(500, new { error = "PDF generation failed.", detail = ex.Message });
        }
    }

    /// <summary>
    /// GET api/pdf/health
    /// Quick health check so the VB.NET app can verify the service is reachable.
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok", utc = DateTime.UtcNow });
}
