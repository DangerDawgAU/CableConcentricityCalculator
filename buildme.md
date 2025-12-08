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
     - Avalonia for VS Code (for GUI development)

3. **Git** (optional, for version control)
   - Download from: https://git-scm.com/

### System Requirements

- Windows 10/11, macOS, or Linux
- 4GB RAM minimum (8GB recommended)
- 1GB disk space

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

```powershell
dotnet restore
```

This downloads all required NuGet packages for both projects.

## Project Structure

```
CableConcentricityCalculator/
├── CableConcentricityCalculator.sln        # Solution file
├── readme.md                                # User documentation
├── buildme.md                               # This file
├── Samples/                                 # Sample configuration files
│   ├── sample-7-conductor.json
│   └── sample-19-conductor.json
├── .vscode/                                 # VS Code configuration
│   ├── launch.json
│   └── tasks.json
├── CableConcentricityCalculator/            # Console application
│   ├── CableConcentricityCalculator.csproj
│   ├── Program.cs                           # Console entry point
│   ├── Models/                              # Data models (shared)
│   ├── Services/                            # Business logic
│   ├── Visualization/                       # Cross-section rendering
│   └── Reports/                             # PDF generation
└── CableConcentricityCalculator.Gui/        # GUI application
    ├── CableConcentricityCalculator.Gui.csproj
    ├── Program.cs                           # GUI entry point
    ├── App.axaml                            # Application definition
    ├── Views/                               # UI views (XAML)
    │   └── MainWindow.axaml
    ├── ViewModels/                          # MVVM view models
    ├── Converters/                          # Value converters
    └── Styles/                              # Application styles
```

## Building the Application

### Build All Projects

```powershell
dotnet build
```

### Build GUI Only

```powershell
dotnet build CableConcentricityCalculator.Gui
```

### Build Console Only

```powershell
dotnet build CableConcentricityCalculator
```

### Release Build

```powershell
dotnet build -c Release
```

## Running the Applications

### Run GUI Application

```powershell
dotnet run --project CableConcentricityCalculator.Gui
```

### Run Console Application

```powershell
# Interactive mode
dotnet run --project CableConcentricityCalculator

# Demo mode
dotnet run --project CableConcentricityCalculator -- --demo

# Load a configuration
dotnet run --project CableConcentricityCalculator -- --load Samples/sample-7-conductor.json
```

## Publishing

### GUI Application

#### Windows (Self-Contained)

```powershell
dotnet publish CableConcentricityCalculator.Gui -c Release -r win-x64 --self-contained true -o ./publish/gui-win-x64
```

#### Linux (Self-Contained)

```powershell
dotnet publish CableConcentricityCalculator.Gui -c Release -r linux-x64 --self-contained true -o ./publish/gui-linux-x64
```

#### macOS (Self-Contained)

```powershell
dotnet publish CableConcentricityCalculator.Gui -c Release -r osx-x64 --self-contained true -o ./publish/gui-osx-x64
```

### Console Application

#### Windows

```powershell
dotnet publish CableConcentricityCalculator -c Release -r win-x64 --self-contained true -o ./publish/console-win-x64
```

#### Single-File Executable

```powershell
dotnet publish CableConcentricityCalculator.Gui -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish/gui-single
```

## VS Code Configuration

The project includes pre-configured VS Code settings:

### Launch Configurations (F5)

- **Launch GUI** - Runs the graphical application
- **Launch Console (Interactive)** - Runs console in interactive mode
- **Launch Console (Demo)** - Runs console with demo assembly

### Build Tasks (Ctrl+Shift+B)

- **build-gui** - Build GUI project (default)
- **build** - Build entire solution
- **build-release** - Build in Release mode
- **run-gui** - Build and run GUI
- **publish-gui-win** - Publish for Windows
- **publish-gui-linux** - Publish for Linux

## Dependencies

### Console Application

| Package | Version | Purpose |
|---------|---------|---------|
| QuestPDF | 2024.10.2 | PDF document generation |
| SkiaSharp | 2.88.8 | 2D graphics rendering |
| Spectre.Console | 0.49.1 | Rich console UI |
| System.Text.Json | 9.0.0 | JSON serialization |

### GUI Application

| Package | Version | Purpose |
|---------|---------|---------|
| Avalonia | 11.2.1 | Cross-platform UI framework |
| Avalonia.Desktop | 11.2.1 | Desktop platform support |
| Avalonia.Themes.Fluent | 11.2.1 | Fluent design theme |
| Avalonia.ReactiveUI | 11.2.1 | Reactive extensions |
| CommunityToolkit.Mvvm | 8.3.2 | MVVM toolkit |

## Architecture

### MVVM Pattern (GUI)

The GUI follows the Model-View-ViewModel pattern:

- **Models** - Shared with console app (`CableAssembly`, `Cable`, etc.)
- **Views** - XAML UI definitions (`MainWindow.axaml`)
- **ViewModels** - UI logic and state (`MainWindowViewModel.cs`)
- **Converters** - Value transformations for bindings

### Shared Components

Both applications share:
- Data models (`CableConcentricityCalculator.Models`)
- Calculation engine (`ConcentricityCalculator`)
- Visualization (`CableVisualizer`)
- PDF generation (`PdfReportGenerator`)
- Configuration handling (`ConfigurationService`)

## Troubleshooting

### Avalonia/GUI Issues

**Linux - Missing dependencies:**
```bash
sudo apt-get install libfontconfig1 libice6 libsm6 libx11-6 libxext6
```

**macOS - App not opening:**
Ensure you have the correct runtime:
```bash
dotnet --list-runtimes
```

### SkiaSharp Issues

**Missing native library:**
- Windows: Install Visual C++ Redistributable
- Linux: `sudo apt-get install libfontconfig1`
- macOS: Native library included automatically

### Build Errors

```powershell
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

### QuestPDF License

The application uses QuestPDF Community License:
```csharp
QuestPDF.Settings.License = LicenseType.Community;
```

## Extending the Application

### Adding New Views (GUI)

1. Create XAML file in `Views/` folder
2. Create corresponding ViewModel in `ViewModels/`
3. Register in navigation if needed

### Adding New Cable Types

1. Add enum value to `CableType` in `Models/Cable.cs`
2. Update visualization in `CableVisualizer.cs`
3. Update GUI cable library in `MainWindowViewModel.cs`

### Adding Custom Cable Libraries

1. Create JSON file with cable definitions
2. Extend `ConfigurationService.LoadCableLibrary()`
3. Update GUI dropdown binding

### Adding Report Sections

1. Create compose method in `PdfReportGenerator.cs`
2. Call from `ComposeContent` method
3. Follow QuestPDF fluent API patterns

## Testing

### Manual Testing

1. **GUI Testing:**
   ```powershell
   dotnet run --project CableConcentricityCalculator.Gui
   ```
   - Create new assembly
   - Add layers and cables
   - Export PDF and image
   - Save/load configurations

2. **Console Testing:**
   ```powershell
   dotnet run --project CableConcentricityCalculator -- --demo
   ```
   - Verify PDF output in `output/` directory

### Adding Unit Tests

```powershell
dotnet new xunit -n CableConcentricityCalculator.Tests
dotnet sln add CableConcentricityCalculator.Tests
dotnet add CableConcentricityCalculator.Tests reference CableConcentricityCalculator
```

## Performance Notes

- GUI updates cross-section in real-time (debounced)
- PDF generation optimized for documents up to ~50 pages
- Large assemblies (100+ cables) render in 1-2 seconds

## Contributing

1. Fork the repository
2. Create a feature branch
3. Follow existing code style
4. Test both GUI and console applications
5. Submit pull request

## License

- QuestPDF Community License for PDF generation
- Avalonia MIT License for GUI framework
