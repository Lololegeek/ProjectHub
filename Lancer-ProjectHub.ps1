$ErrorActionPreference='Stop'
$hubPublished=Join-Path $PSScriptRoot 'artifacts/ProjectHub-win-x64/ProjectHub.App.exe'
$hubBuilt=Join-Path $PSScriptRoot 'src/ProjectHub.App/bin/Release/net10.0-windows10.0.26100.0/win-x64/ProjectHub.App.exe'
if(Test-Path -LiteralPath $hubPublished){Start-Process -FilePath $hubPublished; exit}
if(!(Test-Path -LiteralPath $hubBuilt)){dotnet build (Join-Path $PSScriptRoot 'src/ProjectHub.App/ProjectHub.App.csproj') -c Release; if($LASTEXITCODE -ne 0){throw 'Compilation échouée'}}
Start-Process -FilePath $hubBuilt
