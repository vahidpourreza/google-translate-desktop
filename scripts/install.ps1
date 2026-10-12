[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$executable = Join-Path $projectRoot 'dist\GoogleTranslate\GoogleTranslate.exe'
if (-not (Test-Path -LiteralPath $executable)) {
    throw "Published app not found: $executable"
}

$shell = New-Object -ComObject WScript.Shell
$shortcutTargets = @(
    (Join-Path ([Environment]::GetFolderPath('Desktop')) 'Google Translate.lnk'),
    (Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Google Translate.lnk')
)
$legacyShortcutTargets = @(
    (Join-Path ([Environment]::GetFolderPath('Desktop')) 'Translate Desktop.lnk'),
    (Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Translate Desktop.lnk')
)

foreach ($legacyShortcutPath in $legacyShortcutTargets) {
    if (Test-Path -LiteralPath $legacyShortcutPath) {
        Remove-Item -LiteralPath $legacyShortcutPath -Force
    }
}

foreach ($shortcutPath in $shortcutTargets) {
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $executable
    $shortcut.WorkingDirectory = Split-Path $executable
    $shortcut.IconLocation = "$executable,0"
    $shortcut.Description = 'Open the Google Translate website in its desktop companion'
    $shortcut.Save()
}

$runKey = 'Registry::HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run'
New-Item -Path $runKey -Force | Out-Null
Remove-ItemProperty -LiteralPath $runKey -Name 'TranslateDesktop' -ErrorAction SilentlyContinue
Set-ItemProperty -LiteralPath $runKey -Name 'GoogleTranslate' -Value ('"' + $executable + '" --background') -Type String

[pscustomobject]@{
    Executable = $executable
    DesktopShortcut = $shortcutTargets[0]
    StartMenuShortcut = $shortcutTargets[1]
    StartsWithWindows = $true
} | ConvertTo-Json
