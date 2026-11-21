param([switch]$FrameworkDependent)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    dotnet run --project tests/CatGuard.Checks -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Core checks failed.' }
    $selfContained = if ($FrameworkDependent) { 'false' } else { 'true' }
    dotnet publish src/CatGuard/CatGuard.csproj -c Release -r win-x64 --self-contained $selfContained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist/WindowsCatGuard-win-x64
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
    Copy-Item LICENSE,THIRD_PARTY_NOTICES.md,README.md dist/WindowsCatGuard-win-x64
    Copy-Item licenses dist/WindowsCatGuard-win-x64/licenses -Recurse -Force
} finally { Pop-Location }
