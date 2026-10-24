param(
    [ValidateSet('linux-x64','linux-arm64')]
    [string]$Runtime = 'linux-x64'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$output = Join-Path $root 'snap/local/publish'
if (Test-Path -LiteralPath $output) {
    throw "Staging folder already exists: $output. Move it aside before preparing another architecture."
}
& (Join-Path $PSScriptRoot 'publish.ps1') -Runtime $Runtime -OutputDirectory $output
[xml]$project = Get-Content (Join-Path $root 'C64Screensaver/C64Screensaver.csproj') -Raw
$version = ($project.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1).ToString()
$arch = if ($Runtime -eq 'linux-x64') { 'amd64' } else { 'arm64' }
[IO.File]::WriteAllText((Join-Path $output 'BUILD-VERSION'), $version)
[IO.File]::WriteAllText((Join-Path $output 'BUILD-ARCH'), $arch)
Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination $output
Write-Host "Prepared $arch. On Ubuntu: chmod +x snap/local/publish/c64screensaver snap/local/launcher/launch; snapcraft --platform $arch"
