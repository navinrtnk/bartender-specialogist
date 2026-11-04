# Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
[CmdletBinding()]
param([switch]$SmokeTest, [switch]$Export, [switch]$Compatibility)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$toolRoot = Join-Path $env:LOCALAPPDATA 'BartenderSpecialogist\Tools'

function Invoke-Checked([string]$Command, [string[]]$Arguments) {
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$([IO.Path]::GetFileName($Command)) exited with code $LASTEXITCODE." }
}

# Optional portable tools do not modify the system installation.
if (Test-Path (Join-Path $toolRoot 'dotnet\dotnet.exe')) {
    $env:DOTNET_ROOT = Join-Path $toolRoot 'dotnet'
    $env:PATH = "$env:DOTNET_ROOT;$env:PATH"
}
$godot = $env:GODOT_BIN
if (-not $godot) {
    $godot = (Get-Command godot, godot-mono -ErrorAction SilentlyContinue | Select-Object -First 1).Source
}
if (-not $godot -and (Test-Path (Join-Path $toolRoot 'godot'))) {
    $godot = Get-ChildItem -LiteralPath (Join-Path $toolRoot 'godot') -Recurse -Filter '*console.exe' |
        Select-Object -First 1 -ExpandProperty FullName
}
if (-not $godot) { throw 'Install Godot 4.7.2 .NET and set GODOT_BIN to its executable.' }

Push-Location -LiteralPath $projectRoot
try {
    if ($Export) {
        $version = (Get-Content -LiteralPath 'VERSION' -Raw).Trim()
        if ($version -notmatch '^\d+\.\d+(\.\d+)?$') { throw 'VERSION must contain a version such as 1.0 or 1.0.1.' }
        $parts = [version]$version
        $fileVersion = '{0}.{1}.{2}.0' -f $parts.Major, $parts.Minor, [math]::Max(0, $parts.Build)
        $buildRoot = Join-Path $projectRoot "builds\$version\windows"
        $presetPath = Join-Path $projectRoot 'export_presets.cfg'
        $preset = [IO.File]::ReadAllText($presetPath)
        $preset = $preset -replace 'application/(file|product)_version="[^"]+"', ('application/$1_version="' + $fileVersion + '"')
        $preset = $preset -replace 'export_path="[^"]+"', "export_path=`"builds/$version/windows/BartenderSpecialogist.exe`""
        [IO.File]::WriteAllText($presetPath, $preset)
        $projectPath = Join-Path $projectRoot 'project.godot'
        $project = [IO.File]::ReadAllText($projectPath) -replace 'config/version="[^"]+"', "config/version=`"$version`""
        [IO.File]::WriteAllText($projectPath, $project)
        Invoke-Checked dotnet @('clean', '--nologo')
    }
    Invoke-Checked dotnet @('build', '--no-incremental', '--nologo')
    Invoke-Checked $godot @('--headless', '--path', $projectRoot, '--editor', '--import', '--quit')

    if ($SmokeTest -or $Export) {
        $log = Join-Path ([IO.Path]::GetTempPath()) ("bartender-smoke-$([guid]::NewGuid()).log")
        Invoke-Checked $godot @('--headless', '--path', $projectRoot, '--log-file', $log, '--', '--smoke-test')
        if (Select-String -LiteralPath $log -Pattern '^(ERROR:|SHADER ERROR:|SCRIPT ERROR:)' -Quiet) {
            throw "Godot reported a scene or shader error. See $log"
        }
    }
    if ($Export) {
        New-Item -ItemType Directory -Force -Path $buildRoot | Out-Null
        Invoke-Checked $godot @('--headless', '--path', $projectRoot, '--export-release', 'Windows Desktop', (Join-Path $buildRoot 'BartenderSpecialogist.exe'))
        if (-not (Test-Path (Join-Path $buildRoot 'data_BartenderSpecialogist_windows_x86_64\BartenderSpecialogist.dll'))) {
            throw 'The export is missing its C# assembly.'
        }
        $notices = @('LICENSE', 'THIRD_PARTY_NOTICES.md', 'Assets\Customer\SOURCES.md', 'Assets\Customer\sources.json',
            'Assets\Materials\sources.json', 'Assets\Labels\FONT-LICENSE.txt', 'Assets\Labels\SOURCES.md')
        foreach ($file in $notices) {
            $destination = Join-Path $buildRoot $file
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
            Copy-Item -LiteralPath (Join-Path $projectRoot $file) -Destination $destination -Force
        }
        $zip = Join-Path $projectRoot "builds\BartenderSpecialogist-$version-windows.zip"
        Compress-Archive -Path (Join-Path $buildRoot '*') -DestinationPath $zip -CompressionLevel Optimal -Force
        Write-Output "Build: $buildRoot"
        Write-Output "ZIP: $zip"
    } elseif (-not $SmokeTest) {
        $arguments = @('--path', $projectRoot)
        if ($Compatibility) { $arguments += @('--rendering-method', 'gl_compatibility') }
        Invoke-Checked $godot $arguments
    }
} finally {
    Pop-Location
}
