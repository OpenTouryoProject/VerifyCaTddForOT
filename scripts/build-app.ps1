# Build the solutions taken out (only those that exist) against the vendored OpenTouryo DLLs.
# Prereq: scripts\setup-build.ps1 was run once (populates OpenTouryoAssemblies\). ASCII only (WinPS 5.1 safe).
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msb = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
if (-not $msb) { throw "MSBuild not found (install VS Build Tools / Community)" }

$nuget = Join-Path $repo 'tools\nuget.exe'
if (-not (Test-Path $nuget)) {
    $nuget = Get-ChildItem 'C:\otr\OpenTouryo-*\root\programs\nuget.exe' -EA SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
}
if (-not $nuget) { throw "nuget.exe not found (tools\ or C:\otr\OpenTouryo-<ref>\root\programs\)" }

# net48 (packages.config). One line per sln taken out.
foreach ($rel in 'MVC_Sample\MVC_Sample.sln', 'WebForms_Sample\WebForms_Sample.sln') {
    $sln = Join-Path $repo $rel
    if (-not (Test-Path $sln)) { continue }
    & $nuget restore $sln -MSBuildPath (Split-Path $msb)
    if ($LASTEXITCODE -ne 0) { throw "nuget restore failed: $rel ($LASTEXITCODE)" }
    & $msb $sln /p:Configuration=Debug /nologo /v:m
    if ($LASTEXITCODE -ne 0) { throw "build failed: $rel ($LASTEXITCODE)" }
}

# 3-tier rich client (WSClient_*): *_all.sln = client + WS host(WCFService/ASPNETWebService) + WSServer/WSIFType.
# Mixed restore: packages.config (ASPNETWebService needs a project-local packages\ dir) + PackageReference (WCFService, client).
foreach ($rel in 'WS_sample\WSClient_sample\WSClientWin_sample\WSClientWin_sample_all.sln') {
    $sln = Join-Path $repo $rel
    if (-not (Test-Path $sln)) { continue }
    & $nuget restore $sln -MSBuildPath (Split-Path $msb)
    if ($LASTEXITCODE -ne 0) { throw "nuget restore failed: $rel ($LASTEXITCODE)" }
    $asp = Join-Path $repo 'WS_sample\ServiceInterface\ASPNETWebService'
    if (Test-Path (Join-Path $asp 'packages.config')) {
        & $nuget restore (Join-Path $asp 'packages.config') -PackagesDirectory (Join-Path $asp 'packages') -MSBuildPath (Split-Path $msb)
        if ($LASTEXITCODE -ne 0) { throw "nuget restore (ASPNETWebService) failed ($LASTEXITCODE)" }
    }
    & $msb $sln /t:restore /nologo /v:m
    & $msb $sln /p:Configuration=Debug /nologo /v:m
    if ($LASTEXITCODE -ne 0) { throw "build failed: $rel ($LASTEXITCODE)" }
}
# net10.0 (SDK): dotnet build restores. One line per sln taken out.
foreach ($rel in 'MVC_Sample_Core\MVC_Sample.sln') {
    $sln = Join-Path $repo $rel
    if (-not (Test-Path $sln)) { continue }
    dotnet build $sln -nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "build failed: $rel ($LASTEXITCODE)" }
}
# dev tools (HintPath + PackageReference mix -> restore, then build)
foreach ($tool in 'DaoGen_Tool','DPQuery_Tool') {
    $toolSln = Join-Path $repo "OT_Tools\$tool\$tool.sln"
    if (Test-Path $toolSln) {
        & $msb $toolSln /t:restore,build /p:Configuration=Debug /nologo /v:m
        if ($LASTEXITCODE -ne 0) { throw "$tool build failed ($LASTEXITCODE)" }
    }
    # net48 and Core csproj share obj\ -> always restore before build (no --no-restore)
    $coreSln = Join-Path $repo "OT_Tools\$tool\${tool}Core.sln"
    if (Test-Path $coreSln) {
        dotnet build $coreSln -nologo -v q
        if ($LASTEXITCODE -ne 0) { throw "${tool}Core build failed ($LASTEXITCODE)" }
    }
}
Write-Host "Build OK." -ForegroundColor Green
