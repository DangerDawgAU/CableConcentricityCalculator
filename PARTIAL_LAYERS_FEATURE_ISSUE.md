# Feature Request: Partial Layer Cable Packing Optimization

## Summary

Add support for optimized cable packing using "partial layers" - allowing smaller diameter cables to nestle into the valleys between larger cables, maximizing space efficiency and reducing filler material usage.

## Problem Statement

Currently, the application uses concentric circular packing where all cables in a layer are positioned at the same pitch circle radius. This approach leaves gaps (valleys) between cables that are filled with inert filler material.

When packing cables of different diameters (e.g., 3 large cables of 10mm diameter with 6 smaller cables of 3mm diameter), the smaller cables could physically fit into the valleys between the larger cables at a different radius. This would:

- **Reduce overall harness diameter** by 5-15%
- **Reduce filler material usage** by 30-60%
- **Pack 20-40% more functional cables** in the same diameter
- **Improve space efficiency** for mixed-size cable harnesses

## Proposed Solution

### User Experience

Add a **per-layer checkbox** in the layer list: **"Optimize partial layers"**

When enabled:
1. User adds cables of any size to the layer
2. Application automatically arranges cables by diameter
3. Larger cables are placed in primary positions on the pitch circle
4. Smaller cables are nestled into valleys at optimal radii
5. Visualization immediately updates to show optimized layout

### Implementation Overview

#### 1. Data Model Changes
- **File**: `CableConcentricityCalculator/Models/CableLayer.cs`
- **Change**: Add `bool UsePartialLayerOptimization` property
- **Impact**: Minimal - single property with INotifyPropertyChanged support

#### 2. UI Changes
- **File**: `CableConcentricityCalculator.Gui/Views/MainWindow.axaml`
- **Change**: Add checkbox to layer list item template (lines 124-162)
- **Layout**: Add 4th row to layer grid with checkbox
- **Event**: Wire up toggle event to trigger recalculation

#### 3. Algorithm Implementation
- **File**: `CableConcentricityCalculator/Services/ConcentricityCalculator.cs`
- **New Methods**:
  - `OptimizeLayerPacking()` - Main optimization algorithm
  - `FindBestValley()` - Find optimal valley for cable placement
  - `CalculateValleyPosition()` - Geometric calculations for valley radius
  - `OverlapsAny()` - Collision detection between cables
- **Modified Methods**:
  - `CalculateCablePositions()` - Check optimization flag and use new algorithm
  - `CalculateBundleDiameter()` - Account for cables at varying radii

### Algorithm Details

**Optimization Strategy**: Greedy best-fit algorithm

1. Sort all cables in layer by diameter (largest first)
2. Determine optimal number of cables for primary ring
3. Place primary (largest) cables evenly around pitch circle
4. For each remaining (smaller) cable:
   - Calculate valley positions between adjacent primary cables
   - Find the valley where the cable best fits
   - Calculate optimal radial position (avoiding overlaps)
   - Place cable at valley position
5. Return all cable positions (primary + valley cables)

**Geometric Formula**:
For two adjacent cables of diameter `D` at radius `R` separated by angle `θ`:
- Valley cable max diameter: `d ≤ 2 * R * sin(θ/2) - D`
- Valley cable radius: `r = R - sqrt((D/2 + d/2)² - (d/2)²)`

### Example Scenario

**Before (Standard Packing)**:
- 3× 10mm cables + 6× 4mm cables
- All 9 cables at pitch radius 15mm
- Outer diameter: ~40mm
- Fillers needed: 15

**After (Optimized Packing)**:
- 3× 10mm cables at radius 15mm (primary)
- 6× 4mm cables at radius 10mm (valleys)
- Outer diameter: ~35mm
- Fillers needed: 6
- **Savings**: 5mm diameter reduction, 9 fewer fillers

## Implementation Tasks

### Phase 1: Core Infrastructure (2-3 hours)
- [ ] Add `UsePartialLayerOptimization` property to `CableLayer.cs`
- [ ] Add checkbox to layer list UI in `MainWindow.axaml`
- [ ] Add event handler in `MainWindow.axaml.cs`
- [ ] Ensure property serialization for save/load

### Phase 2: Algorithm Implementation (6-8 hours)
- [ ] Implement `OptimizeLayerPacking()` method
- [ ] Implement `FindBestValley()` method
- [ ] Implement `CalculateValleyPosition()` geometric calculations
- [ ] Implement `OverlapsAny()` collision detection
- [ ] Modify `CalculateCablePositions()` to use optimization when enabled
- [ ] Update `CalculateBundleDiameter()` for accurate diameter calculation

### Phase 3: Testing & Validation (4-6 hours)
- [ ] Test with 3 large cables + small valleys scenario
- [ ] Test with 6 medium cables + 12 small cables
- [ ] Test mixed assemblies (some layers optimized, some standard)
- [ ] Verify visualization displays cables at varying radii correctly
- [ ] Test edge cases: all same size, very small valleys, etc.
- [ ] Verify serialization: save and load assemblies with optimization enabled

### Phase 4: Quality & Documentation (2-3 hours)
- [ ] Write unit tests for valley detection algorithm
- [ ] Write unit tests for collision detection
- [ ] Add feature documentation to README
- [ ] Add tooltips and help text in UI
- [ ] Performance testing with large assemblies

## Acceptance Criteria

1. ✅ Checkbox appears in layer list for each layer
2. ✅ Clicking checkbox immediately recalculates and updates visualization
3. ✅ Optimized cables are positioned at correct radii (no overlaps)
4. ✅ Visualization correctly displays cables at varying radii within same layer
5. ✅ Assembly diameter calculation accounts for valley cables
6. ✅ Feature works independently per layer (can mix optimized and standard layers)
7. ✅ Save/load preserves optimization settings
8. ✅ Backwards compatible (existing assemblies load without optimization)

## Technical Considerations

### Backwards Compatibility
- New property defaults to `false` (optimization disabled)
- Existing assemblies continue to work without modification
- No breaking changes to file format

### Performance
- Algorithm is O(n²) worst case for cable placement
- Acceptable for typical layer sizes (< 50 cables per layer)
- No performance impact when optimization is disabled

### Limitations
- Works best with mixed cable sizes (large + small)
- May not find globally optimal solution (greedy algorithm)
- Requires at least 3 primary cables to create valleys
- Future enhancement: constraint-based optimization for perfect packing

## Future Enhancements (Out of Scope)

- Visual indication of which cables are primary vs valley cables
- Manual override: drag cables to specific positions
- Optimization suggestions: "Add 6× 3mm cables to fill valleys"
- 3D visualization updates for valley cable helical paths
- Advanced optimization algorithms (simulated annealing, genetic algorithms)
- Multi-layer optimization (optimize all layers simultaneously)

## References

### Relevant Files
- **Model**: `CableConcentricityCalculator/Models/CableLayer.cs`
- **Calculator**: `CableConcentricityCalculator/Services/ConcentricityCalculator.cs`
- **Visualizer**: `CableConcentricityCalculator/Visualization/CableVisualizer.cs`
- **UI View**: `CableConcentricityCalculator.Gui/Views/MainWindow.axaml`
- **UI Code-Behind**: `CableConcentricityCalculator.Gui/Views/MainWindow.axaml.cs`

### Related Code
- Current packing algorithm: `ConcentricityCalculator.cs:CalculateMaxCablesInLayer()` (line ~130)
- Current position calculation: `ConcentricityCalculator.cs:CalculateCablePositions()` (line ~150)
- Visualization rendering: `CableVisualizer.cs:GenerateCrossSection()` (line ~100)

## Estimated Effort

**Total**: 14-20 hours of development

- Design & Planning: ✅ Complete
- Implementation: 14-20 hours
- Testing: Included in phases
- Documentation: Included in Phase 4

## Priority

**Medium-High** - Provides significant value for users working with mixed-size cable harnesses, enabling better space utilization and material cost savings.

---

**Labels**: `enhancement`, `feature`, `optimization`, `ui`

**Milestone**: v1.1.0 or v2.0.0 (depending on release schedule)
