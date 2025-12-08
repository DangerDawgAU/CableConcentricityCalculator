# Cable Concentricity Calculator

A comprehensive C# application for designing and visualizing concentrically twisted cable harness assemblies. This tool helps engineers understand cable layups, calculate dimensions, and generate professional PDF reports for cable assembly manufacturing.

**Available in two versions:**
- **GUI Application** - Cross-platform graphical interface (Windows, macOS, Linux)
- **Console Application** - Command-line interface for scripting and automation

## Features

- **Concentric Cable Design**: Design cables with multiple layers of conductors twisted concentrically
- **Multi-Core Support**: Handle single-core and multi-core cables with configurable core counts
- **Flexible Configuration**: Define conductor diameters, insulation, shielding, and jacket properties
- **Filler Wire Calculation**: Automatic calculation of filler wires needed for proper concentricity
- **Visual Cross-Section**: Real-time visual cross-section diagrams of your cable assembly
- **PDF Report Generation**: Create professional PDF reports with all assembly specifications
- **Heat Shrink Support**: Define heat shrink tubing with shrink ratios and wall thicknesses
- **Over-Braid Configuration**: Add EMI shielding braids with coverage specifications
- **Bill of Materials**: Automatic BOM generation from assembly configuration
- **JSON Import/Export**: Save and load designs for reuse and modification

## Quick Start

### GUI Application (Recommended)

Launch the graphical interface:

```powershell
dotnet run --project CableConcentricityCalculator.Gui
```

The GUI provides:
- Visual cable layup editor with real-time cross-section preview
- Drag-and-drop cable management
- Point-and-click layer configuration
- One-click PDF and image export
- Built-in cable library browser

### Console Application

#### Demo Mode

To see a sample cable assembly:

```powershell
dotnet run --project CableConcentricityCalculator -- --demo
```

#### Interactive Console Mode

```powershell
dotnet run --project CableConcentricityCalculator
```

#### Load Existing Design

```powershell
dotnet run --project CableConcentricityCalculator -- --load Samples/sample-7-conductor.json
```

## GUI Application Guide

### Main Interface

The GUI is divided into three panels:

1. **Left Panel - Layers & Cables**
   - View and manage cable layers
   - Add/remove cables from selected layer
   - Quick-add buttons for common quantities (1, 6, 12 cables)

2. **Center Panel - Cross-Section View**
   - Real-time visualization of your cable assembly
   - Updates automatically as you make changes
   - Shows validation warnings if present

3. **Right Panel - Properties**
   - Assembly properties (part number, name, ratings)
   - Calculated dimensions (overall diameter, conductor count)
   - Layer properties (twist direction, lay length, fillers)
   - Heat shrink and over-braid management
   - Notes field

### Workflow

1. **Create New Assembly**: File → New or Ctrl+N
2. **Add First Layer**: Click "+ Layer" button
3. **Add Cables**: Select cable from library dropdown, click "Add 1" or "+6"/"+12"
4. **Configure Layer**: Set twist direction, lay length, fillers in right panel
5. **Add More Layers**: Repeat for each concentric layer
6. **Add Shielding**: Click "+" in Over-Braids section
7. **Validate**: Click "Validate" to check assembly
8. **Export**: File → Export PDF or Export Image

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+N | New Assembly |
| Ctrl+O | Open Assembly |
| Ctrl+S | Save Assembly |
| Ctrl+Shift+S | Save As |
| Ctrl+E | Export PDF |

## Console Interactive Mode Guide

The console mode provides a menu-driven interface:

### Creating a New Assembly

1. Select "Create new cable assembly"
2. Enter the assembly part number and name
3. Add cables to the center layer (Layer 0)
4. Add additional layers as needed

### Adding Cables

**From Library:**
- Select cable types from MIL-SPEC wire library
- Specify quantities for each cable type

**Custom Cable:**
- Define part number and name
- Choose cable type (single-core, multi-core, coaxial, etc.)
- Specify core properties (diameter, insulation, color)
- Add shielding if needed

### Layer Configuration

- **Twist Direction**: Right-Hand (S), Left-Hand (Z), or None
- **Lay Length**: Distance for one complete twist (mm)
- **Fillers**: Add filler wires for proper concentricity
- **Tape Wrap**: Optional PTFE or other tape wrapping

## Cable Assembly Structure

### Layers

Cables are arranged in concentric layers:

```
Layer 0: Center conductor(s)
Layer 1: First ring around center (typically 6 cables)
Layer 2: Second ring (typically 12 cables)
Layer N: Nth ring (approximately 6N cables for equal sizes)
```

### Twist Directions

Alternating twist directions provide stability:

- **Right-Hand (S)**: Clockwise when viewed from end
- **Left-Hand (Z)**: Counter-clockwise when viewed from end

Typically: Layer 1 = RH, Layer 2 = LH, Layer 3 = RH, etc.

### Concentricity Formula

The number of cables that fit in layer N around an inner bundle:

```
Max Cables = π × (Inner Diameter + Cable Diameter) / Cable Diameter
```

## Configuration File Format

Cable assemblies are stored as JSON files:

```json
{
  "partNumber": "CA-001",
  "revision": "A",
  "name": "7-Conductor Shielded Cable",
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
      "layLength": 25
    }
  ],
  "overBraids": [...],
  "heatShrinks": [...],
  "outerJacket": {...}
}
```

See `Samples/` directory for complete examples.

## PDF Report Contents

Generated reports include:

1. **Header**: Part number, revision, name, date, designer
2. **Cross-Section Diagram**: Visual representation of cable layup
3. **Specifications Table**: Dimensions, conductor counts, areas
4. **Layer Structure**: Details of each layer with twist info
5. **Bill of Materials**: Complete parts list with quantities
6. **Cable Specifications**: Detailed info for each cable type
7. **Notes and Warnings**: Design notes and validation warnings

## Cable Library

The built-in library includes:

### MIL-SPEC Wires (M22759)
- Gauges: 16, 18, 20, 22, 24, 26 AWG
- Colors: White, Black, Red, Green, Blue, Yellow, Orange, Brown, Violet, Gray
- Silver-plated copper conductors

### Specialty Cables
- Shielded twisted pairs
- Coaxial cables (RG-178)
- Multi-conductor cables

### Heat Shrink
- DR-25 series (Polyolefin)
- PTFE heat shrink
- Various sizes

### Over-Braids
- Tinned copper braids
- PET expandable sleeving
- Various diameters

## Output Files

The application generates files in the `output/` directory:

| File | Description |
|------|-------------|
| `{PartNumber}.json` | Cable assembly configuration |
| `{PartNumber}_Report.pdf` | Complete specification report |
| `{PartNumber}_CrossSection.png` | Cross-section visualization |

## Tips for Cable Design

1. **Start with Center**: Define the center conductor(s) first
2. **Match Diameters**: Similar cable diameters in each layer work best
3. **Use Fillers**: Add fillers when cable count doesn't fill the layer
4. **Alternate Twist**: Use opposite twist directions for adjacent layers
5. **Validate Often**: Check assembly validity after each change
6. **Standard Lay Lengths**: Typical lay lengths are 6-12× cable diameter

## Command Line Reference (Console App)

```
CableConcentricityCalculator [options]

Options:
  --demo, -d              Run demonstration with sample assembly
  --load, -l <file>       Load assembly from JSON file
  --help, -h              Show help information
  (no arguments)          Launch interactive mode
```

## Troubleshooting

### "Assembly has no layers"
Create at least one layer with cables using "Add Layer".

### "Cannot fit X cables in layer"
The layer is overfilled. Either:
- Use smaller diameter cables
- Move some cables to the next layer
- Remove some cables

### "Layer leaves gaps"
Add filler wires to achieve proper concentricity:
- Use "Optimize Fillers" button/option
- Or manually set filler count in layer properties

### PDF generation fails
Ensure the output directory exists and is writable.

### GUI not launching
Ensure .NET 9 runtime is installed. Try:
```powershell
dotnet --list-runtimes
```

## License

This application uses QuestPDF Community License for PDF generation.

## Support

For questions or issues, refer to the project documentation or contact your cable engineering team.
