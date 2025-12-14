# Cable Concentricity Calculator

Production-grade application for designing concentrically twisted cable harness assemblies. Calculates dimensions, generates technical documentation, and produces manufacturing-ready outputs.

## Overview

Calculates and visualises cable assemblies with multiple concentric layers of twisted conductors. Outputs include cross-sectional diagrams, 3D models (STL), and complete technical reports (PDF).

### Applications
- Multi-layer cable harness design
- Electrical connector assembly specifications
- Manufacturing documentation generation
- Concentricity and filler wire calculations
- Heat shrink and over-braid sizing

## System Requirements

- .NET 9.0 Runtime
- Windows 10/11, macOS 10.14+, or Linux (x64/ARM64)
- 4GB RAM minimum
- 1GB disk space

## Quick Start

### GUI Application
```bash
dotnet run --project CableConcentricityCalculator.Gui
```

### Console Application (Scripting)
```bash
# Interactive mode
dotnet run --project CableConcentricityCalculator

# Demo assembly
dotnet run --project CableConcentricityCalculator -- --demo

# Load existing design
dotnet run --project CableConcentricityCalculator -- --load path/to/assembly.json
```

## Architecture

### Project Structure
```
CableConcentricityCalculator/
├── CableConcentricityCalculator/        # Core library and console application
│   ├── Models/                          # Data models (Cable, CableAssembly, CableLayer, etc.)
│   ├── Services/                        # Calculation engine and component libraries
│   │   ├── ConcentricityCalculator.cs   # Core packing and validation algorithms
│   │   ├── CableLibrary.cs              # Cable component database
│   │   ├── HeatShrinkService.cs         # Heat shrink selection logic
│   │   ├── OverBraidService.cs          # Over-braid/sleeving selection
│   │   └── ConfigurationService.cs      # JSON persistence
│   ├── Visualization/                   # Rendering engines
│   │   ├── CableVisualizer.cs           # 2D cross-sections
│   │   ├── InteractiveVisualizer.cs     # Hit-testing for UI selection
│   │   ├── Cable3DVisualizer.cs         # 3D isometric projections
│   │   ├── Cable3DSTLVisualizer.cs      # STL file generation
│   │   └── LayLengthVisualizer.cs       # Lay length diagrams
│   ├── Reports/                         # PDF generation
│   │   └── PdfReportGenerator.cs        # QuestPDF-based report builder
│   └── Libraries/                       # Component databases (JSON)
│       ├── CableLibrary.json            # Cable specifications
│       ├── HeatShrinkLibrary.json       # Heat shrink tubing catalogue
│       └── OverBraidLibrary.json        # Sleeving and EMI shielding
└── CableConcentricityCalculator.Gui/    # Cross-platform GUI (Avalonia)
    ├── Views/                           # UI definitions (AXAML)
    ├── ViewModels/                      # MVVM state management
    └── Converters/                      # Data binding utilities
```

### Component Libraries

All component specifications are stored as JSON files in `Libraries/`. This enables non-code updates to the cable, heat shrink, and over-braid catalogues.

**Included Libraries:**
- MIL-SPEC wires (M22759, gauges 16-26 AWG)
- Multi-core industrial cables (OLFLEX, 2-25 cores)
- Shielded twisted pairs (foil/braid)
- Coaxial cables (RG-174, RG-178, RG-316)
- DR-25 heat shrink series (1.2mm to 101.6mm)
- Expandable sleeving (MDPC-X, Techflex)
- EMI/RFI shielding braids (tinned copper)

See [Libraries/README.md](CableConcentricityCalculator/Libraries/README.md) for JSON schema and extension procedures.

## GUI Workflow

### Interface Layout
1. **Left Panel**: Layer management and cable selection
2. **Centre Panel**: Real-time cross-section or 3D isometric view
3. **Right Panel**: Properties, dimensions, heat shrinks, over-braids, annotations

### Design Process
1. Create new assembly (Ctrl+N)
2. Add cables to centre layer (Layer 0)
3. Configure twist direction and lay length
4. Add concentric layers as required
5. Optimise filler wire count
6. Apply heat shrink and over-braid
7. Validate assembly
8. Export PDF report (Ctrl+E) or cross-section image

### Keyboard Shortcuts
| Key | Action |
|-----|--------|
| Ctrl+N | New Assembly |
| Ctrl+O | Open Assembly |
| Ctrl+S | Save Assembly |
| Ctrl+Shift+S | Save As |
| Ctrl+E | Export PDF |

## Console Application

The console interface provides menu-driven access to all core functions. Suitable for scripting and automation.

### Command-Line Options
```
CableConcentricityCalculator [options]

Options:
  --demo, -d              Run demonstration with sample assembly
  --load, -l <file>       Load assembly from JSON file
  --help, -h              Show help information
  (no arguments)          Launch interactive mode
```

### Output Files
All outputs are written to `output/` directory:
- `{PartNumber}.json` - Assembly configuration
- `{PartNumber}_Report.pdf` - Technical specification report
- `{PartNumber}_CrossSection.png` - 2D cross-section (1200x1200px)
- `{PartNumber}_3D.stl` - STL model for CAD/3D printing

## Assembly Design

### Layer Structure
Cables are arranged in concentric layers numbered from the centre outward:
```
Layer 0: Centre conductor(s)
Layer 1: First concentric ring (typically 6 cables)
Layer 2: Second ring (typically 12 cables)
Layer N: Nth ring
```

### Twist Direction
Alternating twist directions improve mechanical stability:
- **Right-Hand (S)**: Clockwise rotation when viewed from cable end
- **Left-Hand (Z)**: Counter-clockwise rotation

Recommended pattern: Layer 1 = RH, Layer 2 = LH, Layer 3 = RH, etc.

### Concentricity Formula
Maximum cables in layer N surrounding an inner bundle of diameter D with cable diameter d:
```
Max Cables = π × (D + d) / d
```

### Filler Wires
When cable count does not completely fill a layer, filler wires maintain concentricity. The calculator automatically optimises filler diameter and count using the "Optimise Fillers" function.

## Configuration Files

Assemblies are saved as JSON with the following structure:
```json
{
  "partNumber": "CA-001",
  "revision": "A",
  "name": "7-Conductor Shielded Cable",
  "designer": "Engineer Name",
  "temperatureRating": 200,
  "voltageRating": 600,
  "layers": [
    {
      "layerNumber": 0,
      "cables": [...],
      "twistDirection": "None"
    },
    {
      "layerNumber": 1,
      "cables": [...],
      "twistDirection": "RightHand",
      "layLength": 25.0,
      "fillerCount": 2,
      "fillerDiameter": 1.5,
      "fillerMaterial": "PTFE"
    }
  ],
  "heatShrinks": [...],
  "overBraids": [...],
  "annotations": [...]
}
```

Sample configurations are provided in `Samples/` directory.

## Technical Specifications

### Dependencies
**Core Library:**
- QuestPDF 2024.10.2 (PDF generation, Community Licence)
- SkiaSharp 2.88.8 (2D graphics rendering)
- System.Text.Json 9.0.0 (JSON serialisation)
- Spectre.Console 0.49.1 (Console UI)

**GUI Application:**
- Avalonia 11.2.1 (Cross-platform UI framework, MIT Licence)
- CommunityToolkit.Mvvm 8.3.2 (MVVM infrastructure)

### Performance Characteristics
- Real-time cross-section rendering (< 100ms for typical assemblies)
- PDF generation: ~2 seconds for 50-page reports
- Maximum tested: 100+ cables across 5 layers

## Troubleshooting

### Assembly Validation Errors

**"Assembly has no layers"**
- Add at least one layer with cables using "+ Layer" button

**"Cannot fit X cables in layer"**
- Layer is overfilled. Solutions:
  - Use smaller diameter cables
  - Distribute cables across additional layers
  - Remove excess cables

**"Layer leaves gaps"**
- Add filler wires for proper concentricity:
  - Use "Optimise Fillers" button (GUI) or menu option (Console)
  - Manually configure filler count in layer properties

### Application Startup Issues

**GUI not launching**
```bash
# Verify .NET runtime
dotnet --list-runtimes

# Should show: Microsoft.NETCore.App 9.x.x
```

**Linux: Missing native dependencies**
```bash
sudo apt-get install libfontconfig1 libice6 libsm6 libx11-6 libxext6
```

**macOS: Application cannot be opened**
```bash
# First run: Right-click executable > Open
# Grant execution permission if required
chmod +x CableConcentricityCalculator.Gui
```

### PDF Generation Failures

**Error: "Output directory not accessible"**
- Verify `output/` directory exists and has write permissions
- Check available disk space

**Error: "QuestPDF licence exception"**
- Community Licence is configured in code
- For commercial use, upgrade to QuestPDF Professional Licence

### Component Library Issues

**Components not appearing in GUI**
- Verify JSON files exist in `Libraries/` directory
- Check JSON syntax validity (use online validator)
- Review console output for parsing errors
- Confirm file permissions allow read access

**Custom components not loading**
- Ensure all required fields are present (see Libraries/README.md)
- Property names are case-insensitive
- Restart application after modifying JSON files

### Visualisation Problems

**Cross-section appears blank**
- Verify assembly has at least one layer with cables
- Check console/log for SkiaSharp errors
- On Linux: Install `libfontconfig1` package

**STL export fails**
- Ensure assembly is valid (run Validate first)
- Check write permissions in output directory
- Verify sufficient disk space

## Build and Deployment

See [BUILD.md](BUILD.md) for complete build instructions, including:
- Development environment setup
- Cross-platform compilation
- Self-contained executable generation
- Deployment procedures

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development guidelines, code standards, and submission procedures.

## Licence

This application uses the following third-party components:
- QuestPDF Community Licence (PDF generation)
- Avalonia MIT Licence (GUI framework)
- SkiaSharp MIT Licence (Graphics rendering)

For commercial deployment, verify licence compliance requirements.
