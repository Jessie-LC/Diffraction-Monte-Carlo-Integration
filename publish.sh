#cd "Diffraction Monte-Carlo Integration.UI"
#export VCTargetsPath='C:\Program Files\Microsoft Visual Studio\2022\Community\Msbuild\Microsoft\VC\v170\'
"C:\Program Files\Microsoft Visual Studio\2022\Community\Msbuild\Current\Bin\msbuild.exe" -restore -t:Publish -p:PublishProfile=FolderProfile
#dotnet build "Diffraction Monte-Carlo Integration.UI\Diffraction Monte-Carlo Integration.UI.csproj" -c Release
