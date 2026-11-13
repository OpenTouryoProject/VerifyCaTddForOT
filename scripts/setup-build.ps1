# Download archive/<ref>.zip -> build base -> vendor to OpenTouryoAssemblies\Build_<runtime>.
# Idempotent; re-run to refresh. One script for all runtimes (-Runtime). ASCII only (WinPS 5.1 safe).
param(
    [ValidateSet('net48','netcore100')][string]$Runtime = 'net48',
    [string]$Ref = 'develop',
    [switch]$RichClient,      # 2CS / WSClient_* targets
    [switch]$Redownload,      # moving ref (develop): drop the cached zip and fetch again
    [switch]$KeepExtract      # reuse the extract tree (only when another runtime was just built in the same setup)
)
$ErrorActionPreference = 'Stop'
$repo    = Split-Path -Parent $PSScriptRoot
$work    = 'C:\otr'   # short root: avoids MAX_PATH (MSB3553)
$zip     = Join-Path $work "OpenTouryo-$Ref.zip"
$extract = Join-Path $work "OpenTouryo-$Ref"
$cs      = Join-Path $extract 'root\programs\CS'
$vendor  = Join-Path $repo "OpenTouryoAssemblies\Build_$Runtime"
$overlay = Join-Path $repo 'base2-overlay'

New-Item -ItemType Directory -Force -Path $work | Out-Null
if ($Redownload) { Remove-Item $zip, $extract -Recurse -Force -ErrorAction SilentlyContinue }
if (-not $KeepExtract -and (Test-Path $extract)) { Remove-Item $extract -Recurse -Force }
if (-not (Test-Path $extract)) {
    if (-not (Test-Path $zip)) {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -Uri "https://github.com/OpenTouryoProject/OpenTouryo/archive/$Ref.zip" -OutFile $zip
    }
    Expand-Archive -Path $zip -DestinationPath $work -Force
}
$zipHash = (Get-FileHash $zip -Algorithm SHA256).Hash.Substring(0,12)

if (Test-Path $overlay) { Copy-Item -Path (Join-Path $overlay '*') -Destination $cs -Recurse -Force }

Push-Location $cs
try {
    cmd /c ".\2_Build_NuGet_$Runtime.bat < nul"
    if ($LASTEXITCODE -ne 0) { throw "2_Build_NuGet_$Runtime failed ($LASTEXITCODE)" }
    cmd /c ".\3_Build_Business_$Runtime.bat < nul"
    if ($LASTEXITCODE -ne 0) { throw "3_Build_Business_$Runtime failed ($LASTEXITCODE)" }
    if ($RichClient -and $Runtime -eq 'netcore100') {
        cmd /c ".\3_Build_BusinessRichClient_netcore100.bat < nul"
        if ($LASTEXITCODE -ne 0) { throw "3_Build_BusinessRichClient_netcore100 failed ($LASTEXITCODE)" }
    }
} finally { Pop-Location }

if ($RichClient -and $Runtime -eq 'net48') {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    $msb = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
    $brc = Join-Path $cs 'Frameworks\Infrastructure\BusinessRichClient_net48.sln'
    foreach ($rcObj in @((Join-Path $cs 'Frameworks\Infrastructure\Business\RichClient\obj'),
                         (Join-Path $cs 'Frameworks\Infrastructure\CustomControl\RichClient\obj'))) {
        if (Test-Path $rcObj) {
            Get-ChildItem $rcObj -Filter 'project.assets.json' -EA SilentlyContinue | Remove-Item -Force -EA SilentlyContinue
            Get-ChildItem $rcObj -Filter '*.nuget.*' -EA SilentlyContinue | Remove-Item -Force -EA SilentlyContinue
        }
    }
    & $msb $brc /t:build /p:Configuration=Release /nologo /v:m
    if ($LASTEXITCODE -ne 0) { throw "BusinessRichClient build failed ($LASTEXITCODE)" }
}

$src = Join-Path $cs "Frameworks\Infrastructure\Build_$Runtime"
if (-not (Test-Path $src)) { throw "Build output not found: $src" }
New-Item -ItemType Directory -Force -Path $vendor | Out-Null
Copy-Item -Path (Join-Path $src '*') -Destination $vendor -Recurse -Force
$probe = if ($Runtime -eq 'net48') { 'OpenTouryo.Business.dll' } else { 'net10.0\OpenTouryo.Business.dll' }
if (-not (Test-Path (Join-Path $vendor $probe))) { throw "Base build did not produce $probe (check the build output)." }
if ($RichClient -and $Runtime -eq 'net48') {
    foreach ($rc in 'OpenTouryo.Business.RichClient.dll','OpenTouryo.CustomControl.RichClient.dll') {
        if (-not (Test-Path (Join-Path $vendor $rc))) { throw "RichClient vendor incomplete: $rc missing." }
    }
}
Get-ChildItem $vendor -Filter 'OpenTouryo.*.dll' -Recurse | Select-Object -ExpandProperty Name

"{0}`t{1}`tref={2}`trichclient={3}`tzip={4}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm'), $Runtime, $Ref, [bool]$RichClient, $zipHash |
    Add-Content -Path (Join-Path $repo 'OpenTouryoAssemblies\build-ref.txt')
