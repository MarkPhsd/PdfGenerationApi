# IIS Hosting Setup — PdfGenerationApi

## Step 1 — Install the .NET 8 Hosting Bundle (server only, once)

Download and run on the server:
https://dotnet.microsoft.com/en-us/download/dotnet/8.0
→ "ASP.NET Core Runtime 8.x" → **Hosting Bundle** (not the SDK)

After installing, run in an elevated command prompt:
```
iisreset
```

---

## Step 2 — Publish the app

From the project folder on your dev machine:

```
dotnet publish -c Release -r win-x64 --no-self-contained -o C:\pointlessHosting\PdfGenerationApi
```

This outputs everything IIS needs into `C:\pointlessHosting\PdfGenerationApi`.

**Important:** Copy `libwkhtmltox.dll` into that publish folder if it didn't copy automatically.
Check the publish output — it should be there alongside `PdfGenerationApi.dll`.

---

## Step 3 — Create a logs folder

```
mkdir C:\pointlessHosting\PdfGenerationApi\logs
```

IIS needs this for stdout logging (configured in web.config).

---

## Step 4 — Create a PDF output folder

```
mkdir C:\PdfOutput
```

Or whatever path you set in `appsettings.json` → `PdfOutputPath`.

---

## Step 5 — Create the IIS Site

1. Open **IIS Manager**
2. Right-click **Sites** → **Add Website**
3. Fill in:
   - **Site name:** `PdfGenerationApi`
   - **Physical path:** `C:\pointlessHosting\PdfGenerationApi`
   - **Binding → Port:** `5100`  (or any free port — match your `web.config` `PdfApiUrl`)
   - **Host name:** leave blank (localhost only)
4. Click **OK**

---

## Step 6 — Configure the Application Pool

1. In IIS Manager → **Application Pools** → find `PdfGenerationApi`
2. Click **Basic Settings**:
   - **.NET CLR Version:** `No Managed Code`  ← required for .NET 8
3. Click **Advanced Settings**:
   - **Identity:** `ApplicationPoolIdentity` (default is fine)

---

## Step 7 — Set folder permissions

The app pool identity (`IIS AppPool\PdfGenerationApi`) needs:

| Folder | Permission |
|--------|-----------|
| `C:\pointlessHosting\PdfGenerationApi` | Read & Execute |
| `C:\PdfOutput` | **Modify** (so it can write PDFs) |
| `C:\pointlessHosting\PdfGenerationApi\logs` | Modify |

To set permissions (run as admin or via Explorer → Properties → Security):
```
icacls "C:\PdfOutput" /grant "IIS AppPool\PdfGenerationApi:(OI)(CI)M"
icacls "C:\pointlessHosting\PdfGenerationApi\logs" /grant "IIS AppPool\PdfGenerationApi:(OI)(CI)M"
```

---

## Step 8 — Test

Browse to:
```
http://localhost:5100/api/pdf/health
```

Expected response:
```json
{ "status": "ok", "utc": "..." }
```

---

## Step 9 — Update web.config in your VB.NET app

Add to `<appSettings>` in your VB.NET app's `web.config`:
```xml
<add key="PdfApiUrl" value="http://localhost:5100/api/pdf/generate" />
```

---

## Troubleshooting

- **502.5 / 500.30** — .NET 8 Hosting Bundle not installed, or `iisreset` not run after install
- **libwkhtmltox.dll missing** — copy it manually to the publish output folder
- **Access denied writing PDF** — app pool identity lacks Modify on `C:\PdfOutput`
- **Check logs** — `C:\pointlessHosting\PdfGenerationApi\logs\stdout_*.log`
