$ErrorActionPreference='Stop'
$hubRoot=Split-Path $PSScriptRoot -Parent
$hubOutput=Join-Path $hubRoot 'artifacts/ProjectHub-win-x64'
dotnet publish (Join-Path $hubRoot 'src/ProjectHub.App/ProjectHub.App.csproj') -c Release -r win-x64 --self-contained true -o $hubOutput
if($LASTEXITCODE -ne 0){throw 'Publication échouée'}
foreach($hubResource in @('ProjectHub.App.pri','MainWindow.xbf','Views/Library/LibraryPage.xbf','Views/Details/ProjectDetailsView.xbf')) {
    if(!(Test-Path -LiteralPath (Join-Path $hubOutput $hubResource))){throw "Ressource WinUI absente : $hubResource"}
}
$hubArchive=Join-Path $hubRoot 'artifacts/ProjectHub-win-x64.zip'
Compress-Archive -Path (Join-Path $hubOutput '*') -DestinationPath $hubArchive -Force
Write-Output $hubOutput
Write-Output $hubArchive
