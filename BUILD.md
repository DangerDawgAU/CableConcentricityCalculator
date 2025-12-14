# Build Instructions

Build procedures for Cable Concentricity Calculator. Produces self-contained executables for Windows, macOS, and Linux.

## Prerequisites

### Required
- .NET 9.0 SDK ([download](https://dotnet.microsoft.com/download/dotnet/9.0))
- Python 3.6+ (for automated builds)

### Verification
```bash
dotnet --version    # Should show 9.x.x
python --version    # Should show 3.6 or higher
```

## Quick Build

### Automated (All Platforms)
```bash
python build.py
```

### Automated (Single Platform)
```bash
python build.py --target win-x64
python build.py --target linux-x64
python build.py --target osx-x64
python build.py --target osx-arm64
```

### Manual (Development)
```bash
# Build all projects
dotnet build

# Build specific project
dotnet build CableConcentricityCalculator.Gui
dotnet build CableConcentricityCalculator

# Release configuration
dotnet build -c Release
```

## Running During Development

### GUI Application
```bash
dotnet run --project CableConcentricityCalculator.Gui
```

### Console Application
```bash
# Interactive mode
dotnet run --project CableConcentricityCalculator

# Demo mode
dotnet run --project CableConcentricityCalculator -- --demo

# Load configuration
dotnet run --project CableConcentricityCalculator -- --load Samples/sample-7-conductor.json
```

## Production Builds

### Build Script Options
```bash
# Clean build
python build.py --clean

# Debug configuration
python build.py --configuration Debug

# Specific platform with clean
python build.py --clean --target win-x64 --configuration Release
```

### Output Locations
```
Publish/Release/
├── CableConcentricityCalculator_win_x64.exe        (~80-100 MB)
├── CableConcentricityCalculator_linux_x64          (~80-100 MB)
├── CableConcentricityCalculator_osx_x64            (~70-90 MB)
└── CableConcentricityCalculator_osx_arm64          (~70-90 MB)
```

All executables are self-contained (include .NET runtime and dependencies) and require no supporting files.

## Manual Production Builds

### Windows x64
```bash
dotnet publish CableConcentricityCalculator.Gui/CableConcentricityCalculator.Gui.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  --output publish/win-x64 \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=false \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true
```

### Linux x64
```bash
dotnet publish CableConcentricityCalculator.Gui/CableConcentricityCalculator.Gui.csproj \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained true \
  --output publish/linux-x64 \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=false \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true
```

### macOS x64 (Intel)
```bash
dotnet publish CableConcentricityCalculator.Gui/CableConcentricityCalculator.Gui.csproj \
  --configuration Release \
  --runtime osx-x64 \
  --self-contained true \
  --output publish/osx-x64 \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=false \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true
```

### macOS ARM64 (Apple Silicon)
```bash
dotnet publish CableConcentricityCalculator.Gui/CableConcentricityCalculator.Gui.csproj \
  --configuration Release \
  --runtime osx-arm64 \
  --self-contained true \
  --output publish/osx-arm64 \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=false \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true
```

## Deployment

All executables are located in `Publish/Release/` with architecture-specific naming.

### Windows
Distribute `CableConcentricityCalculator_win_x64.exe`. No installation or supporting files required. Users may need to approve security warning on first run (right-click > Properties > Unblock).

### macOS
1. Distribute `CableConcentricityCalculator_osx_x64` (Intel) or `CableConcentricityCalculator_osx_arm64` (Apple Silicon)
2. Grant execution permission:
   ```bash
   chmod +x CableConcentricityCalculator_osx_x64
   # or
   chmod +x CableConcentricityCalculator_osx_arm64
   ```
3. First run: Right-click > Open (to bypass Gatekeeper)

### Linux
1. Distribute `CableConcentricityCalculator_linux_x64`
2. Grant execution permission:
   ```bash
   chmod +x CableConcentricityCalculator_linux_x64
   ```
3. Install dependencies (Debian/Ubuntu):
   ```bash
   sudo apt-get install libfontconfig1 libice6 libsm6 libx11-6 libxext6
   ```

## Dependencies

### Core Library (CableConcentricityCalculator)
| Package | Version | Purpose |
|---------|---------|---------|
| QuestPDF | 2024.10.2 | PDF document generation |
| SkiaSharp | 2.88.8 | 2D graphics rendering |
| System.Text.Json | 9.0.0 | JSON serialisation |
| Spectre.Console | 0.49.1 | Console UI |

### GUI Application (CableConcentricityCalculator.Gui)
| Package | Version | Purpose |
|---------|---------|---------|
| Avalonia | 11.2.1 | Cross-platform UI framework |
| Avalonia.Desktop | 11.2.1 | Desktop platform support |
| Avalonia.Themes.Fluent | 11.2.1 | Fluent design theme |
| Avalonia.ReactiveUI | 11.2.1 | Reactive MVVM extensions |
| CommunityToolkit.Mvvm | 8.3.2 | MVVM toolkit |

All dependencies are automatically included in self-contained builds.

## Troubleshooting

### Build Failures

**SDK not found**
```bash
# Install .NET 9 SDK from https://dotnet.microsoft.com/download/dotnet/9.0
dotnet --version
```

**Python not found**
```bash
# Windows: Install from https://www.python.org/downloads/
# Linux: sudo apt-get install python3
# macOS: brew install python3
python --version
```

**Permission denied (Linux/macOS)**
```bash
chmod +x build.py
./build.py
```

**Disk space errors**
- Self-contained builds require ~500MB free space per platform
- Clear previous builds: `python build.py --clean`

### Runtime Errors

**SkiaSharp native library missing**
- Windows: Install Visual C++ Redistributable
- Linux: `sudo apt-get install libfontconfig1`
- macOS: Native library included automatically

**Avalonia display issues (Linux)**
```bash
sudo apt-get install libfontconfig1 libice6 libsm6 libx11-6 libxext6
```

**QuestPDF licence exception**
- Community Licence is configured in code
- For commercial deployment, verify QuestPDF licence requirements

### Large Executable Size
Self-contained executables include the .NET runtime, Avalonia UI, SkiaSharp, and all dependencies. This is normal for self-contained applications. To reduce size:

**Enable trimming (may cause runtime issues):**
```bash
-p:PublishTrimmed=true
```

**Framework-dependent build (requires .NET runtime installed):**
```bash
dotnet publish --self-contained false
```

## Build Optimisation

### Parallel Builds
```bash
dotnet build -m    # Use all CPU cores
```

### Clean Builds
```bash
# Remove all build artefacts
dotnet clean

# Restore NuGet packages
dotnet restore

# Clean and rebuild
dotnet clean && dotnet build
```

### Build Verification
```bash
# Run tests (if present)
dotnet test

# Verify executables
ls -lh Publish/Release/CableConcentricityCalculator_*
```

## CI/CD Integration

### GitHub Actions
```yaml
- name: Build all platforms
  run: python build.py --clean --configuration Release
```

### GitLab CI
```yaml
build:
  script:
    - python build.py --clean --configuration Release
```

## Licence Compliance

Verify licence requirements before commercial deployment:
- QuestPDF: Community Licence (free for non-commercial use)
- Avalonia: MIT Licence
- SkiaSharp: MIT Licence

For commercial use, review QuestPDF Professional Licence at [questpdf.com](https://www.questpdf.com).
