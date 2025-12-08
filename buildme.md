# Developer Build Guide

This guide explains how to set up the development environment and build the Cable Concentricity Calculator application using PowerShell and Visual Studio Code.

## Prerequisites

### Required Software

1. **.NET 9 SDK**
   - Download from: https://dotnet.microsoft.com/download/dotnet/9.0
   - Verify installation: `dotnet --version` (should show 9.x.x)

2. **Visual Studio Code**
   - Download from: https://code.visualstudio.com/
   - Recommended extensions:
     - C# Dev Kit (Microsoft)
     - .NET Install Tool (Microsoft)
     - EditorConfig for VS Code

3. **Git** (optional, for version control)
   - Download from: https://git-scm.com/

### System Requirements

- Windows 10/11, macOS, or Linux
- 4GB RAM minimum (8GB recommended)
- 500MB disk space

## Initial Setup

### 1. Clone or Extract the Project

```powershell
# If using git
git clone <repository-url>
cd CableConcentricityCalculator

# Or extract the zip file and navigate to the directory
cd CableConcentricityCalculator
```

### 2. Open in VS Code

```powershell
code .
```

Or open VS Code and use `File > Open Folder` to select the project directory.

### 3. Restore NuGet Packages

Open a terminal in VS Code (`` Ctrl+` ``) or PowerShell and run:

```powershell
dotnet restore
```

This downloads all required NuGet packages:
- QuestPDF (PDF generation)
- SkiaSharp (Graphics/visualization)
- Spectre.Console (Console UI)
- System.Text.Json (JSON serialization)

## Building the Application

### Debug Build

```powershell
dotnet build
```

Output location: `CableConcentricityCalculator/bin/Debug/net9.0/`

### Release Build

```powershell
dotnet build -c Release
```

Output location: `CableConcentricityCalculator/bin/Release/net9.0/`

### Build and Run

```powershell
dotnet run --project CableConcentricityCalculator
```

### Run with Arguments

```powershell
# Demo mode
dotnet run --project CableConcentricityCalculator -- --demo

# Load a configuration
dotnet run --project CableConcentricityCalculator -- --load Samples/sample-7-conductor.json

# Show help
dotnet run --project CableConcentricityCalculator -- --help
```

## Publishing

### Self-Contained Executable (Windows)

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish/win-x64
```

### Self-Contained Executable (Linux)

```powershell
dotnet publish -c Release -r linux-x64 --self-contained true -o ./publish/linux-x64
```

### Self-Contained Executable (macOS)

```powershell
dotnet publish -c Release -r osx-x64 --self-contained true -o ./publish/osx-x64
```

### Framework-Dependent (requires .NET runtime on target)

```powershell
dotnet publish -c Release -o ./publish/framework-dependent
```

### Single-File Executable

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish/single-file
```

## Project Structure

```
CableConcentricityCalculator/
├── CableConcentricityCalculator.sln    # Solution file
├── readme.md                            # User documentation
├── buildme.md                           # This file
├── Samples/                             # Sample configuration files
│   ├── sample-7-conductor.json
│   └── sample-19-conductor.json
└── CableConcentricityCalculator/        # Main project
    ├── CableConcentricityCalculator.csproj
    ├── Program.cs                       # Entry point & console UI
    ├── Models/                          # Data models
    │   ├── Cable.cs                     # Cable & CableType
    │   ├── CableCore.cs                 # Individual core definition
    │   ├── CableLayer.cs                # Layer & twist configuration
    │   ├── CableAssembly.cs             # Complete assembly model
    │   ├── HeatShrink.cs                # Heat shrink tubing
    │   └── OverBraid.cs                 # Over-braid shielding
    ├── Services/                        # Business logic
    │   ├── ConcentricityCalculator.cs   # Geometry calculations
    │   └── ConfigurationService.cs      # JSON save/load
    ├── Visualization/                   # Graphics
    │   └── CableVisualizer.cs           # Cross-section rendering
    └── Reports/                         # Document generation
        └── PdfReportGenerator.cs        # PDF report creation
```

## VS Code Configuration

### Recommended settings.json

Create or update `.vscode/settings.json`:

```json
{
    "editor.formatOnSave": true,
    "editor.defaultFormatter": "ms-dotnettools.csharp",
    "omnisharp.enableRoslynAnalyzers": true,
    "omnisharp.enableEditorConfigSupport": true,
    "dotnet.defaultSolution": "CableConcentricityCalculator.sln"
}
```

### Launch Configuration

Create `.vscode/launch.json`:

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Launch (Interactive)",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/CableConcentricityCalculator/bin/Debug/net9.0/CableConcentricityCalculator.dll",
            "args": [],
            "cwd": "${workspaceFolder}",
            "console": "integratedTerminal"
        },
        {
            "name": "Launch (Demo)",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/CableConcentricityCalculator/bin/Debug/net9.0/CableConcentricityCalculator.dll",
            "args": ["--demo"],
            "cwd": "${workspaceFolder}",
            "console": "integratedTerminal"
        }
    ]
}
```

### Tasks Configuration

Create `.vscode/tasks.json`:

```json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "build",
            "command": "dotnet",
            "type": "process",
            "args": [
                "build",
                "${workspaceFolder}/CableConcentricityCalculator.sln",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:NoSummary"
            ],
            "problemMatcher": "$msCompile",
            "group": {
                "kind": "build",
                "isDefault": true
            }
        },
        {
            "label": "publish",
            "command": "dotnet",
            "type": "process",
            "args": [
                "publish",
                "${workspaceFolder}/CableConcentricityCalculator.sln",
                "-c", "Release"
            ],
            "problemMatcher": "$msCompile"
        },
        {
            "label": "clean",
            "command": "dotnet",
            "type": "process",
            "args": ["clean"],
            "problemMatcher": "$msCompile"
        }
    ]
}
```

## Dependencies

### NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| QuestPDF | 2024.10.2 | PDF document generation |
| SkiaSharp | 2.88.8 | 2D graphics rendering |
| Spectre.Console | 0.49.1 | Rich console UI |
| System.Text.Json | 9.0.0 | JSON serialization |

### Native Dependencies

SkiaSharp requires native libraries which are automatically included:
- Windows: `libSkiaSharp.dll`
- Linux: `libSkiaSharp.so`
- macOS: `libSkiaSharp.dylib`

## Troubleshooting

### SkiaSharp native library not found

**Linux:** Install required dependencies:
```bash
sudo apt-get install libfontconfig1
```

**macOS:** The native library should be automatically included.

**Windows:** Ensure Visual C++ Redistributable is installed.

### QuestPDF license warning

The application uses QuestPDF Community License. This is set in code:
```csharp
QuestPDF.Settings.License = LicenseType.Community;
```

For commercial use, obtain a professional license from https://www.questpdf.com/

### Build errors after package updates

```powershell
dotnet clean
dotnet restore
dotnet build
```

### "Cannot find project" error

Ensure you're in the solution root directory:
```powershell
cd CableConcentricityCalculator
dotnet build CableConcentricityCalculator.sln
```

## Testing

### Manual Testing

1. Run demo mode and verify PDF output:
   ```powershell
   dotnet run --project CableConcentricityCalculator -- --demo
   ```

2. Open generated files in `output/` directory

3. Test loading sample configurations:
   ```powershell
   dotnet run --project CableConcentricityCalculator -- --load Samples/sample-7-conductor.json
   ```

### Adding Unit Tests

To add unit tests, create a test project:

```powershell
dotnet new xunit -n CableConcentricityCalculator.Tests
dotnet sln add CableConcentricityCalculator.Tests
dotnet add CableConcentricityCalculator.Tests reference CableConcentricityCalculator
```

## Extending the Application

### Adding New Cable Types

1. Add enum value to `CableType` in `Models/Cable.cs`
2. Update `CoreBundleDiameter` calculation if needed
3. Add visualization support in `CableVisualizer.cs`

### Adding New Report Sections

1. Create new compose method in `PdfReportGenerator.cs`
2. Call it from `ComposeContent` method
3. Follow QuestPDF fluent API patterns

### Adding Custom Cable Libraries

1. Extend `ConfigurationService.CreateSampleCableLibrary()`
2. Or load from external JSON file

## Performance Notes

- PDF generation is optimized for documents up to ~50 pages
- Cross-section rendering is resolution-independent
- Large assemblies (100+ cables) may take a few seconds to render

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make changes following existing code style
4. Test thoroughly
5. Submit pull request

## License

QuestPDF Community License applies to PDF generation features.
Application code is provided as-is for internal use.
