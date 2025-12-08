# Cable Concentricity Calculator

A comprehensive C# application for designing and visualizing concentrically twisted cable harness assemblies. This tool helps engineers understand cable layups, calculate dimensions, and generate professional PDF reports for cable assembly manufacturing.

## Features

- **Concentric Cable Design**: Design cables with multiple layers of conductors twisted concentrically
- **Multi-Core Support**: Handle single-core and multi-core cables with configurable core counts
- **Flexible Configuration**: Define conductor diameters, insulation, shielding, and jacket properties
- **Filler Wire Calculation**: Automatic calculation of filler wires needed for proper concentricity
- **Visual Cross-Section**: Generate visual cross-section diagrams of your cable assembly
- **PDF Report Generation**: Create professional PDF reports with all assembly specifications
- **Heat Shrink Support**: Define heat shrink tubing with shrink ratios and wall thicknesses
- **Over-Braid Configuration**: Add EMI shielding braids with coverage specifications
- **Bill of Materials**: Automatic BOM generation from assembly configuration
- **JSON Import/Export**: Save and load designs for reuse and modification

## Quick Start

### Run Demo Mode

To see the application in action with a sample cable assembly:

```powershell
dotnet run --project CableConcentricityCalculator -- --demo
```

This generates:
- A sample 19-conductor cable configuration
- Cross-section visualization (PNG)
- Complete PDF report
- JSON configuration file

### Interactive Mode

Launch the interactive designer:

```powershell
dotnet run --project CableConcentricityCalculator
```

### Load Existing Design

Process a saved cable design and generate a report:

```powershell
dotnet run --project CableConcentricityCalculator -- --load Samples/sample-7-conductor.json output/report.pdf
```

## Interactive Mode Guide

The interactive mode provides a menu-driven interface for cable design:

### Creating a New Assembly

1. Select "Create new cable assembly"
2. Enter the assembly part number and name
3. Add cables to the center layer (Layer 0)
4. Add additional layers as needed

### Adding Cables

You can add cables from the built-in library or define custom cables:

**From Library:**
- Select cable types from MIL-SPEC wire library
- Specify quantities for each cable type
- Cables include pre-defined conductor, insulation, and jacket properties

**Custom Cable:**
- Define part number and name
- Choose cable type (single-core, multi-core, coaxial, etc.)
- Specify core properties (diameter, insulation, color)
- Add shielding if needed
- Set jacket properties

### Layer Configuration

Each layer (except center) has configurable properties:

- **Twist Direction**: Right-Hand (S), Left-Hand (Z), or None
- **Lay Length**: Distance for one complete twist (mm)
- **Fillers**: Add filler wires for proper concentricity
- **Tape Wrap**: Optional PTFE or other tape wrapping

### Adding Outer Components

**Heat Shrink:**
- Select from library or define custom
- Specify shrink ratio and recovered dimensions
- Set material and color

**Over-Braid:**
- Choose shielding or protective braid
- Set coverage percentage
- Define wall thickness

**Outer Jacket:**
- Set material and wall thickness
- Specify color and printing options

### Validation and Optimization

- **Validate Assembly**: Check for geometric feasibility
- **Optimize Fillers**: Automatically calculate filler requirements

### Generating Output

- **Generate PDF Report**: Creates comprehensive specification document
- **Save Assembly**: Export to JSON for later use

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

Cable assemblies are stored as JSON files. Example structure:

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

## Command Line Reference

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
Create at least one layer with cables using the interactive mode.

### "Cannot fit X cables in layer"
The layer is overfilled. Either:
- Use smaller diameter cables
- Move some cables to the next layer
- Remove some cables

### "Layer leaves gaps"
Add filler wires to achieve proper concentricity:
- Use "Optimize fillers" option
- Or manually add fillers in layer configuration

### PDF generation fails
Ensure the output directory exists and is writable.

## License

This application uses QuestPDF Community License for PDF generation.

## Support

For questions or issues, refer to the project documentation or contact your cable engineering team.
