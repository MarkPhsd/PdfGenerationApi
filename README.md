# PdfGenerationApi

A lightweight C# .NET 8 Web API that converts HTML to PDF using DinkToPdf (wkhtmltopdf wrapper).

## Flow

```
VB.NET / EF App
  └─► POST /api/pdf/generate  { htmlContent, orientation?, paperSize? }
           ▼
  C# PDF API
    • Converts HTML → PDF via DinkToPdf
    • Saves to disk:  C:\PdfOutput\<uuid>.pdf
    • Returns:        { fileId, fileName, filePath, createdAt }
           ▼
  VB.NET / EF App
    • Saves fileId + filePath to DB (EF)
    • Attaches filePath to outgoing email
```

## Setup

### 1. Install the NuGet package
```
dotnet add package DinkToPdf --version 1.0.8
```

### 2. Download the native wkhtmltopdf library

DinkToPdf requires the native binary placed **next to your compiled .exe**.

| Platform | File to download | Rename to |
|----------|-----------------|-----------|
| Windows 64-bit | `wkhtmltox-0.12.6-1.msvc2015-win64.exe` (extract DLL) | `libwkhtmltox.dll` |
| Linux 64-bit   | `wkhtmltox_0.12.6-1.bionic_amd64.deb` (extract .so)  | `libwkhtmltox.so`  |

Download from: https://github.com/wkhtmltopdf/packaging/releases/tag/0.12.6-1

Place the file in the **project root** — the .csproj is set to copy it automatically.

### 3. Configure the output path

Edit `appsettings.json`:
```json
"PdfOutputPath": "C:\\PdfOutput"
```

Make sure:
- The API process has **write** permission to this folder.
- The VB.NET app has **read** permission to the same folder.
- On the same machine, this just works. On separate servers, use a UNC share (e.g. `\\server\PdfOutput`).

### 4. Run

```
dotnet run
```

Swagger UI available at: `https://localhost:{port}/swagger`

Health check: `GET /api/pdf/health`

---

## API

### POST /api/pdf/generate

**Request body:**
```json
{
  "htmlContent": "<html><body><h1>Invoice #123</h1></body></html>",
  "orientation": "Portrait",
  "paperSize": "A4",
  "fileNameOverride": null
}
```

**Response:**
```json
{
  "fileId":    "3f2a1b4c-9e87-4d12-a3f1-000000000001",
  "fileName":  "3f2a1b4c-9e87-4d12-a3f1-000000000001.pdf",
  "filePath":  "C:\\PdfOutput\\3f2a1b4c-9e87-4d12-a3f1-000000000001.pdf",
  "createdAt": "2026-05-15T10:30:00Z"
}
```

Use `filePath` directly as your `System.Net.Mail.Attachment(filePath)` argument in VB.NET.

---

## VB.NET caller (quick reference)

```vbnet
Public Class PdfApiResponse
    Public Property FileId As String
    Public Property FileName As String
    Public Property FilePath As String
    Public Property CreatedAt As DateTime
End Class

Public Async Function GeneratePdfAsync(htmlContent As String) As Task(Of PdfApiResponse)
    Dim payload = New With {.htmlContent = htmlContent}
    Dim json = JsonConvert.SerializeObject(payload)
    Dim content = New StringContent(json, Encoding.UTF8, "application/json")

    Dim response = Await _httpClient.PostAsync("http://localhost:5100/api/pdf/generate", content)
    response.EnsureSuccessStatusCode()

    Return JsonConvert.DeserializeObject(Of PdfApiResponse)(
        Await response.Content.ReadAsStringAsync())
End Function

' Attach to email:
' Dim pdfInfo = Await GeneratePdfAsync(myHtml)
' mail.Attachments.Add(New Attachment(pdfInfo.FilePath))
```
