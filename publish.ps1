dotnet build .\Triggered\Triggered.csproj -c Release /p:DeployOnBuild=true /p:PublishProfile=FolderProfile
dotnet publish .\Triggered.Launcher\Triggered.Launcher.csproj /p:PublishProfile=FolderProfile

cp ModuleMaker\Modules Releases\win-x64\Triggered\Modules -Recurse
cp ModuleMaker\Utilities Releases\win-x64\Triggered\Utilities -Recurse

$version = (Get-Item Releases\win-x64\Triggered\Triggered.exe).VersionInfo.FileVersion
if (Test-Path "Releases\win-x64\${version}.zip") {
    del "Releases\win-x64\${version}.zip"
}

Compress-Archive -Path Releases\win-x64\Triggered* -CompressionLevel Optimal -DestinationPath "Releases\win-x64\${version}.zip"
Set-Content latestversion ${version}

del Releases\win-x64\Triggered* -Recurse

git tag -a v${version} -m "Release v${version}"
git add .
git commit -a -m "Release v${version}"
git push