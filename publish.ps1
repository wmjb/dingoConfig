# Extract version from api.csproj
$version = Select-String -Path "api/api.csproj" -Pattern "<Version>(.*?)</Version>" |
    ForEach-Object { $_.Matches.Groups[1].Value }

if (-not $version) {
    Write-Error "Error: Could not extract version from api/api.csproj"
    exit 1
}

Write-Host "Publishing dingoConfig version $version" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# Create output directory
$outputDir = "publish/dingoConfig-$version"
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

# Common publish arguments
$project = "api/api.csproj"
$config = "Release"
$publishArgs = "--self-contained", "true", "-p:PublishSingleFile=true", "-p:IncludeNativeLibrariesForSelfExtract=true"

# Publish for Windows (x64)
Write-Host ""
Write-Host "Building for Windows (x64)..." -ForegroundColor Yellow
dotnet publish $project -c $config -r win-x64 @publishArgs -o "$outputDir/win-x64"
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Windows build complete" -ForegroundColor Green
} else {
    Write-Host "✗ Windows build failed" -ForegroundColor Red
}

# Publish for Linux (x64)
Write-Host ""
Write-Host "Building for Linux (x64)..." -ForegroundColor Yellow
dotnet publish $project -c $config -r linux-x64 @publishArgs -o "$outputDir/linux-x64"
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Linux build complete" -ForegroundColor Green
} else {
    Write-Host "✗ Linux build failed" -ForegroundColor Red
}

# Publish for macOS (x64 - Intel)
Write-Host ""
Write-Host "Building for macOS (x64 - Intel)..." -ForegroundColor Yellow
dotnet publish $project -c $config -r osx-x64 @publishArgs -o "$outputDir/osx-x64"
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ macOS Intel build complete" -ForegroundColor Green
} else {
    Write-Host "✗ macOS Intel build failed" -ForegroundColor Red
}

# Publish for macOS (arm64 - Apple Silicon)
Write-Host ""
Write-Host "Building for macOS (arm64 - Apple Silicon)..." -ForegroundColor Yellow
dotnet publish $project -c $config -r osx-arm64 @publishArgs -o "$outputDir/osx-arm64"
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ macOS Apple Silicon build complete" -ForegroundColor Green
} else {
    Write-Host "✗ macOS Apple Silicon build failed" -ForegroundColor Red
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "All builds complete!" -ForegroundColor Green
Write-Host "Output directory: $outputDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "Executables:" -ForegroundColor Cyan
Write-Host "  Windows:               $outputDir/win-x64/dingoConfig.exe"
Write-Host "  Linux:                 $outputDir/linux-x64/dingoConfig"
Write-Host "  macOS (Intel):         $outputDir/osx-x64/dingoConfig"
Write-Host "  macOS (Apple Silicon): $outputDir/osx-arm64/dingoConfig"
