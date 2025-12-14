# Contributing Guidelines

Technical contribution guidelines for Cable Concentricity Calculator. Assumes experience with C#, .NET, and software engineering principles.

## Code Standards

### Language and Framework
- C# 12 with .NET 9.0
- Enable nullable reference types (`<Nullable>enable</Nullable>`)
- Use implicit usings where appropriate
- Follow standard .NET naming conventions

### Architecture Patterns
- **Shared Core**: Models, Services, Visualisation, and Reports are shared between GUI and Console
- **MVVM**: GUI uses strict Model-View-ViewModel separation (CommunityToolkit.Mvvm)
- **JSON Libraries**: Component databases are JSON files, not hard-coded
- **Separation of Concerns**: Keep calculation logic independent of UI

### Code Organisation
```
Models/           # Data structures only (no business logic)
Services/         # Business logic, calculations, library management
Visualization/    # Rendering engines (SkiaSharp-based)
Reports/          # PDF generation (QuestPDF-based)
Utilities/        # Helper functions and extensions
```

## Development Workflow

### Initial Setup
```bash
git clone <repository-url>
cd CableConcentricityCalculator
dotnet restore
dotnet build
```

### Testing Changes
```bash
# Run GUI application
dotnet run --project CableConcentricityCalculator.Gui

# Run console with demo
dotnet run --project CableConcentricityCalculator -- --demo

# Load sample configuration
dotnet run --project CableConcentricityCalculator -- --load Samples/sample-19-conductor.json
```

### Before Committing
1. Build both projects without errors: `dotnet build`
2. Test GUI and console applications with sample data
3. Verify PDF and image outputs in `output/` directory
4. Remove debug code and console writes
5. Update relevant documentation if public API changes

## Component Library Modifications

### Adding Cables
Edit `CableConcentricityCalculator/Libraries/CableLibrary.json`:
```json
{
  "partNumber": "CUSTOM-001",
  "name": "Custom Cable Name",
  "manufacturer": "Manufacturer",
  "type": "SingleCore | TwistedPair | MultiCore",
  "jacketColor": "Color",
  "jacketThickness": 0.8,
  "cores": [
    {
      "coreId": "1",
      "conductorDiameter": 1.0,
      "insulationThickness": 0.5,
      "insulationColor": "Red",
      "gauge": "18",
      "conductorMaterial": "Copper"
    }
  ]
}
```

### Adding Heat Shrink
Edit `CableConcentricityCalculator/Libraries/HeatShrinkLibrary.json`:
```json
{
  "partNumber": "DR-25-XX",
  "name": "Heat Shrink Name",
  "manufacturer": "TE Connectivity",
  "material": "Polyolefin",
  "suppliedInnerDiameter": 10.0,
  "recoveredInnerDiameter": 5.0,
  "recoveredWallThickness": 0.5,
  "shrinkRatio": "2:1",
  "temperatureRating": 125,
  "recoveryTemperature": 120
}
```

### Adding Over-Braids
Edit `CableConcentricityCalculator/Libraries/OverBraidLibrary.json`:
```json
{
  "partNumber": "BRAID-001",
  "name": "Over-Braid Name",
  "manufacturer": "Manufacturer",
  "type": "RoundBraid | ExpandableSleeving | FlatBraid",
  "material": "Tinned Copper",
  "nominalInnerDiameter": 8.0,
  "minInnerDiameter": 6.0,
  "maxInnerDiameter": 12.0,
  "wallThickness": 0.3,
  "coveragePercent": 90,
  "isShielding": true
}
```

See [Libraries/README.md](CableConcentricityCalculator/Libraries/README.md) for complete schema.

## Modifying Core Functionality

### Calculation Engine
Location: `CableConcentricityCalculator/Services/ConcentricityCalculator.cs`

Contains:
- Layer packing algorithms
- Filler optimisation
- Assembly validation
- Geometric calculations

**Critical**: Maintain backward compatibility with existing JSON configurations.

### Visualisation
Location: `CableConcentricityCalculator/Visualization/`

| File | Purpose |
|------|---------|
| `CableVisualizer.cs` | 2D cross-section rendering |
| `InteractiveVisualizer.cs` | Hit-testing for UI selection |
| `Cable3DVisualizer.cs` | 3D isometric projections |
| `Cable3DSTLVisualizer.cs` | STL file generation |
| `LayLengthVisualizer.cs` | Lay length diagrams |

All visualisers return raw image data (`byte[]`) or results with metadata. Keep UI-agnostic.

### PDF Reports
Location: `CableConcentricityCalculator/Reports/PdfReportGenerator.cs`

Uses QuestPDF fluent API. Modify report sections using the compose pattern:
```csharp
private void ComposeNewSection(IContainer container)
{
    container.Column(column =>
    {
        // QuestPDF fluent API
    });
}
```

Call from `ComposeContent()` method.

### GUI Modifications
Location: `CableConcentricityCalculator.Gui/`

- **Views**: AXAML files in `Views/` directory
- **ViewModels**: C# files in `ViewModels/` directory (use `[ObservableProperty]` attributes)
- **Converters**: Value converters in `Converters/` directory

Follow existing MVVM patterns. Do not put business logic in code-behind.

## Common Modifications

### Adding New Cable Type
1. Add enum value to `CableType` in `Models/Cable.cs`
2. Update visualisation logic in `CableVisualizer.cs` (if rendering differs)
3. Add sample entries to `Libraries/CableLibrary.json`
4. Test with GUI and console applications

### Adding New Report Section
1. Create compose method in `PdfReportGenerator.cs`
2. Call from `ComposeContent()` in appropriate location
3. Use existing helper methods for consistent formatting
4. Test PDF generation with sample assemblies

### Modifying UI Layout
1. Edit AXAML files in `Views/` directory
2. Update corresponding ViewModel if properties change
3. Use data binding (avoid code-behind logic)
4. Test on all platforms (Windows, macOS, Linux)

## Performance Considerations

### Critical Paths
- Real-time cross-section rendering (< 100ms target)
- PDF generation (< 5 seconds for typical reports)
- UI responsiveness (no blocking operations on UI thread)

### Optimisation Guidelines
- Use async/await for I/O operations
- Cache visualisation results when appropriate
- Avoid excessive allocations in rendering loops
- Profile before optimising

## Debugging

### GUI Application
Use Visual Studio Code launch configurations:
```json
{
  "name": "Launch GUI",
  "type": "coreclr",
  "request": "launch",
  "program": "CableConcentricityCalculator.Gui/bin/Debug/net9.0/CableConcentricityCalculator.Gui.dll"
}
```

### Console Application
```bash
# Debug with demo assembly
dotnet run --project CableConcentricityCalculator -- --demo

# Debug with specific file
dotnet run --project CableConcentricityCalculator -- --load path/to/file.json
```

### Common Issues

**SkiaSharp native library errors**
- Windows: Install Visual C++ Redistributable
- Linux: `sudo apt-get install libfontconfig1`

**QuestPDF licence errors**
- Community Licence is configured in `Program.cs`
- Verify `QuestPDF.Settings.License = LicenseType.Community;` is present

**Avalonia designer errors**
- Ignore designer-time errors if runtime works
- Use `Design.DataContext` for preview data

## Commit Guidelines

### Commit Messages
```
Component: Brief description

Detailed explanation if needed.

Fixes: Issue reference if applicable
```

Examples:
```
Visualization: Fix core spacing in multi-core cables

Updated GetCorePositionsByCableOD to use correct spacing
multiplier for cables with more than 7 cores.

Fixes: #123
```

### Code Review Checklist
- [ ] Builds without warnings
- [ ] GUI application launches and functions
- [ ] Console application runs demo successfully
- [ ] No debug/console output in production code
- [ ] Documentation updated if public API changed
- [ ] JSON library files valid (if modified)
- [ ] Cross-platform compatibility maintained

## Pull Request Process

1. Create feature branch from main
2. Implement changes following code standards
3. Test on target platforms (Windows minimum)
4. Update relevant documentation
5. Commit with descriptive messages
6. Submit PR with:
   - Summary of changes
   - Testing performed
   - Screenshots (if UI changes)
   - Sample outputs (if visualisation changes)

## Licence Compliance

Ensure changes comply with third-party licences:
- **QuestPDF**: Community Licence (free for non-commercial use)
- **Avalonia**: MIT Licence
- **SkiaSharp**: MIT Licence

Do not introduce dependencies with incompatible licences.

## Questions and Support

For technical questions about the codebase:
1. Review existing code and comments
2. Check documentation in `README.md` and `BUILD.md`
3. Examine sample configurations in `Samples/` directory
4. Contact project maintainers

## Directory-Specific Guidelines

### CableConcentricityCalculator/ (Core Library)
- Keep UI-agnostic
- All public APIs must work from console and GUI
- Maintain backward compatibility with JSON configurations
- Unit testable where practical

### CableConcentricityCalculator.Gui/ (GUI Application)
- Follow MVVM strictly
- No business logic in code-behind
- Use data binding for all UI updates
- Support keyboard shortcuts for common operations

### Libraries/ (Component Databases)
- Validate JSON syntax before committing
- Include representative samples of each component type
- Document any custom fields or extensions
- Maintain alphabetical ordering within categories
