# Take_Screenshot.ps1
# Launches Notepad (white background), SqlPropagate, executes scripts via
# UI Automation, waits for completion, captures screenshot, and cleans up.
#
# Usage: Right-click > Run with PowerShell, or run from terminal:
#   powershell -ExecutionPolicy Bypass -File Take_Screenshot.ps1

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

# Load C# helper class (supports re-running in the same session).
$csFilePath = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "Take_Screenshot.ps1.cs"
$csFileContent = Get-Content -Path $csFilePath -Raw
$fileHash = Get-FileHash -InputStream ([System.IO.MemoryStream]::new([System.Text.Encoding]::UTF8.GetBytes($csFileContent))) -Algorithm SHA256
$className = "TakeScreenshot"
if (-not $script:loadedClasses) { $script:loadedClasses = @{} }
if (-not $script:loadedClasses.ContainsKey($fileHash.Hash)) {
    $className += (Get-Date -Format "yyyyMMddHHmmss")
    $csCode = $csFileContent -replace "TakeScreenshot", $className
    Add-Type -TypeDefinition $csCode -ReferencedAssemblies System.Windows.Forms, System.Drawing, UIAutomationClient, UIAutomationTypes
    $script:loadedClasses[$fileHash.Hash] = $className
} else {
    $className = $script:loadedClasses[$fileHash.Hash]
}
$helper = [Type]$className

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$screenshot1 = Join-Path $scriptDir "Images\JocysComSqlPropagate.png"
$screenshot2 = Join-Path $scriptDir "Images\JocysComSqlPropagate_Files.png"

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

# Ensure a fresh config (delete existing so defaults are extracted).
$xmlConfig = Join-Path $appDir "JocysCom.Sql.Propagate.xml"
if (Test-Path $xmlConfig) { Remove-Item $xmlConfig -Force }

$notepad = $null
$sqlPropagate = $null

try {
    # 1. Start Notepad as white background.
    Write-Host "1. Starting Notepad (background)..."
    $tempFile = Join-Path $env:TEMP "SqlPropagate_Background.txt"
    "" | Out-File -FilePath $tempFile -Encoding UTF8
    $notepadPidsBefore = @(Get-Process -Name "Notepad" -ErrorAction SilentlyContinue | ForEach-Object { $_.Id })
    Start-Process -FilePath "notepad.exe" -ArgumentList "`"$tempFile`""
    Start-Sleep -Seconds 2
    $notepad = Get-Process -Name "Notepad" -ErrorAction SilentlyContinue |
        Where-Object { $notepadPidsBefore -notcontains $_.Id -and $_.MainWindowHandle -ne [IntPtr]::Zero } |
        Select-Object -First 1
    if ($notepad) {
        $npHwnd = $notepad.MainWindowHandle
        $helper::CenterAndResize($npHwnd, 1040, 680)
        $helper::SetForegroundWindow($npHwnd) | Out-Null
        Start-Sleep -Milliseconds 300
    }

    # 2. Start SqlPropagate.
    Write-Host "2. Starting SqlPropagate..."
    $sqlPropagate = Start-Process -FilePath $appPath -WorkingDirectory $appDir -PassThru
    Start-Sleep -Seconds 3
    $sqlPropagate.Refresh()
    $appHwnd = $sqlPropagate.MainWindowHandle
    $helper::CenterAndResize($appHwnd, 920, 480)
    $helper::SetForegroundWindow($appHwnd) | Out-Null
    Start-Sleep -Seconds 1

    # 3. Screenshot 1: Main tab with default data (before execution).
    Write-Host "3. Taking screenshot 1 (before execution)..."
    $helper::CaptureWindow($appHwnd, $screenshot1)
    Write-Host "   Saved: $screenshot1"

    # 4. Find UI elements via Automation and click Execute.
    Write-Host "4. Clicking Execute via UI Automation..."
    $mainWindow = $helper::FindMainWindow($sqlPropagate.Id, 10000)
    if (-not $mainWindow) { throw "Main window not found via UI Automation." }

    # Find and click the ExecuteButton inside ScriptsPanel.
    $executeBtn = $helper::FindDescendant($mainWindow, "ExecuteButton", 5000)
    if (-not $executeBtn) { throw "ExecuteButton not found." }
    $helper::ClickButton($executeBtn)
    Start-Sleep -Milliseconds 500

    # 5. Handle the confirmation dialog — click OK (Button1).
    Write-Host "5. Confirming execution dialog..."
    # The dialog is a new window in the same process.
    $desktop = [System.Windows.Automation.AutomationElement]::RootElement
    $dialogCondition = New-Object System.Windows.Automation.AndCondition(
        (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $sqlPropagate.Id)),
        (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Window))
    )
    Start-Sleep -Milliseconds 500
    $windows = $desktop.FindAll([System.Windows.Automation.TreeScope]::Children, $dialogCondition)
    foreach ($win in $windows) {
        $okBtn = $helper::FindDescendant($win, "Button1", 2000)
        if ($okBtn) {
            $helper::ClickButton($okBtn)
            Write-Host "   Clicked OK."
            break
        }
    }

    # 6. Wait for execution to complete (look for "Done." in log).
    Write-Host "6. Waiting for execution to complete..."
    $logTextBox = $helper::FindDescendant($mainWindow, "LogTextBox", 5000)
    if ($logTextBox) {
        $done = $helper::WaitForText($logTextBox, "Done.", 30000)
        if ($done) {
            Write-Host "   Execution completed."
        } else {
            Write-Host "   WARNING: Timed out waiting for 'Done.' in log."
        }
    } else {
        Write-Host "   WARNING: LogTextBox not found, waiting 5 seconds..."
        Start-Sleep -Seconds 5
    }
    Start-Sleep -Milliseconds 500

    # 7. Bring app to front and take screenshot 2 (after execution with log output).
    Write-Host "7. Taking screenshot 2 (after execution)..."
    $helper::SetForegroundWindow($appHwnd) | Out-Null
    Start-Sleep -Milliseconds 500
    $helper::CaptureWindow($appHwnd, $screenshot2)
    Write-Host "   Saved: $screenshot2"

} finally {
    Write-Host "8. Cleaning up..."
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
