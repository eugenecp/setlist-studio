# Local Run Instructions (PowerShell)

This file shows quick commands to run Setlist Studio locally on Windows (PowerShell).

## Development run (recommended)
Open PowerShell in the repo root and run:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:SKIP_SECRET_VALIDATION='true'   # skip production-only secret checks for local dev
dotnet run --project "src\SetlistStudio.Web\SetlistStudio.Web.csproj" --configuration Debug
```

This uses the `appsettings.json` (SQLite by default) and seeds development sample data.

## Force known HTTP/HTTPS ports
If you want Kestrel to bind to known ports (useful for testing):

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:SKIP_SECRET_VALIDATION='true'
$env:ASPNETCORE_URLS='http://localhost:5000;https://localhost:5001'
dotnet run --project "src\SetlistStudio.Web\SetlistStudio.Web.csproj" --configuration Debug
```

Notes:
- `SKIP_SECRET_VALIDATION` is for local development only. Do NOT set this in production.
- For HTTPS you may need to trust the dev certificate (or provide a valid cert).

## Background run (PowerShell)
To start the app without blocking the terminal:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'; $env:SKIP_SECRET_VALIDATION='true'; Start-Process -NoNewWindow -FilePath 'dotnet' -ArgumentList 'run','--project','src\SetlistStudio.Web\SetlistStudio.Web.csproj','--configuration','Debug'
```

## Production notes
- Provide real secrets for OAuth and a valid `ConnectionStrings:DefaultConnection`.
- The production settings are in `src/SetlistStudio.Web/appsettings.Production.json` and the app supports Azure Key Vault and Docker secrets.

## Quick health-check (PowerShell)
After starting the app (example using port 5000):

```powershell
try { Invoke-WebRequest -Uri http://localhost:5000 -UseBasicParsing -TimeoutSec 10 | Select-Object -Property StatusCode } catch { Write-Output "error: $($_.Exception.Message)" }
```

If you want, I can also add a `launchSettings.json` profile or a VS Code task to automate this. — Let me know which you prefer.
