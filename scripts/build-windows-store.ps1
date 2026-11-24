param(
    [string]$IdentityFile,
    [switch]$TestIdentity,
    [ValidateSet('x64','arm64')][string[]]$Architectures = @('x64','arm64'),
    [string]$OutputDirectory,
    [string]$MakeAppxPath
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if ($TestIdentity -and $IdentityFile) { throw 'Use IdentityFile OR TestIdentity.' }
if ($TestIdentity) {
    $identity = @{ Name = 'GruetzeSoftware.RetroSaver.Test'; Publisher = 'CN=RetroSaver Local Packaging Test'; PublisherDisplayName = 'Grütze-Software (TEST)' }
} else {
    if (!$IdentityFile) { throw 'Pass -IdentityFile with the public Partner Center identity values, or -TestIdentity for packaging checks only.' }
    $identity = Get-Content -LiteralPath $IdentityFile -Raw | ConvertFrom-Json
    foreach ($key in @('Name','Publisher','PublisherDisplayName')) {
        if (![string]$identity.$key -or $identity.$key -match 'COPY |PLACEHOLDER') { throw "Missing real Partner Center value: $key" }
    }
    if ($identity.Name -notmatch '^[A-Za-z0-9.-]{3,50}$' -or $identity.Publisher -notmatch '^CN=') { throw 'Invalid package identity; copy exact values from Partner Center.' }
}
if (!$MakeAppxPath) {
    $kit = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits/10/bin'
    $MakeAppxPath = Get-ChildItem -LiteralPath $kit -Directory | Where-Object Name -match '^10\.0\.' | Sort-Object { [version]$_.Name } -Descending | ForEach-Object { Join-Path $_.FullName 'x64/makeappx.exe' } | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (!$MakeAppxPath -or !(Test-Path -LiteralPath $MakeAppxPath)) { throw 'Install the Windows 10/11 SDK (MakeAppx.exe).' }
[xml]$project = Get-Content -LiteralPath (Join-Path $root 'C64Screensaver/C64Screensaver.csproj') -Raw
$version = ($project.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1).ToString()
$packageVersion = "$version.0"
$base = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else { Join-Path $root 'artifacts/windows-store' }
New-Item -ItemType Directory -Path $base -Force | Out-Null
$prefix = if ($TestIdentity) { 'TEST-ONLY' } else { 'STORE-CANDIDATE' }
$run = Join-Path $base ("$prefix-$version-" + [Guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory -Path $run | Out-Null
$bundleInput = Join-Path $run 'packages'
New-Item -ItemType Directory -Path $bundleInput | Out-Null
$logos = Join-Path $run 'listing-assets'
dotnet run --project (Join-Path $root 'tools/StoreAssets') -c Release -- (Join-Path $root 'C64Screensaver/Assets/app-icon.png') $logos
if ($LASTEXITCODE -ne 0) { throw 'Store asset export failed.' }
function EscapeXml([string]$text) { [System.Security.SecurityElement]::Escape($text) }
foreach ($arch in ($Architectures | Select-Object -Unique)) {
    $stage = Join-Path $run "stage-$arch"
    $app = Join-Path $stage 'App'
    & (Join-Path $PSScriptRoot 'publish.ps1') -Runtime "win-$arch" -OutputDirectory $app -WindowsStore
    Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination $app
    Copy-Item -LiteralPath (Join-Path $root 'C64Screensaver/Assets/Kickstart/CREDITS.md') -Destination (Join-Path $app 'KICKSTART-CREDITS.md')
    $wrong = Get-ChildItem -LiteralPath $app -Recurse -File | Where-Object { $_.Extension -in @('.pdb','.dbg','.dylib','.so','.icns') -or $_.Name -eq 'createdump' }
    if ($wrong) { throw "Unexpected files in Windows payload: $($wrong.Name -join ', ')" }
    Copy-Item -LiteralPath $logos -Destination (Join-Path $stage 'Assets') -Recurse
    $manifest = Get-Content -LiteralPath (Join-Path $root 'store/windows/AppxManifest.template.xml') -Raw
    $manifest = $manifest.Replace('@NAME@',(EscapeXml $identity.Name)).Replace('@PUBLISHER@',(EscapeXml $identity.Publisher)).Replace('@PUBLISHER_DISPLAY_NAME@',(EscapeXml $identity.PublisherDisplayName)).Replace('@VERSION@',$packageVersion).Replace('@ARCH@',$arch)
    $manifest | Set-Content -LiteralPath (Join-Path $stage 'AppxManifest.xml') -Encoding utf8
    & $MakeAppxPath pack /d $stage /p (Join-Path $bundleInput "Gruetze-RetroSaver-$version-$arch.msix") /o *> (Join-Path $run "makeappx-$arch.log")
    if ($LASTEXITCODE -ne 0) { throw "MSIX validation/packing failed: $arch" }
}
$bundle = Join-Path $run "Gruetze-RetroSaver-$version-$prefix.msixbundle"
& $MakeAppxPath bundle /d $bundleInput /p $bundle /bv $packageVersion /o *> (Join-Path $run "makeappx-bundle.log")
if ($LASTEXITCODE -ne 0) { throw 'Bundle creation failed.' }
Get-FileHash -LiteralPath $bundle -Algorithm SHA256 | ForEach-Object { "$($_.Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($_.Path))" } | Set-Content -LiteralPath (Join-Path $run 'SHA256SUMS.txt') -Encoding ascii
$note = if ($TestIdentity) { 'TEST IDENTITY ONLY. Do not submit to the Store. Rebuild with exact Partner Center identity.' } else { 'Unsigned Microsoft Store candidate. Microsoft signs the accepted MSIX. Not a sideload installer.' }
$note | Set-Content -LiteralPath (Join-Path $run 'READ-ME-FIRST.txt') -Encoding utf8
Write-Host "$note`nBundle: $bundle"
