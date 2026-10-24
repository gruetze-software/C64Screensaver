param(
    [ValidateSet('win-x64','win-arm64','linux-x64','linux-arm64','osx-x64','osx-arm64')]
    [string]$Runtime = 'win-x64',
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (!$OutputDirectory) { $OutputDirectory = Join-Path $root "artifacts/$Runtime" }
dotnet publish (Join-Path $root 'C64Screensaver/C64Screensaver.csproj') -c Release -r $Runtime --self-contained true -p:DebugType=None -p:DebugSymbols=false -o $OutputDirectory --nologo
if ($LASTEXITCODE -ne 0) { throw "Publish failed: $Runtime" }
if ($Runtime.StartsWith('win-')) {
    Copy-Item -LiteralPath (Join-Path $OutputDirectory 'c64screensaver.exe') -Destination (Join-Path $OutputDirectory 'c64screensaver.scr')
}
Write-Host "Published to $OutputDirectory"
