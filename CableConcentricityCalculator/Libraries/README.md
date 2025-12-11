# Cable Libraries

This folder contains JSON-based libraries for cables, heat shrink tubing, and over-braids. These files provide an easier way to manage and extend the component catalogs without recompiling the application.

## Library Files

### CableLibrary.json
Contains cable specifications including:
- Part numbers and names
- Manufacturer information
- Cable types (SingleCore, TwistedPair, MultiCore)
- Physical specifications (conductor diameter, insulation thickness, jacket specifications)
- Shield specifications if applicable
- Core configurations with color coding

### HeatShrinkLibrary.json
Contains heat shrink tubing specifications including:
- Part numbers (e.g., DR-25-6.4)
- Supplied and recovered inner diameters
- Wall thicknesses
- Shrink ratios (typically 2:1 or 3:1)
- Temperature ratings
- Adhesive lining options

### OverBraidLibrary.json
Contains over-braid and sleeving specifications including:
- Part numbers (e.g., MDPC-X-Medium-Black, BRAID-TC-8mm)
- Braid types (ExpandableSleeving, RoundBraid, etc.)
- Diameter ranges (min/nominal/max inner diameters)
- Material specifications
- Coverage percentages
- Shielding capabilities

## How It Works

The application follows this loading sequence:

1. **JSON First**: The application attempts to load components from the JSON files in this directory
2. **Fallback Generation**: If JSON files are missing or empty, the application falls back to programmatic generation of standard components
3. **Runtime Loading**: Libraries are loaded when the application starts or when library functions are first called

## Managing Libraries

### Adding New Components

To add new components to the libraries, edit the appropriate JSON file:

1. Open the JSON file in a text editor
2. Add a new entry following the existing format
3. Ensure all required fields are present
4. Save the file
5. Restart the application to load the new components

Example - Adding a new cable:

```json
{
  "partNumber": "CUSTOM-CABLE-001",
  "name": "Custom 4-Core Cable",
  "manufacturer": "Custom Mfg",
  "type": "MultiCore",
  "jacketColor": "Blue",
  "jacketThickness": 0.8,
  "hasShield": false,
  "shieldType": "None",
  "specifiedOuterDiameter": 8.5,
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

### Generating Complete Libraries from Code

If you want to generate complete JSON libraries from the programmatic definitions:

1. Uncomment the save lines in `CableLibrary.cs`:
   - Line ~1479: `LibraryLoader.SaveCableLibrary(library);`
   - Line ~1501: `LibraryLoader.SaveHeatShrinkLibrary(library);`

2. Similarly in `OverBraidService.cs` (line ~220), uncomment the save call

3. Run the application once

4. The generated JSON files will contain all programmatically defined components

5. Re-comment the save lines to prevent overwriting on each run

## File Locations

### Development
During development, edit files in:
```
CableConcentricityCalculator/Libraries/
```

### Runtime
At runtime, the application looks for JSON files in:
```
{ApplicationDirectory}/Libraries/
```

The build system automatically copies JSON files from the source to the output directory.

## Schema Reference

### Cable Schema

Required fields:
- `partNumber`: Unique identifier
- `name`: Display name
- `manufacturer`: Manufacturer name
- `type`: "SingleCore", "TwistedPair", or "MultiCore"
- `cores`: Array of core specifications

Optional fields:
- `specifiedOuterDiameter`: Override calculated diameter (mm)
- `hasShield`: Boolean for shield presence
- `shieldType`: "None", "Braid", "Foil", "FoilAndBraid"
- `shieldThickness`: Shield thickness in mm
- `shieldCoverage`: Coverage percentage (0-100)
- `jacketColor`: Jacket color name
- `jacketThickness`: Jacket thickness in mm

### HeatShrink Schema

Required fields:
- `partNumber`: Unique identifier
- `name`: Display name
- `manufacturer`: Manufacturer name
- `material`: Material type (e.g., "Polyolefin", "PTFE")
- `suppliedInnerDiameter`: As-supplied ID in mm
- `recoveredInnerDiameter`: Fully shrunk ID in mm
- `recoveredWallThickness`: Wall thickness after shrinking in mm
- `shrinkRatio`: Ratio as string (e.g., "2:1", "3:1")
- `temperatureRating`: Maximum operating temperature in °C
- `recoveryTemperature`: Shrink activation temperature in °C

### OverBraid Schema

Required fields:
- `partNumber`: Unique identifier
- `name`: Display name
- `manufacturer`: Manufacturer name
- `type`: "RoundBraid", "ExpandableSleeving", "FlatBraid", etc.
- `material`: Material type
- `nominalInnerDiameter`: Relaxed diameter in mm
- `minInnerDiameter`: Minimum contracted diameter in mm
- `maxInnerDiameter`: Maximum expanded diameter in mm
- `wallThickness`: Wall thickness in mm
- `coveragePercent`: Coverage percentage (0-100)
- `isShielding`: Boolean for EMI/RFI shielding capability

## Best Practices

1. **Backup Before Editing**: Always keep a backup of working library files before making changes
2. **Validate JSON**: Use a JSON validator to ensure files are properly formatted
3. **Test After Changes**: Test the application after adding new components
4. **Consistent Naming**: Use consistent part number formats for easier searching
5. **Document Custom Parts**: Add comments (if your JSON editor supports them) or maintain a separate documentation file for custom components

## Troubleshooting

### Components Not Loading

If components aren't appearing:

1. Check the application console output for error messages
2. Verify JSON files exist in the Libraries folder
3. Validate JSON syntax (use jsonlint.com or similar)
4. Ensure file permissions allow reading
5. Check that required fields are present

### Missing Fields

The application uses case-insensitive property matching, so `partNumber`, `PartNumber`, and `partnumber` all work. However, if required fields are completely missing, components may fail to load.

### File Location Issues

If the application can't find JSON files:
- Verify files are in the `Libraries` subfolder
- Check that files have `.json` extension
- Ensure the build copied files to the output directory
- Look in `bin/Debug/net9.0/Libraries/` or `bin/Release/net9.0/Libraries/`
