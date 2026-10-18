$ErrorActionPreference = "Stop"
$pidFile = Join-Path $PSScriptRoot "..\Server\.tunnel.pid"

if (Test-Path -LiteralPath $pidFile) {
    $tunnelProcessId = [int](Get-Content -LiteralPath $pidFile -Raw)
    $tunnelProcess = Get-Process -Id $tunnelProcessId -ErrorAction SilentlyContinue
    if ($null -ne $tunnelProcess -and $tunnelProcess.ProcessName -eq "cloudflared") {
        Stop-Process -Id $tunnelProcessId
        Write-Host "Cloudflare Tunnel stopped. PID: $tunnelProcessId"
    }
    Remove-Item -LiteralPath $pidFile -Force
}

& (Join-Path $PSScriptRoot "StopUpdateServer.ps1")
