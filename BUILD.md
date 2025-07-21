# Build Commands

## Development Build
```bash
dotnet build
```

## Self-Contained Release Builds
```bash
# Windows x64 (single executable, no dependencies required)
dotnet publish -r win-x64 -c Release

# Linux x64 (single executable, no dependencies required) 
dotnet publish -r linux-x64 -c Release

# macOS x64 (single executable, no dependencies required)
dotnet publish -r osx-x64 -c Release

# macOS ARM64 (Apple Silicon)
dotnet publish -r osx-arm64 -c Release
```

Output locations:
- `bin/Release/net8.0/{runtime}/publish/QueryPush.exe` (Windows)
- `bin/Release/net8.0/{runtime}/publish/QueryPush` (Linux/macOS)

## Project Configuration

The following properties ensure single-file, self-contained deployment:

- `PublishSingleFile=true` - Bundles into single executable
- `SelfContained=true` - Includes .NET runtime
- `PublishTrimmed=true` - Removes unused code (smaller size)
- `Version=1.0.0` - Assembly version metadata
