$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
function RunChecked([string]$exe, [string[]]$arguments) {
  & $exe @arguments
  if ($LASTEXITCODE -ne 0) { throw "$exe failed with exit code $LASTEXITCODE" }
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Install .NET 8 SDK first.' }
RunChecked dotnet @('publish','src/FocusGate.Service/FocusGate.Service.csproj','-c','Release','-r','win-x64','--self-contained','true','-p:PublishSingleFile=true','-o','publish/service')
RunChecked dotnet @('publish','src/FocusGate.Desktop/FocusGate.Desktop.csproj','-c','Release','-r','win-x64','--self-contained','true','-p:PublishSingleFile=true','-o','publish/desktop')
if (-not (Get-Command wix -ErrorAction SilentlyContinue)) { RunChecked dotnet @('tool','install','--global','wix','--version','5.0.2') }
RunChecked wix @('build','installer/Package.wxs','-arch','x64','-out','FocusGate.msi')
if (-not (Test-Path 'FocusGate.msi')) { throw 'MSI missing after WiX build.' }
Write-Host 'Built FocusGate.msi' -ForegroundColor Green
