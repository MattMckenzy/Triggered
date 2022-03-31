dotnet build .\TownBulletin\TownBulletin.csproj -c Release /p:DeployOnBuild=true /p:PublishProfile=FolderProfile
dotnet publish .\TownBulletin.Launcher\TownBulletin.Launcher.csproj /p:PublishProfile=FolderProfile

cp ModuleMaker\Modules Releases\win-x64\TownBulletin\Modules

$version = (Get-Item Releases\win-x64\TownBulletin\TownBulletin.exe).VersionInfo.FileVersion
if (Test-Path "Releases\win-x64\${version}.zip") {
    del "Releases\win-x64\${version}.zip"
}

Compress-Archive -Path Releases\win-x64\TownBulletin* -CompressionLevel Optimal -DestinationPath "Releases\win-x64\${version}.zip"
Set-Content Releases\win-x64\latest ${version}

del Releases\win-x64\TownBulletin* -Recurse

git tag -a v${version} -m "Release v${version}"
git add .
git commit -a -m "Release v${version}"
git push