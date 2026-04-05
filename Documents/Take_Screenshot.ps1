# Take_Screenshot.ps1
# Launches Notepad (white background), then SqlPropagate with /AutoExecute,
# waits for completion, and captures a single screenshot.
#
# Usage: Right-click > Run with PowerShell, or run from terminal:
#   powershell -ExecutionPolicy Bypass -File Take_Screenshot.ps1

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# Load C# helper class (supports re-running in the same session).
$csFilePath = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "Take_Screenshot.ps1.cs"
$csFileContent = Get-Content -Path $csFilePath -Raw
$fileHash = Get-FileHash -InputStream ([System.IO.MemoryStream]::new([System.Text.Encoding]::UTF8.GetBytes($csFileContent))) -Algorithm SHA256
$className = "TakeScreenshot"
if (-not $script:loadedClasses) { $script:loadedClasses = @{} }
if (-not $script:loadedClasses.ContainsKey($fileHash.Hash)) {
    $className += (Get-Date -Format "yyyyMMddHHmmss")
    $csCode = $csFileContent -replace "TakeScreenshot", $className
    Add-Type -TypeDefinition $csCode -ReferencedAssemblies System.Windows.Forms, System.Drawing
    $script:loadedClasses[$fileHash.Hash] = $className
} else {
    $className = $script:loadedClasses[$fileHash.Hash]
}
$helper = [Type]$className

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$screenshotPath = Join-Path $scriptDir "Images\JocysComSqlPropagate.png"

# Find SqlPropagate executable.
$appPath = Join-Path $scriptDir "..\SqlPropagate\bin\Debug\net8.0-windows\JocysCom.Sql.Propagate.exe"
if (-not (Test-Path $appPath)) {
    $appPath = Join-Path $scriptDir "..\SqlPropagate\bin\Release\net8.0-windows\JocysCom.Sql.Propagate.exe"
}
if (-not (Test-Path $appPath)) {
    Write-Error "SqlPropagate not found. Build the project first."
    exit 1
}
$appPath = Resolve-Path $appPath
$appDir = Split-Path -Parent $appPath

# Ensure fresh config (delete existing so defaults are extracted).
$xmlConfig = Join-Path $appDir "JocysCom.Sql.Propagate.xml"
if (Test-Path $xmlConfig) { Remove-Item $xmlConfig -Force }

# Ensure Images folder exists.
$imagesDir = Join-Path $scriptDir "Images"
if (-not (Test-Path $imagesDir)) { New-Item -ItemType Directory -Path $imagesDir | Out-Null }

$notepad = $null
$sqlPropagate = $null
$tempFile = Join-Path $env:TEMP "SqlPropagate_Background.txt"

try {
    # 1. Start Notepad as white background.
    Write-Host "1. Starting Notepad (background)..."
    "" | Out-File -FilePath $tempFile -Encoding UTF8
    $notepadPidsBefore = @(Get-Process -Name "Notepad" -ErrorAction SilentlyContinue | ForEach-Object { $_.Id })
    Start-Process -FilePath "notepad.exe" -ArgumentList "`"$tempFile`""
    Start-Sleep -Seconds 2
    $notepad = Get-Process -Name "Notepad" -ErrorAction SilentlyContinue |
        Where-Object { $notepadPidsBefore -notcontains $_.Id -and $_.MainWindowHandle -ne [IntPtr]::Zero } |
        Select-Object -First 1
    if ($notepad) {
        $npHwnd = $notepad.MainWindowHandle
        $helper::CenterAndResize($npHwnd, 1100, 800)
        $helper::SetForegroundWindow($npHwnd) | Out-Null
        Start-Sleep -Milliseconds 300
    }

    # 2. Launch with /AutoExecute, wait for completion, capture.
    Write-Host "2. Starting SqlPropagate with /AutoExecute..."
    $sqlPropagate = Start-Process -FilePath $appPath -WorkingDirectory $appDir -ArgumentList "/AutoExecute /AutoDelay=2000" -PassThru
    Start-Sleep -Seconds 3
    $sqlPropagate.Refresh()
    $appHwnd = $sqlPropagate.MainWindowHandle
    $helper::CenterAndResize($appHwnd, 920, 610)
    $helper::SetForegroundWindow($appHwnd) | Out-Null

    Write-Host "   Waiting for execution to complete..."
    Start-Sleep -Seconds 8

    # 3. Capture screenshot.
    $helper::SetForegroundWindow($appHwnd) | Out-Null
    Start-Sleep -Milliseconds 500
    Write-Host "3. Taking screenshot..."
    $helper::CaptureWindow($appHwnd, $screenshotPath)
    Write-Host "   Saved: $screenshotPath"

} finally {
    Write-Host "4. Cleaning up..."
    if ($sqlPropagate -and -not $sqlPropagate.HasExited) {
        $sqlPropagate.Kill()
        $sqlPropagate.WaitForExit(3000) | Out-Null
    }
    if ($notepad -and -not $notepad.HasExited) {
        $notepad.Kill()
        $notepad.WaitForExit(3000) | Out-Null
    }
    Remove-Item $tempFile -ErrorAction SilentlyContinue
    Write-Host "Done."
}
