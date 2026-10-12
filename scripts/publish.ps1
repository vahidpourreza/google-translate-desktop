[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$iconProject = Join-Path $projectRoot 'tools\IconGenerator\IconGenerator.csproj'
$iconSource = Join-Path $projectRoot 'assets\google-translate-logo-original.png'
$iconPath = Join-Path $projectRoot 'assets\translate.ico'
$outputPath = Join-Path $projectRoot 'dist\GoogleTranslate'
$testOutput = Join-Path $projectRoot 'test-results\self-test.json'
$webViewTestOutput = Join-Path $projectRoot 'test-results\webview-self-test.json'
$webViewProfile = Join-Path $projectRoot 'test-results\webview-profile'

& dotnet run --project $iconProject -c Release -- $iconSource $iconPath
if ($LASTEXITCODE -ne 0) { throw 'Icon generation failed.' }

& dotnet publish (Join-Path $projectRoot 'TranslateDesktop.csproj') `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $outputPath
if ($LASTEXITCODE -ne 0) { throw 'Publishing failed.' }

New-Item -ItemType Directory -Path (Split-Path $testOutput) -Force | Out-Null
$testProcess = Start-Process `
    -FilePath (Join-Path $outputPath 'GoogleTranslate.exe') `
    -ArgumentList @('--self-test', '--test-output', ('"' + $testOutput + '"')) `
    -WindowStyle Hidden `
    -Wait `
    -PassThru
if ($testProcess.ExitCode -ne 0) { throw "Self-test failed with exit code $($testProcess.ExitCode)." }

$webViewTestProcess = Start-Process `
    -FilePath (Join-Path $outputPath 'GoogleTranslate.exe') `
    -ArgumentList @(
        '--webview-self-test',
        '--test-output', ('"' + $webViewTestOutput + '"'),
        '--profile-path', ('"' + $webViewProfile + '"')
    ) `
    -WindowStyle Hidden `
    -Wait `
    -PassThru
if ($webViewTestProcess.ExitCode -ne 0) { throw "WebView smoke test failed with exit code $($webViewTestProcess.ExitCode)." }

Get-Item -LiteralPath (Join-Path $outputPath 'GoogleTranslate.exe') | Select-Object FullName, Length, LastWriteTime
Get-Content -LiteralPath $testOutput -Raw
Get-Content -LiteralPath $webViewTestOutput -Raw
