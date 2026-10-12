[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$shortcutTargets = @(
    (Join-Path ([Environment]::GetFolderPath('Desktop')) 'Google Translate.lnk'),
    (Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Google Translate.lnk'),
    (Join-Path ([Environment]::GetFolderPath('Desktop')) 'Translate Desktop.lnk'),
    (Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Translate Desktop.lnk')
)

foreach ($shortcutPath in $shortcutTargets) {
    if (Test-Path -LiteralPath $shortcutPath) {
        Remove-Item -LiteralPath $shortcutPath -Force
    }
}

$runKey = 'Registry::HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run'
Remove-ItemProperty -LiteralPath $runKey -Name 'TranslateDesktop' -ErrorAction SilentlyContinue
Remove-ItemProperty -LiteralPath $runKey -Name 'GoogleTranslate' -ErrorAction SilentlyContinue

'Translate Desktop shortcuts and startup registration were removed. The source and published app remain on drive D.'
