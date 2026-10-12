[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+(\.\d+)?$')][string]$Version,
    [Parameter(Mandatory)][string]$PublishDir,
    [Parameter(Mandatory)][string]$Compiler,
    [Parameter(Mandatory)][string]$OutputRoot
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$testRoot = [IO.Path]::GetFullPath($OutputRoot)
$verificationRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'artifacts\verification')).TrimEnd('\') + '\'
if (-not $testRoot.StartsWith($verificationRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Installer validation must stay inside artifacts\verification.'
}
if (Test-Path -LiteralPath $testRoot) { throw 'Use a new installer validation directory.' }

New-Item -ItemType Directory -Path $testRoot | Out-Null
$installDirectory = Join-Path $testRoot 'installed-app'
$isolatedData = Join-Path $testRoot 'isolated-data'
$selfTestReport = Join-Path $isolatedData 'self-test.json'
$webViewReport = Join-Path $isolatedData 'webview-test.json'
$webViewProfile = Join-Path $isolatedData 'webview-profile'
New-Item -ItemType Directory -Path $isolatedData | Out-Null
$sentinel = Join-Path $isolatedData 'preserve-me.txt'
[IO.File]::WriteAllText($sentinel, 'Installer validation must preserve external data.')
$sentinelHash = (Get-FileHash -LiteralPath $sentinel).Hash
$checks = [Collections.Generic.List[object]]::new()

function Invoke-CheckedProcess([string]$File, [string[]]$Arguments, [int]$TimeoutSeconds = 120) {
    $info = [Diagnostics.ProcessStartInfo]::new($File)
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    foreach ($argument in $Arguments) { $info.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::Start($info)
    try {
        if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
            $process.Kill()
            throw "Timed out: $File"
        }
        if ($process.ExitCode -ne 0) { throw "$File failed with exit code $($process.ExitCode)." }
    } finally {
        $process.Dispose()
    }
}

function Invoke-InstallerProcess([string]$File, [string[]]$Arguments) {
    for ($attempt = 0; $attempt -lt 5; $attempt++) {
        $info = [Diagnostics.ProcessStartInfo]::new($File)
        $info.UseShellExecute = $false
        $info.CreateNoWindow = $true
        $info.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
        foreach ($argument in $Arguments) { $info.ArgumentList.Add($argument) }
        $process = [Diagnostics.Process]::Start($info)
        try {
            if (-not $process.WaitForExit(120000)) {
                $process.Kill()
                throw "Timed out: $File"
            }
            $exitCode = $process.ExitCode
        } finally {
            $process.Dispose()
        }
        if ($exitCode -eq 0) { return }
        if ($exitCode -ne 3 -or $attempt -eq 4) { throw "$File failed with exit code $exitCode." }
        Start-Sleep -Seconds 2
    }
}

$installerScript = Join-Path $projectRoot 'packaging\google-translate.iss'
& $Compiler '/Q' '/DValidationBuild=1' ('/DAppVersion=' + $Version) ('/DPublishDir=' + $PublishDir) ('/DReleaseDir=' + $testRoot) ('/DSourceRoot=' + $projectRoot) '/FGoogleTranslate-Validation' $installerScript
if ($LASTEXITCODE -ne 0) { throw 'Validation installer compilation failed.' }

$setup = Join-Path $testRoot 'GoogleTranslate-Validation.exe'
$setupArguments = @(
    '/VERYSILENT',
    '/SUPPRESSMSGBOXES',
    '/SP-',
    '/NORESTART',
    '/NOICONS',
    '/MERGETASKS=!autostart',
    ('/DIR=' + $installDirectory),
    ('/LOG=' + (Join-Path $testRoot 'install.log'))
)
Invoke-InstallerProcess $setup $setupArguments

$installedExecutable = Join-Path $installDirectory 'GoogleTranslate.exe'
$documentationFile = Join-Path $installDirectory 'Microsoft.Web.WebView2.WinForms.xml'
$checks.Add(@{ name = 'installer-extracts-self-contained-app'; passed = (Test-Path -LiteralPath $installedExecutable) -and (Test-Path -LiteralPath $documentationFile) })
$checks.Add(@{ name = 'validation-installer-does-not-register-uninstaller'; passed = -not (Test-Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{3945D878-36B4-45D8-A221-404EA525189E}_is1') })

Invoke-CheckedProcess $installedExecutable @('--self-test', '--test-output', $selfTestReport)
$selfTest = Get-Content -LiteralPath $selfTestReport -Raw | ConvertFrom-Json
$checks.Add(@{ name = 'installed-app-self-tests-pass'; passed = $selfTest.Passed -eq $true })

Invoke-CheckedProcess $installedExecutable @('--webview-self-test', '--test-output', $webViewReport, '--profile-path', $webViewProfile)
$webViewTest = Get-Content -LiteralPath $webViewReport -Raw | ConvertFrom-Json
$checks.Add(@{ name = 'installed-app-loads-live-google-translate-offscreen'; passed = $webViewTest.Passed -eq $true -and $webViewTest.Offscreen -eq $true })

$uninstaller = Join-Path $installDirectory 'unins000.exe'
Invoke-CheckedProcess $uninstaller @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/LOG=' + (Join-Path $testRoot 'uninstall.log'))
for ($attempt = 0; $attempt -lt 100 -and (Test-Path -LiteralPath $installedExecutable); $attempt++) {
    Start-Sleep -Milliseconds 100
}
$checks.Add(@{ name = 'uninstall-removes-application'; passed = -not (Test-Path -LiteralPath $installedExecutable) })
$checks.Add(@{ name = 'uninstall-preserves-external-data'; passed = (Get-FileHash -LiteralPath $sentinel).Hash -eq $sentinelHash })

$resultsPath = Join-Path $testRoot 'results.json'
[IO.File]::WriteAllText($resultsPath, ($checks | ConvertTo-Json))
$failed = @($checks | Where-Object { -not $_.passed })
if ($failed.Count) { throw ('Installer validation failed: ' + (($failed | ForEach-Object name) -join ', ')) }
Write-Host "$($checks.Count) isolated installer checks passed."
