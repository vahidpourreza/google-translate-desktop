[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+(\.\d+)?$')][string]$Version,
    [string]$InnoCompiler,
    [switch]$SkipInstallerTest
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$projectFile = Join-Path $projectRoot 'TranslateDesktop.csproj'
$artifactsRoot = Join-Path $projectRoot 'artifacts'
$releaseRoot = Join-Path $projectRoot 'dist\installer'
$publishDirectory = Join-Path $artifactsRoot 'release\publish\win-x64'
$verificationDirectory = Join-Path $artifactsRoot ('verification\' + [DateTime]::Now.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 6))

function Assert-ProjectChild([string]$Path) {
    $full = [IO.Path]::GetFullPath($Path)
    $root = $projectRoot.TrimEnd('\') + '\'
    if (-not $full.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) { throw "Path is outside the project: $full" }
    return $full
}

function Reset-BuildDirectory([string]$Path) {
    $safe = Assert-ProjectChild $Path
    if (Test-Path -LiteralPath $safe) {
        if (@(Get-ChildItem -LiteralPath $safe -Recurse -Force -Attributes ReparsePoint).Count) { throw "Refusing to clean linked build content: $safe" }
        Remove-Item -LiteralPath $safe -Recurse -Force
    }
    New-Item -ItemType Directory -Path $safe -Force | Out-Null
}

function Invoke-CheckedTool([string]$File, [string[]]$Arguments) {
    & $File @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$File failed with exit code $LASTEXITCODE." }
}

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

if (-not $Version) {
    [xml]$project = Get-Content -LiteralPath $projectFile
    $Version = [string]($project.Project.PropertyGroup | Where-Object Version | Select-Object -First 1).Version
}
if (-not $InnoCompiler) {
    $InnoCompiler = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
    ) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $InnoCompiler -or -not (Test-Path -LiteralPath $InnoCompiler)) { throw 'Inno Setup 6 is required on the build machine.' }

Reset-BuildDirectory $publishDirectory
New-Item -ItemType Directory -Path $verificationDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $releaseRoot -Force | Out-Null

Invoke-CheckedTool 'dotnet' @('run', '--project', (Join-Path $projectRoot 'tools\IconGenerator\IconGenerator.csproj'), '-c', 'Release', '--', (Join-Path $projectRoot 'assets\google-translate-logo-original.png'), (Join-Path $projectRoot 'assets\translate.ico'))
Invoke-CheckedTool 'dotnet' @('restore', $projectFile, '-r', 'win-x64', '--locked-mode')
Invoke-CheckedTool 'dotnet' @('publish', $projectFile, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '--no-restore', ('-p:Version=' + $Version), '-p:PublishSingleFile=true', '-p:DebugType=None', '-p:DebugSymbols=false', '-o', $publishDirectory)

$publishedExecutable = Join-Path $publishDirectory 'GoogleTranslate.exe'
if (-not (Test-Path -LiteralPath $publishedExecutable)) { throw 'Published executable is missing.' }

$selfTestReport = Join-Path $verificationDirectory 'self-test.json'
$webViewReport = Join-Path $verificationDirectory 'webview-test.json'
Invoke-CheckedProcess $publishedExecutable @('--self-test', '--test-output', $selfTestReport)
Invoke-CheckedProcess $publishedExecutable @('--webview-self-test', '--test-output', $webViewReport, '--profile-path', (Join-Path $verificationDirectory 'webview-profile'))
if (-not (Get-Content -LiteralPath $selfTestReport -Raw | ConvertFrom-Json).Passed) { throw 'Published self-test failed.' }
if (-not (Get-Content -LiteralPath $webViewReport -Raw | ConvertFrom-Json).Passed) { throw 'Published WebView smoke test failed.' }

if (-not $SkipInstallerTest) {
    & (Join-Path $PSScriptRoot 'test-installer.ps1') -Version $Version -PublishDir $publishDirectory -Compiler $InnoCompiler -OutputRoot (Join-Path $verificationDirectory 'installer')
    if ($LASTEXITCODE -ne 0) { throw 'Installer validation failed.' }
}

Invoke-CheckedTool $InnoCompiler @(
    '/Q',
    ('/DAppVersion=' + $Version),
    ('/DPublishDir=' + $publishDirectory),
    ('/DReleaseDir=' + $releaseRoot),
    ('/DSourceRoot=' + $projectRoot),
    (Join-Path $projectRoot 'packaging\google-translate.iss')
)

$installer = Join-Path $releaseRoot ('GoogleTranslate-Setup-' + $Version + '-win-x64.exe')
if (-not (Test-Path -LiteralPath $installer)) { throw 'Installer output is missing.' }
$hash = (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash
$hashFile = $installer + '.sha256'
[IO.File]::WriteAllText($hashFile, $hash + '  ' + [IO.Path]::GetFileName($installer) + [Environment]::NewLine)
$details = [ordered]@{
    version = $Version
    installer = $installer
    sha256 = $hash
    sizeMiB = [Math]::Round((Get-Item -LiteralPath $installer).Length / 1MB, 2)
    verification = $verificationDirectory
    installerTested = -not $SkipInstallerTest
}
[IO.File]::WriteAllText((Join-Path $releaseRoot 'release-info.json'), ($details | ConvertTo-Json))
$details | ConvertTo-Json
