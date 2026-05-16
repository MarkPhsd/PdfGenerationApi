using DinkToPdf;
using DinkToPdf.Contracts;
using PdfGenerationApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ─────────────────────────────────────────────────────────────────

builder.Services.AddControllers();

// DinkToPdf — SynchronizedConverter is required (wkhtmltopdf is single-threaded)
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

// Our PDF service
builder.Services.AddSingleton<PdfService>();

// ── App ───────────────────────────────────────────────────────────────────────

var app = builder.Build();

// app.UseHttpsRedirection(); // HTTP only for local use
app.UseAuthorization();
app.MapControllers();

app.Run();
