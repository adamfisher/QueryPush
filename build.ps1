#!/usr/bin/env pwsh
# QueryPush Build Script - PowerShell
# Builds self-contained binaries for all platforms and architectures

param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "./publish"
)

$ErrorActionPreference = "Stop"

# Define runtime identifiers for different platforms
$runtimes = @(
    "win-x64",
    "win-x86",
    "win-arm64",
    "linux-x64",
    "linux-arm64",
    "linux-arm",
    "osx-x64",
    "osx-arm64"
)

$projectPath = "./QueryPush/QueryPush.csproj"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "QueryPush Multi-Platform Build Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path $OutputDir) {
    Remove-Item -Recurse -Force $OutputDir
}
New-Item -ItemType Directory -Path $OutputDir | Out-Null

# Build for each runtime
foreach ($runtime in $runtimes) {
    Write-Host ""
    Write-Host "Building for $runtime..." -ForegroundColor Green

    $outputPath = "$OutputDir/$runtime"

    try {
        dotnet publish $projectPath `
            --configuration $Configuration `
            --runtime $runtime `
            --self-contained true `
            --output $outputPath `
            /p:PublishSingleFile=true `
            /p:PublishTrimmed=false `
            /p:DebugType=None `
            /p:DebugSymbols=false

        if ($LASTEXITCODE -eq 0) {
            Write-Host "[OK] Successfully built $runtime" -ForegroundColor Green

            # Get the output file size
            $exeName = if ($runtime.StartsWith("win")) { "QueryPush.exe" } else { "QueryPush" }
            $exePath = Join-Path $outputPath $exeName
            if (Test-Path $exePath) {
                $size = (Get-Item $exePath).Length / 1MB
                Write-Host "  Output: $exePath ($([math]::Round($size, 2)) MB)" -ForegroundColor Gray
            }

            # Create zip archive
            Write-Host "  Creating archive..." -ForegroundColor Gray
            $zipPath = "$OutputDir/QueryPush-$runtime.zip"
            Compress-Archive -Path "$outputPath/*" -DestinationPath $zipPath -Force

            if (Test-Path $zipPath) {
                $zipSize = (Get-Item $zipPath).Length / 1MB
                Write-Host "  Archive: $zipPath ($([math]::Round($zipSize, 2)) MB)" -ForegroundColor Gray
            }
        } else {
            Write-Host "[FAIL] Failed to build $runtime" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "[ERROR] Error building $runtime : $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Build Complete!" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Binaries are located in: $OutputDir" -ForegroundColor Yellow
Write-Host ""

# List all zip files
Write-Host "Created archives:" -ForegroundColor Cyan
Get-ChildItem -Path $OutputDir -Filter "*.zip" | ForEach-Object {
    $zipSize = $_.Length / 1MB
    Write-Host "  $($_.Name) ($([math]::Round($zipSize, 2)) MB)" -ForegroundColor Gray
}
Write-Host ""
