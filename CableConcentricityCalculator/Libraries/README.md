# Component Libraries

JSON-based component databases for cables, heat shrink tubing, and over-braids. Enables catalogue updates without recompilation.

## Library Files

| File | Contents |
|------|----------|
| `CableLibrary.json` | Cable specifications (MIL-SPEC, OLFLEX, coaxial, twisted pairs) |
| `HeatShrinkLibrary.json` | Heat shrink tubing (DR-25 series, various manufacturers) |
| `OverBraidLibrary.json` | Sleeving and EMI/RFI shielding braids |

## Loading Sequence

1. Application attempts to load JSON files from `Libraries/` directory
2. If files are missing or invalid, falls back to programmatic generation
3. Libraries are loaded once at application startup

## JSON Schemas

### Cable Schema

**Required Fields:**
```json
{
  "partNumber": "string",           // Unique identifier
  "name": "string",                 // Display name
  "manufacturer": "string",         // Manufacturer name
  "type": "enum",                   // SingleCore | TwistedPair | MultiCore
  "cores": [                        // Array of core specifications
    {
      "coreId": "string",           // Core identifier (e.g., "1", "A")
      "conductorDiameter": number,  // mm
      "insulationThickness": number,// mm
      "insulationColor": "string",  // Color name
      "gauge": "string",            // AWG gauge (e.g., "18", "22")
      "conductorMaterial": "string" // Copper | Aluminium | Silver
    }
  ]
}
```

**Optional Fields:**
```json
{
  "specifiedOuterDiameter": number, // mm (overrides calculated value)
  "hasShield": boolean,             // Shield presence
  "shieldType": "enum",             // None | Braid | Foil | FoilAndBraid
  "shieldThickness": number,        // mm
  "shieldCoverage": number,         // 0-100%
  "jacketColor": "string",          // Color name
  "jacketThickness": number         // mm
}
```

### Heat Shrink Schema

**Required Fields:**
```json
{
  "partNumber": "string",           // Unique identifier
  "name": "string",                 // Display name
  "manufacturer": "string",         // Manufacturer name
  "material": "string",             // Polyolefin | PTFE | PVC | FEP
  "suppliedInnerDiameter": number,  // mm (as-supplied)
  "recoveredInnerDiameter": number, // mm (fully shrunk)
  "recoveredWallThickness": number, // mm (after shrinking)
  "shrinkRatio": "string",          // "2:1" | "3:1" | "4:1"
  "temperatureRating": number,      // °C (maximum operating)
  "recoveryTemperature": number     // °C (shrink activation)
}
```

**Optional Fields:**
```json
{
  "color": "string",                // Black | Clear | Red | etc.
  "adhesiveLinedDescription": "string", // "Yes" | "No" | "Optional"
  "lengthSuppliedMeters": number,   // Standard supply length
  "militarySpec": "string"          // MIL-DTL-XXXXX
}
```

### Over-Braid Schema

**Required Fields:**
```json
{
  "partNumber": "string",           // Unique identifier
  "name": "string",                 // Display name
  "manufacturer": "string",         // Manufacturer name
  "type": "enum",                   // RoundBraid | ExpandableSleeving | FlatBraid
  "material": "string",             // TinnedCopper | PET | Nylon | Nomex
  "nominalInnerDiameter": number,   // mm (relaxed state)
  "minInnerDiameter": number,       // mm (maximum contraction)
  "maxInnerDiameter": number,       // mm (maximum expansion)
  "wallThickness": number,          // mm
  "coveragePercent": number,        // 0-100%
  "isShielding": boolean            // EMI/RFI shielding capability
}
```

**Optional Fields:**
```json
{
  "color": "string",                // Color name
  "endType": "string",              // CleanCut | Frayed | Sealed
  "temperatureRating": number       // °C (maximum operating)
}
```

## Modifying Libraries

### Direct JSON Editing (Recommended)
1. Open JSON file in text editor
2. Add new entry following schema above
3. Validate JSON syntax ([jsonlint.com](https://jsonlint.com))
4. Save file
5. Restart application

**Example: Adding Custom Cable**
```json
{
  "partNumber": "CUSTOM-AWG18-RED",
  "name": "Custom 18 AWG Red Wire",
  "manufacturer": "Custom Mfg",
  "type": "SingleCore",
  "jacketColor": "Red",
  "jacketThickness": 0.5,
  "hasShield": false,
  "cores": [
    {
      "coreId": "1",
      "conductorDiameter": 1.024,
      "insulationThickness": 0.38,
      "insulationColor": "Red",
      "gauge": "18",
      "conductorMaterial": "Copper"
    }
  ]
}
```

### Programmatic Generation
1. Edit generation code in `Services/CableLibrary.cs`, `Services/HeatShrinkService.cs`, or `Services/OverBraidService.cs`
2. Uncomment `LibraryLoader.Save*Library()` calls
3. Run application once to export JSON
4. Re-comment save calls to prevent overwrite on subsequent runs

**Example Locations:**
- `CableLibrary.cs`: Line ~1479 (cable library save)
- `CableLibrary.cs`: Line ~1501 (heat shrink library save)
- `OverBraidService.cs`: Line ~220 (over-braid library save)

## File Locations

### Development
```
CableConcentricityCalculator/Libraries/
├── CableLibrary.json
├── HeatShrinkLibrary.json
└── OverBraidLibrary.json
```

### Runtime
```
{ApplicationDirectory}/Libraries/
├── CableLibrary.json
├── HeatShrinkLibrary.json
└── OverBraidLibrary.json
```

Build system automatically copies JSON files from source to output directory.

## Validation Rules

### Cable Validation
- `partNumber` must be unique across all cables
- `type` must be valid enum value
- At least one core required
- All diameter and thickness values must be positive
- Shield coverage must be 0-100%

### Heat Shrink Validation
- `partNumber` must be unique
- `recoveredInnerDiameter` must be less than `suppliedInnerDiameter`
- Both temperature values must be positive
- Shrink ratio must match pattern "X:1" (e.g., "2:1", "3:1")

### Over-Braid Validation
- `partNumber` must be unique
- `minInnerDiameter` ≤ `nominalInnerDiameter` ≤ `maxInnerDiameter`
- Coverage must be 0-100%
- Wall thickness must be positive

## Troubleshooting

### Components Not Loading

**Check JSON Syntax:**
```bash
# Use online validator: jsonlint.com
# Or Python:
python -m json.tool CableLibrary.json
```

**Verify File Permissions:**
```bash
# Linux/macOS
ls -la Libraries/*.json

# Ensure read permissions
chmod 644 Libraries/*.json
```

**Check Console Output:**
Application logs parsing errors to console. Look for:
- "Failed to load cable library"
- "JSON parsing error"
- "Missing required field"

### Missing Fields Error
Property names are case-insensitive (`partNumber` = `PartNumber` = `partnumber`). However, completely missing required fields will cause load failures.

**Fix:** Add missing fields according to schema above.

### File Not Found
**Symptom:** Application uses programmatic generation instead of JSON files.

**Fix:**
1. Verify files exist in `Libraries/` subdirectory
2. Check file extensions are `.json` (not `.txt` or `.json.bak`)
3. Confirm build copied files to output: `bin/Debug/net9.0/Libraries/`

### Invalid Enum Values
**Symptom:** Component loads but behaves unexpectedly.

**Valid Enum Values:**
- Cable Type: `SingleCore`, `TwistedPair`, `MultiCore`
- Shield Type: `None`, `Braid`, `Foil`, `FoilAndBraid`
- Over-Braid Type: `RoundBraid`, `ExpandableSleeving`, `FlatBraid`

**Fix:** Correct enum value to match valid options exactly (case-sensitive).

## Best Practices

1. **Backup Before Editing:** Keep working copy of JSON files before modifications
2. **Validate Syntax:** Always validate JSON before committing changes
3. **Consistent Naming:** Use systematic part numbering (e.g., `M22759/16-18-WHT` for MIL-SPEC)
4. **Test After Changes:** Verify components appear in GUI and console applications
5. **Document Custom Parts:** Maintain separate documentation for non-standard components
6. **Alphabetical Ordering:** Keep entries sorted by part number for easier maintenance

## Performance Notes

- Libraries are loaded once at startup (not per-component access)
- Typical load time: < 100ms for all three libraries
- Memory footprint: ~5-10 MB for complete catalogues
- No performance impact from library size during normal operation
