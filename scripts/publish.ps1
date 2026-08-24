$ErrorActionPreference = "Stop"
$project = "src/PhotoManager.Wpf/PhotoManager.Wpf.csproj"
foreach ($runtime in @("win-x64", "win-arm64")) {
  dotnet publish $project -c Release -r $runtime --self-contained true -o "artifacts/$runtime" `
    -p:PublishSingleFile=true -p:PublishTrimmed=false -p:DebugSymbols=false
  Compress-Archive -Path "artifacts/$runtime/PhotoManager.exe" -DestinationPath "artifacts/PhotoManager-2.0.0-$runtime-portable.zip" -Force
}
