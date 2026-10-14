$ErrorActionPreference = "Stop"
$serverDirectory = (Resolve-Path (Join-Path $PSScriptRoot "..\Server")).Path
$pidFile = Join-Path $serverDirectory ".server.pid"

if (-not (Test-Path -LiteralPath $pidFile)) {
    Write-Host "The update server is not running."
    exit 0
}

$serverProcessId = [int](Get-Content -LiteralPath $pidFile -Raw)
$serverProcess = Get-Process -Id $serverProcessId -ErrorAction SilentlyContinue
if ($null -ne $serverProcess -and $serverProcess.ProcessName -like "python*") {
    Stop-Process -Id $serverProcessId
    Write-Host "Update server stopped. PID: $serverProcessId"
} else {
    Write-Host "No matching Python server process was found."
}

Remove-Item -LiteralPath $pidFile -Force
