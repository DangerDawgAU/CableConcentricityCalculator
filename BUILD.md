# Build Instructions

This document describes how to build self-contained executables for Cable Concentricity Calculator.

## Prerequisites

- .NET 9.0 SDK or later
- Python 3.6 or later

## Quick Start

### All Platforms (Python)

```bash
python build.py
```

or on Linux/macOS:

```bash
./build.py
```

## Build Options

### Basic Usage

```bash
# Build all platforms
python build.py

# Clean build
python build.py --clean

# Debug configuration
python build.py --configuration Debug

# Build specific platform only
python build.py --target win-x64
python build.py --target osx-x64
python build.py --target osx-arm64
python build.py --target linux-x64

# Combined options
python build.py --clean --configuration Release
```

### Available Arguments

| Argument | Description | Values |
|----------|-------------|--------|
| `--clean` | Clean previous builds before building | Flag |
| `--configuration` | Build configuration | `Debug`, `Release` (default) |
| `--target` | Build specific platform only | `win-x64`, `osx-x64`, `osx-arm64`, `linux-x64` |
| `--skip-tests` | Skip running tests (not implemented) | Flag |

### Examples

```bash
# Release build for Windows only
python build.py --target win-x64

# Clean debug build for all platforms
python build.py --clean --configuration Debug

# Build macOS Apple Silicon only
python build.py --target osx-arm64
```

## Output

Executables are published to the `publish/` directory:

```
publish/
├── win-x64/                  # Windows x64
│   └── CableConcentricityCalculator.Gui.exe
├── osx-x64/                  # macOS Intel
│   └── CableConcentricityCalculator.Gui
├── osx-arm64/                # macOS Apple Silicon
│   └── CableConcentricityCalculator.Gui
└── linux-x64/                # Linux x64
    └── CableConcentricityCalculator.Gui
```

## Platform-Specific Builds (Manual)

To build for a specific platform using the dotnet CLI directly:

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

## Deployment

The generated executables are self-contained and include all necessary dependencies. Users do not need to install .NET runtime to run them.

### Windows
Simply distribute the `.exe` file from `publish/win-x64/`

### macOS
1. Distribute the executable from `publish/osx-x64/` or `publish/osx-arm64/`
2. Users may need to grant execution permissions:
   ```bash
   chmod +x CableConcentricityCalculator.Gui
   ```
3. On first run, macOS may show a security warning. Users should right-click and select "Open"

### Linux
1. Distribute the executable from `publish/linux-x64/`
2. Users may need to grant execution permissions:
   ```bash
   chmod +x CableConcentricityCalculator.Gui
   ```

## Troubleshooting

### Build fails with "SDK not found"
Ensure .NET 9.0 SDK is installed:
```bash
dotnet --version
```

### Python not found
Ensure Python 3.6+ is installed:
```bash
python --version
# or
python3 --version
```

### Permission denied on Linux/macOS
Make the build script executable:
```bash
chmod +x build.py
```

### Large executable size
The executables are self-contained and include the .NET runtime, SkiaSharp, Avalonia UI, and all dependencies. This is normal for self-contained applications.

Typical sizes:
- Windows: ~80-100 MB
- macOS: ~70-90 MB
- Linux: ~80-100 MB

To reduce size, you can enable trimming (may cause runtime issues):
```bash
-p:PublishTrimmed=true
```

## CI/CD Integration

The build script can be integrated into CI/CD pipelines:

### GitHub Actions Example
```yaml
- name: Build all platforms
  run: python build.py --clean --configuration Release
```

### GitLab CI Example
```yaml
build:
  script:
    - python build.py --clean --configuration Release
```

## Features

- ✅ Cross-platform (Windows, macOS, Linux)
- ✅ Single-file executables
- ✅ Self-contained (includes .NET runtime)
- ✅ Compressed for smaller size
- ✅ Color-coded console output
- ✅ Shows executable size after build
- ✅ Clean option to remove previous builds
- ✅ Target-specific builds
- ✅ Progress indicators
