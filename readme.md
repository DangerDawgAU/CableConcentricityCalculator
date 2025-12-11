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
- **Visual Cross-Section**: Real-time visual cross-section diagrams of your cable assembly with interactive element selection
- **3D Isometric View**: Generate 3D isometric projections and STL files for manufacturing
- **PDF Report Generation**: Create professional PDF reports with all assembly specifications, cross-sections, and lay length diagrams
- **Heat Shrink Support**: Comprehensive heat shrink library (DR-25 series) with automatic sizing and detailed specifications
- **Over-Braid Configuration**: Add EMI shielding braids and expandable sleeves with coverage specifications
- **JSON-Based Libraries**: Extensible cable, heat shrink, and over-braid libraries stored in JSON files for easy customization
- **Bill of Materials**: Automatic BOM generation from assembly configuration
- **JSON Import/Export**: Save and load designs for reuse and modification
- **Interactive Annotations**: Add balloon callouts and notes with reference markers
- **Cable Browser**: Advanced cable selection dialog with filtering by type, manufacturer, and core count

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
   - View and manage cable layers with visual indicators
   - Add/remove cables from selected layer
   - "Add Cable..." button opens advanced cable browser dialog with:
     - Filter by cable type, manufacturer, core count, and gauge
     - Live search across cable library
     - Quick preview of cable specifications
   - Visual layer badges showing cable/conductor counts

2. **Center Panel - Visualization**
   - **Cross-Section View**: Real-time 2D cross-section diagram
     - Interactive - click elements to select and view details
     - Color-coded cables by jacket color
     - Shows heat shrink, over-braids, and annotations
   - **3D Isometric View**: Perspective view of cable assembly
     - Automatically generates STL files for 3D printing/CAD
     - Shows length and layup structure
   - Toggle between views with tabs
   - Displays validation warnings if present

3. **Right Panel - Properties**
   - **Assembly Properties**: Part number, revision, name, designer, temperature/voltage ratings
   - **Dimensions** (calculated): Overall diameter, core bundle diameter, cable/conductor/filler counts
   - **Layer Properties**: Twist direction, lay length, filler count/diameter/material
   - **Heat Shrinks**:
     - Dropdown selector with all DR-25 sizes (black and clear)
     - "Add Selected Heat Shrink" button
     - List of applied heat shrinks with specifications
     - Shows shrink ratios, inner diameters, and wall thicknesses
   - **Over-Braids**:
     - Dropdown selector with expandable sleeves and EMI braids
     - "Add Selected Over-Braid" button
     - List of applied over-braids with coverage details
     - Shows diameter ranges and shielding capabilities
   - **Annotations**: Add balloon callouts with notes
   - **Notes**: Free-form text field for design notes

### Workflow

1. **Create New Assembly**: File → New or Ctrl+N
2. **Add First Layer**: Click "+ Layer" button (center layer is Layer 0)
3. **Add Cables**:
   - Click "Add Cable..." button in left panel
   - Use filters to find cables by type, manufacturer, core count, or gauge
   - Select cable and click "Add to Layer"
4. **Configure Layer**: Set twist direction, lay length, fillers in right panel
5. **Add More Layers**: Repeat for each concentric layer
6. **Add Heat Shrink** (optional):
   - Select heat shrink from dropdown in right panel
   - Click "Add Selected Heat Shrink"
   - Application automatically suggests appropriate sizes
7. **Add Over-Braids** (optional):
   - Select over-braid/sleeving from dropdown
   - Click "Add Selected Over-Braid"
8. **Add Annotations** (optional): Click "+" in Annotations section to add notes
9. **Optimize Fillers**: Click "Optimize Fillers" to automatically calculate filler requirements
10. **Validate**: Click "Validate" to check assembly for issues
11. **Export**: File → Export PDF or Export Image

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

## Cable Library System

The application uses a **JSON-based library system** for easy customization and extension. All libraries are stored in the `Libraries/` folder as JSON files.

### Cable Library (`CableLibrary.json`)
Includes comprehensive cable specifications:
- **MIL-SPEC Wires** (M22759): Gauges 16-26 AWG in 10 colors
- **OLFLEX Cables**: Multi-core industrial cables (2-25 cores)
- **Shielded Twisted Pairs**: With foil and braid shielding
- **Coaxial Cables**: RG-series (RG-174, RG-178, RG-316)
- **Custom Cables**: Easily add your own cable definitions

### Heat Shrink Library (`HeatShrinkLibrary.json`)
Complete DR-25 series from TE Connectivity/Raychem:
- **Standard DR-25**: Sizes from 1.2mm to 101.6mm (2:1 shrink ratio)
- **Black and Clear**: Available in both colors for all sizes
- **Adhesive-Lined (DR-25-HM)**: For environmental sealing
- Detailed specifications: Supplied/recovered diameters, wall thicknesses, temperature ratings
- Automatic size suggestions based on cable diameter

### Over-Braid Library (`OverBraidLibrary.json`)
Comprehensive sleeving and shielding options:
- **MDPC-X Sleeving**: Expandable sleeves in multiple colors and sizes
- **Techflex Sleeving**: Clean-cut and expandable braids
- **Tinned Copper Braids**: EMI/RFI shielding braids with various coverage percentages
- **PET Expandable**: Flexible protective sleeving
- Specifications include: Diameter ranges (min/nominal/max), coverage %, material, shielding capability

### Adding Custom Parts

All libraries can be edited directly as JSON files. See [Libraries/README.md](CableConcentricityCalculator/Libraries/README.md) for:
- JSON schema documentation
- Step-by-step instructions for adding custom cables, heat shrinks, and over-braids
- Validation guidelines
- Best practices for library management

## Output Files

The application generates files in the `output/` directory:

| File | Description |
|------|-------------|
| `{PartNumber}.json` | Cable assembly configuration (JSON format) |
| `{PartNumber}_Report.pdf` | Complete specification report with cross-sections and BOM |
| `{PartNumber}_CrossSection.png` | 2D cross-section visualization (1200×1200 pixels) |
| `{PartNumber}_3D.stl` | 3D model in STL format for CAD/3D printing |

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
