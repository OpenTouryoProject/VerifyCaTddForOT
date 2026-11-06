# Download archive/<ref>.zip -> build the net10.0 base -> vendor to OpenTouryoAssemblies\Build_netcore100.
# Based on opentouryo-project-setup-build examples.md (setup-build-netcore.ps1), plus -Fresh / -Redownload
# from setup-build.ps1 (this repo has no net48 run that would refresh the moving ref first).
param(
    [string]$Ref = 'develop',
    [switch]$Fresh,        # re-extract the work tree (pristine build; an earlier round applied base2-overlay)
    [switch]$Redownload    # delete <ref>.zip first (develop is a moving ref)
)
$ErrorActionPreference = 'Stop'
$repo    = Split-Path -Parent $PSScriptRoot
$work    = 'C:\otr'                      # short root (MAX_PATH)
$zip     = Join-Path $work "OpenTouryo-$Ref.zip"
$extract = Join-Path $work "OpenTouryo-$Ref"
$cs      = Join-Path $extract 'root\programs\CS'
$vendor  = Join-Path $repo 'OpenTouryoAssemblies\Build_netcore100'
$needRichClient = $false                 # MVC_Sample_Core (Web) -> false

# --- 1. ZIP acquisition ---
New-Item -ItemType Directory -Force -Path $work | Out-Null
if ($Redownload) { Remove-Item $zip, $extract -Recurse -Force -ErrorAction SilentlyContinue }
if ($Fresh -and (Test-Path $extract)) { Remove-Item $extract -Recurse -Force }
if (-not (Test-Path $cs)) {
    if (-not (Test-Path $zip)) {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -Uri "https://github.com/OpenTouryoProject/OpenTouryo/archive/$Ref.zip" -OutFile $zip
    }
    Expand-Archive -Path $zip -DestinationPath $work -Force
}
$zipHash = (Get-FileHash $zip -Algorithm SHA256).Hash.Substring(0,12)

# --- 2. base build (net10.0 bats only) ---
Push-Location $cs
try {
    cmd /c ".\2_Build_NuGet_netcore100.bat    < nul"
    if ($LASTEXITCODE -ne 0) { throw "2_Build_NuGet_netcore100 failed ($LASTEXITCODE)" }
    cmd /c ".\3_Build_Business_netcore100.bat < nul"
    if ($LASTEXITCODE -ne 0) { throw "3_Build_Business_netcore100 failed ($LASTEXITCODE)" }
    if ($needRichClient) {
        cmd /c ".\3_Build_BusinessRichClient_netcore100.bat < nul"
        if ($LASTEXITCODE -ne 0) { throw "3_Build_BusinessRichClient_netcore100 failed ($LASTEXITCODE)" }
    }
} finally { Pop-Location }

# --- 3. vendor (both TFM subfolders as a whole) ---
$src = Join-Path $cs 'Frameworks\Infrastructure\Build_netcore100'
if (-not (Test-Path $src)) { throw "Build output not found: $src" }
New-Item -ItemType Directory -Force -Path $vendor | Out-Null
Copy-Item -Path (Join-Path $src '*') -Destination $vendor -Recurse -Force
if (-not (Test-Path (Join-Path $vendor 'net10.0\OpenTouryo.Business.dll'))) {
    throw "netcore base build did not produce net10.0\OpenTouryo.Business.dll (check output)."
}
Get-ChildItem (Join-Path $vendor 'net10.0') -Filter 'OpenTouryo.*.dll' | Select-Object -ExpandProperty Name
"{0}`tnetcore100`tref={1}`trichclient={2}`tzip={3}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm'), $Ref, $needRichClient, $zipHash |
    Add-Content -Path (Join-Path $repo 'OpenTouryoAssemblies\build-ref.txt')
