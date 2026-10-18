param(
    [int]$Port = 8000,
    [string]$BindAddress = "127.0.0.1"
)

$ErrorActionPreference = "Stop"
$serverDirectory = (Resolve-Path (Join-Path $PSScriptRoot "..\Server")).Path
$pidFile = Join-Path $serverDirectory ".server.pid"

if (Test-Path -LiteralPath $pidFile) {
    $savedProcessId = [int](Get-Content -LiteralPath $pidFile -Raw)
    $existingProcess = Get-Process -Id $savedProcessId -ErrorAction SilentlyContinue
    if ($null -ne $existingProcess -and $existingProcess.ProcessName -like "python*") {
        Write-Host "Update server is already running. PID: $savedProcessId"
        exit 0
    }
}

$pythonPath = (& py -3 -c "import sys; print(sys.executable)").Trim()
if (-not (Test-Path -LiteralPath $pythonPath)) {
    throw "Python 3 was not found."
}

$serverProcess = Start-Process `
    -FilePath $pythonPath `
    -ArgumentList @("-m", "http.server", $Port, "--bind", $BindAddress) `
    -WorkingDirectory $serverDirectory `
    -WindowStyle Hidden `
    -PassThru

Start-Sleep -Milliseconds 800
if ($serverProcess.HasExited) {
    throw "The update server could not start. Port $Port may already be in use."
}

Set-Content -LiteralPath $pidFile -Value $serverProcess.Id -NoNewline
$defaultRoute = Get-NetRoute -DestinationPrefix "0.0.0.0/0" -ErrorAction SilentlyContinue |
    Sort-Object RouteMetric, InterfaceMetric |
    Select-Object -First 1
$lanAddress = if ($null -ne $defaultRoute) {
    Get-NetIPAddress -InterfaceIndex $defaultRoute.InterfaceIndex -AddressFamily IPv4 -ErrorAction SilentlyContinue |
        Select-Object -First 1 -ExpandProperty IPAddress
}

Write-Host "Update server started. PID: $($serverProcess.Id)"
Write-Host "This PC: http://127.0.0.1:$Port/version.json"
if ($BindAddress -ne "127.0.0.1" -and $lanAddress) {
    Write-Host "LAN:     http://${lanAddress}:$Port/version.json"
}
