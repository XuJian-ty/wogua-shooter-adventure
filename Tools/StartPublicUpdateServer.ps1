param(
    [int]$Port = 8000,
    [string]$Hostname = "update.xinyin.space"
)

$ErrorActionPreference = "Stop"
$cloudflared = "C:\Program Files (x86)\cloudflared\cloudflared.exe"
$configPath = Join-Path $env:USERPROFILE ".cloudflared\config.yml"
$runDirectory = Join-Path $PSScriptRoot "..\Server\Run"
New-Item -ItemType Directory -Force -Path $runDirectory | Out-Null
$pidFile = Join-Path $runDirectory ".tunnel.pid"

if (-not (Test-Path -LiteralPath $cloudflared)) {
    throw "cloudflared is not installed."
}
if (-not (Test-Path -LiteralPath $configPath)) {
    throw "Cloudflare Tunnel is not configured: $configPath"
}

& (Join-Path $PSScriptRoot "StartUpdateServer.ps1") -Port $Port -BindAddress "127.0.0.1"

if (Test-Path -LiteralPath $pidFile) {
    $savedProcessId = [int](Get-Content -LiteralPath $pidFile -Raw)
    $existingProcess = Get-Process -Id $savedProcessId -ErrorAction SilentlyContinue
    if ($null -ne $existingProcess -and $existingProcess.ProcessName -eq "cloudflared") {
        Write-Host "Cloudflare Tunnel is already running. PID: $savedProcessId"
        exit 0
    }
}

$tunnelProcess = Start-Process `
    -FilePath $cloudflared `
    -ArgumentList @("tunnel", "--config", $configPath, "run", "wogua-update") `
    -WindowStyle Hidden `
    -PassThru

Start-Sleep -Seconds 2
if ($tunnelProcess.HasExited) {
    throw "Cloudflare Tunnel could not start. Check the tunnel configuration."
}

Set-Content -LiteralPath $pidFile -Value $tunnelProcess.Id -NoNewline
Write-Host "Public update server started: https://$Hostname/version.json"
