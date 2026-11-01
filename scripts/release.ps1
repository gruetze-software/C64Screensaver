param(
    [ValidateSet('win-x64','win-arm64','linux-x64','linux-arm64')]
    [string[]]$Runtimes = @('win-x64','win-arm64','linux-x64','linux-arm64'),
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
[xml]$project = Get-Content (Join-Path $root 'C64Screensaver/C64Screensaver.csproj') -Raw
$version = ($project.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1).ToString()
$release = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else { Join-Path $root "artifacts/release/$version" }
New-Item -ItemType Directory -Path $release -Force | Out-Null
dotnet build (Join-Path $root 'C64Screensaver.sln') -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Release build failed' }
# Preserve the curated hero image; the smoke suite writes a diagnostic with the same name.
$heroPath = Join-Path $root 'docs/images/glowing-crt.png'
$heroBytes = [IO.File]::ReadAllBytes($heroPath)
try {
    dotnet run --project (Join-Path $root 'tests/Smoke/Smoke.csproj') -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Release checks failed' }
} finally {
    [IO.File]::WriteAllBytes($heroPath, $heroBytes)
}
$archives = @()
foreach ($runtime in ($Runtimes | Select-Object -Unique)) {
    # A fresh staging directory prevents stale files from an earlier publish.
    $stage = Join-Path $root ("artifacts/staging/$version-$runtime-" + [Guid]::NewGuid().ToString('N'))
    & (Join-Path $PSScriptRoot 'publish.ps1') -Runtime $runtime -OutputDirectory $stage
    foreach ($file in @('LICENSE','CHANGELOG.md')) {
        Copy-Item -LiteralPath (Join-Path $root $file) -Destination $stage
    }
    # Ship a short offline guide, not the website's screenshots and Mac build docs.
    $readme = (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'PACKAGE-README.md') -Raw).Replace('@VERSION@', $version).Replace('@RUNTIME@', $runtime)
    $readme | Set-Content -LiteralPath (Join-Path $stage 'README.md') -Encoding utf8
    $docs = Join-Path $stage 'docs'
    New-Item -ItemType Directory -Path $docs -Force | Out-Null
    $setup = (Get-Content -LiteralPath (Join-Path $root 'docs/SCREEN-SAVER-SETUP.md') -Raw).Replace('(../macos/', '(https://github.com/gruetze-software/C64Screensaver/blob/main/macos/')
    $setup | Set-Content -LiteralPath (Join-Path $docs 'SCREEN-SAVER-SETUP.md') -Encoding utf8
    Copy-Item -LiteralPath (Join-Path $root 'C64Screensaver/Assets/Kickstart/CREDITS.md') -Destination (Join-Path $docs 'KICKSTART-CREDITS.md')
    # Fail before archiving if dependencies start copying debug symbols again.
    $symbols = Get-ChildItem -LiteralPath $stage -Recurse -File | Where-Object { $_.Extension -in @('.pdb','.dbg') }
    if ($symbols) { throw "Debug symbols found in release output: $($symbols.Name -join ', ')" }
    $archive = Join-Path $release "Gruetze-RetroSaver-$version-$runtime.zip"
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $archive -CompressionLevel Optimal -Force
    $archives += $archive
}
$checksums = foreach ($archive in $archives) {
    $hash = Get-FileHash -LiteralPath $archive -Algorithm SHA256
    "$($hash.Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($archive))"
}
$checksums | Set-Content (Join-Path $release 'SHA256SUMS.txt') -Encoding ascii
Copy-Item -LiteralPath (Join-Path $root 'CHANGELOG.md') -Destination (Join-Path $release 'RELEASE-NOTES.md')
Write-Host "Release $version prepared in $release (not uploaded)."
