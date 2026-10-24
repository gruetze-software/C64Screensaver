param(
    [ValidateSet('win-x64','win-arm64','linux-x64','linux-arm64','osx-x64','osx-arm64')]
    [string[]]$Runtimes = @('win-x64','win-arm64','linux-x64','linux-arm64','osx-x64','osx-arm64')
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
[xml]$project = Get-Content (Join-Path $root 'C64Screensaver/C64Screensaver.csproj') -Raw
$version = ($project.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1).ToString()
$release = Join-Path $root "artifacts/release/$version"
New-Item -ItemType Directory -Path $release -Force | Out-Null
dotnet build (Join-Path $root 'C64Screensaver.sln') -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Release build failed' }
dotnet run --project (Join-Path $root 'tests/Smoke/Smoke.csproj') -c Release --no-build
if ($LASTEXITCODE -ne 0) { throw 'Release checks failed' }
$archives = @()
foreach ($runtime in ($Runtimes | Select-Object -Unique)) {
    # A fresh staging directory prevents stale files from an earlier publish.
    $stage = Join-Path $root ("artifacts/staging/$version-$runtime-" + [Guid]::NewGuid().ToString('N'))
    & (Join-Path $PSScriptRoot 'publish.ps1') -Runtime $runtime -OutputDirectory $stage
    foreach ($file in @('README.md','LICENSE','CHANGELOG.md')) {
        Copy-Item -LiteralPath (Join-Path $root $file) -Destination $stage
    }
    $images = Join-Path $stage 'docs/images'
    New-Item -ItemType Directory -Path $images -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $root 'docs/CRT-RESEARCH.md') -Destination (Join-Path $stage 'docs')
    Copy-Item -LiteralPath (Join-Path $root 'docs/LINUX-INTEGRATION.md') -Destination (Join-Path $stage 'docs')
    foreach ($image in @('c64screensaver.png','settings-playback.png','settings-monitor.png','glowing-crt.png','automatic-start-settings.png')) {
        Copy-Item -LiteralPath (Join-Path $root "docs/images/$image") -Destination $images
    }
    $archive = Join-Path $release "C64Screensaver-$version-$runtime.zip"
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
