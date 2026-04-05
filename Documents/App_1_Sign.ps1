Import-Module "d:\_Backup\Configuration\SSL\Tools\app_signModule.ps1" -Force

[string[]]$appFiles = @(
    "..\SqlPropagate\bin\Release\publish\JocysCom.Sql.Propagate.exe"
)
[string]$appName = "Jocys.com SQL Propagate"
[string]$appLink = "https://www.jocys.com"

ProcessFiles $appName $appLink $appFiles
pause
