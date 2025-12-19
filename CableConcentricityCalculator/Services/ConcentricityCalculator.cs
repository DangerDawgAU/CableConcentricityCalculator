using CableConcentricityCalculator.Models;

namespace CableConcentricityCalculator.Services;

/// <summary>
/// Engine for calculating concentric cable layups
/// </summary>
public class ConcentricityCalculator
{
    /// <summary>
    /// Standard concentric layer cable counts (number that fit in each layer)
    /// Layer 0: 1-7 (center)
    /// Layer 1: 6 (for 1 center) to 12 (for 7 center)
    /// Layer n: approximately 6n for equal diameter cables
    /// </summary>
    public static readonly int[] StandardLayerCounts = { 1, 6, 12, 18, 24, 30, 36, 42, 48, 54 };

    /// <summary>
    /// Calculate the maximum number of cables that fit in a layer
    /// </summary>
    /// <param name="innerDiameter">Diameter of the bundle inside this layer</param>
    /// <param name="cableDiameter">Diameter of cables to place in this layer</param>
    /// <returns>Maximum number of cables that fit</returns>
    public static int CalculateMaxCablesInLayer(double innerDiameter, double cableDiameter)
    {
        if (cableDiameter <= 0) return 0;
        if (innerDiameter <= 0) return 1; // Center conductor

        // The pitch circle diameter is the inner diameter plus the cable diameter
        double pitchCircleDiameter = innerDiameter + cableDiameter;

        // Each cable occupies an arc length equal to its diameter
        // Maximum count is circumference divided by cable diameter
        double circumference = Math.PI * pitchCircleDiameter;
        int maxCount = (int)Math.Floor(circumference / cableDiameter);

        return maxCount;
    }

    /// <summary>
    /// Calculate required filler count to achieve proper concentricity
    /// </summary>
    /// <param name="innerDiameter">Diameter of the bundle inside this layer</param>
    /// <param name="cableDiameter">Diameter of cables in this layer</param>
    /// <param name="cableCount">Number of actual cables</param>
    /// <param name="fillerDiameter">Diameter of filler wires (0 to use same as cables)</param>
    /// <returns>Number of fillers needed, and recommended filler diameter</returns>
    public static (int FillerCount, double FillerDiameter) CalculateFillers(
        double innerDiameter,
        double cableDiameter,
        int cableCount,
        double fillerDiameter = 0)
    {
        if (fillerDiameter <= 0) fillerDiameter = cableDiameter;

        int maxCables = CalculateMaxCablesInLayer(innerDiameter, cableDiameter);

        // If we have more cables than fit, we have a problem
        if (cableCount > maxCables)
        {
            throw new ArgumentException(
                $"Cannot fit {cableCount} cables (diameter {cableDiameter}mm) in layer with inner diameter {innerDiameter}mm. Maximum is {maxCables}.");
        }

        // For proper concentricity, we want to fill the layer completely
        // or use a standard fill pattern
        int fillersNeeded = 0;

        if (cableCount < maxCables)
        {
            // Calculate how many fillers of the given diameter would fill the gap
            double usedArc = cableCount * cableDiameter;
            double pitchCircumference = Math.PI * (innerDiameter + cableDiameter);
            double remainingArc = pitchCircumference - usedArc;

            fillersNeeded = (int)Math.Floor(remainingArc / fillerDiameter);

            // Adjust filler diameter if needed for exact fit
            if (fillersNeeded > 0)
            {
                double actualRemainingArc = pitchCircumference - usedArc;
                double adjustedFillerDiameter = actualRemainingArc / fillersNeeded;

                // If adjustment is small (within 10%), use adjusted diameter
                if (Math.Abs(adjustedFillerDiameter - fillerDiameter) / fillerDiameter <= 0.1)
                {
                    fillerDiameter = adjustedFillerDiameter;
                }
            }
        }

        return (fillersNeeded, fillerDiameter);
    }

    /// <summary>
    /// Calculate the angular positions of elements in a layer
    /// </summary>
    public static List<double> CalculateAngularPositions(int elementCount, double startAngle = 0)
    {
        var positions = new List<double>();

        if (elementCount <= 0) return positions;

        double angleStep = 2 * Math.PI / elementCount;

        for (int i = 0; i < elementCount; i++)
        {
            positions.Add(startAngle + (i * angleStep));
        }

        return positions;
    }

    /// <summary>
    /// Calculate positions (x, y) of cables in a layer (with assembly context for optimization)
    /// </summary>
    public static List<(double X, double Y, double Diameter)> CalculateCablePositions(
        CableAssembly assembly,
        int layerNumber)
    {
        if (layerNumber >= assembly.Layers.Count)
            return new List<(double, double, double)>();

        var layer = assembly.Layers[layerNumber];
        var cables = layer.Cables.ToList();

        if (cables.Count == 0)
            return new List<(double, double, double)>();

        DebugLogger.Log($"[CALC_POS] Layer {layerNumber}: LayerNumber={layerNumber}, UsePartialLayerOptimization={layer.UsePartialLayerOptimization}, CableCount={cables.Count}");

        // Check if optimization is enabled for this layer (not applicable to layer 0)
        if (layerNumber > 0 && layer.UsePartialLayerOptimization && cables.Count > 0)
        {
            DebugLogger.Log($"[OPTIMIZATION ACTIVE] Layer {layerNumber}: Valley packing optimization ENABLED, {cables.Count} cables");

            // Get positions from the previous layer
            var previousLayerPositions = CalculateCablePositions(assembly, layerNumber - 1);
            DebugLogger.Log($"[OPTIMIZATION ACTIVE] Layer {layerNumber}: Got {previousLayerPositions.Count} previous layer positions");

            if (previousLayerPositions.Count > 0)
            {
                var optimizedCablePositions = OptimizeLayerPacking(cables, previousLayerPositions);
                DebugLogger.Log($"[OPTIMIZATION ACTIVE] Layer {layerNumber}: OptimizeLayerPacking returned {optimizedCablePositions.Count} positions");

                // Create a mapping from Cable to position
                var cableToPosition = optimizedCablePositions.ToDictionary(p => p.Cable, p => (p.X, p.Y, p.Diameter));

                // Build result in the SAME ORDER as layer.GetElements() expects
                var result = new List<(double, double, double)>();
                var elements = layer.GetElements();

                foreach (var element in elements)
                {
                    if (element.Cable != null && cableToPosition.ContainsKey(element.Cable))
                    {
                        result.Add(cableToPosition[element.Cable]);
                        DebugLogger.Log($"[OPTIMIZATION ACTIVE] Layer {layerNumber}: Mapped cable to position ({cableToPosition[element.Cable].X:F2}, {cableToPosition[element.Cable].Y:F2})");
                    }
                    else
                    {
                        DebugLogger.Log($"[OPTIMIZATION ACTIVE] Layer {layerNumber}: WARNING - Element has no matching position!");
                    }
                }

                // Add fillers at standard positions on outer ring if there are any
                if (layer.FillerCount > 0)
                {
                    // Calculate outer radius from optimized cable positions
                    double maxRadius = optimizedCablePositions.Max(p =>
                        Math.Sqrt(p.X * p.X + p.Y * p.Y) + p.Diameter / 2);

                    double fillerPitchRadius = maxRadius + layer.FillerDiameter / 2;
                    // Apply layer rotation angle (convert degrees to radians)
                    double startAngle = layer.RotationAngle * Math.PI / 180.0;
                    var fillerAngles = CalculateAngularPositions(layer.FillerCount, startAngle);

                    for (int i = 0; i < layer.FillerCount; i++)
                    {
                        double x = fillerPitchRadius * Math.Cos(fillerAngles[i]);
                        double y = fillerPitchRadius * Math.Sin(fillerAngles[i]);
                        result.Add((x, y, layer.FillerDiameter));
                    }
                }

                return result;
            }
        }

        // Standard concentric packing
        DebugLogger.Log($"[STANDARD PACKING] Layer {layerNumber}: Using standard concentric packing (optimization disabled or layer 0)");
        // Get the outer boundary of the previous layer (where cables should touch)
        double innerBoundary = CalculateInnerBoundaryRadius(assembly, layerNumber);
        return CalculateCablePositions(layer, innerBoundary);
    }

    /// <summary>
    /// Calculate positions (x, y) of cables in a layer
    /// </summary>
    /// <param name="layer">The layer to calculate positions for</param>
    /// <param name="innerBoundaryRadius">The radius where cables should touch (outer surface of previous layer)</param>
    public static List<(double X, double Y, double Diameter)> CalculateCablePositions(
        CableLayer layer,
        double innerBoundaryRadius)
    {
        var positions = new List<(double X, double Y, double Diameter)>();
        var elements = layer.GetElements();

        if (elements.Count == 0) return positions;

        // For center layer (layer 0), special handling
        if (layer.LayerNumber == 0)
        {
            return CalculateCenterLayerPositions(elements);
        }

        // Calculate angular positions with layer rotation
        // Apply layer rotation angle (convert degrees to radians)
        double startAngle = layer.RotationAngle * Math.PI / 180.0;
        var angles = CalculateAngularPositions(elements.Count, startAngle);

        // All cables should touch the inner boundary (outer surface of previous layer)
        // Each cable's center is positioned at innerBoundary + (cable's radius)
        DebugLogger.Log($"[STANDARD PACKING DETAIL] Layer {layer.LayerNumber}: InnerBoundary={innerBoundaryRadius:F2}mm, RotationAngle={layer.RotationAngle:F1}°");

        for (int i = 0; i < elements.Count; i++)
        {
            // Each cable's center should be positioned so its inner surface touches the inner boundary
            double cableRadius = elements[i].Diameter / 2;
            double centerRadius = innerBoundaryRadius + cableRadius;

            double x = centerRadius * Math.Cos(angles[i]);
            double y = centerRadius * Math.Sin(angles[i]);
            positions.Add((x, y, elements[i].Diameter));

            DebugLogger.Log($"[STANDARD PACKING DETAIL] Layer {layer.LayerNumber} Cable {i}: Diameter={elements[i].Diameter:F2}mm, CenterRadius={centerRadius:F2}mm, Position=({x:F2}, {y:F2})");
        }

        return positions;
    }

    /// <summary>
    /// Calculate the inner boundary radius for a layer (where cables should touch the previous layer)
    /// </summary>
    private static double CalculateInnerBoundaryRadius(CableAssembly assembly, int layerNumber)
    {
        if (layerNumber == 0) return 0;

        double radius = 0;

        DebugLogger.Log($"[INNER BOUNDARY] Calculating for layer {layerNumber}");

        for (int i = 0; i < layerNumber && i < assembly.Layers.Count; i++)
        {
            var layer = assembly.Layers[i];

            if (i == 0)
            {
                // Center layer - calculate its outer radius from actual positions
                var elements = layer.GetElements();
                if (elements.Count == 1)
                {
                    radius = elements[0].Diameter / 2;
                    DebugLogger.Log($"[INNER BOUNDARY] Layer 0 (single): radius = {radius:F2}mm");
                }
                else
                {
                    var positions = CalculateCenterLayerPositions(elements);
                    if (positions.Count > 0)
                    {
                        radius = positions.Max(p => Math.Sqrt(p.X * p.X + p.Y * p.Y) + p.Diameter / 2);
                        DebugLogger.Log($"[INNER BOUNDARY] Layer 0 (bundle): outer radius = {radius:F2}mm");
                    }
                }
            }
            else
            {
                // Calculate actual outer radius from this layer's cable positions
                // IMPORTANT: Use CalculateCablePositions(assembly, layerNumber) to respect optimization settings
                var positions = CalculateCablePositions(assembly, i);
                if (positions.Count > 0)
                {
                    double outerRadius = positions.Max(p => Math.Sqrt(p.X * p.X + p.Y * p.Y) + p.Diameter / 2);
                    double prevRadius = radius;
                    radius = outerRadius;
                    DebugLogger.Log($"[INNER BOUNDARY] Layer {i}: Calculated from actual positions, radius {prevRadius:F2} -> {radius:F2}mm");
                }
            }

            // Add tape wrap if present
            if (layer.TapeWrap != null)
            {
                double prevRadius = radius;
                radius += layer.TapeWrap.EffectiveThickness;
                DebugLogger.Log($"[INNER BOUNDARY] Layer {i}: Adding tape={layer.TapeWrap.EffectiveThickness:F2}mm, radius {prevRadius:F2} -> {radius:F2}mm");
            }
        }

        DebugLogger.Log($"[INNER BOUNDARY] Layer {layerNumber} inner boundary radius = {radius:F2}mm");
        return radius;
    }

    /// <summary>
    /// Calculate positions for center layer (layer 0)
    /// </summary>
    private static List<(double X, double Y, double Diameter)> CalculateCenterLayerPositions(
        List<LayerElement> elements)
    {
        var positions = new List<(double X, double Y, double Diameter)>();

        if (elements.Count == 0) return positions;

        if (elements.Count == 1)
        {
            positions.Add((0, 0, elements[0].Diameter));
            return positions;
        }

        // Standard patterns for small counts
        switch (elements.Count)
        {
            case 2:
                // Two cables touching - position based on their actual radii
                double r0 = elements[0].Diameter / 2;
                double r1 = elements[1].Diameter / 2;
                positions.Add((-r0, 0, elements[0].Diameter));
                positions.Add((r1, 0, elements[1].Diameter));
                DebugLogger.Log($"[CENTER LAYER] 2 cables: d0={elements[0].Diameter:F2}mm @ x={-r0:F2}, d1={elements[1].Diameter:F2}mm @ x={r1:F2}");
                break;

            case 3:
                // Triangular arrangement - three circles touching each other
                // Use circle packing geometry for different sized circles
                double r3_0 = elements[0].Diameter / 2;
                double r3_1 = elements[1].Diameter / 2;
                double r3_2 = elements[2].Diameter / 2;

                // Calculate positions where all three circles touch each other
                // Cable 0 at origin
                // Cable 1 to the right, touching cable 0
                // Cable 2 positioned so it touches both 0 and 1

                double x3_0 = 0;
                double y3_0 = 0;

                double x3_1 = r3_0 + r3_1;
                double y3_1 = 0;

                // Cable 2: Find position where it touches both cable 0 and cable 1
                // Distance from cable 0 to cable 2 = r3_0 + r3_2
                // Distance from cable 1 to cable 2 = r3_1 + r3_2
                double d01 = x3_1; // Distance between centers of cable 0 and 1
                double d02 = r3_0 + r3_2; // Required distance from 0 to 2
                double d12 = r3_1 + r3_2; // Required distance from 1 to 2

                // Using triangle law of cosines to find position
                // Calculate x coordinate of cable 2
                double x3_2 = (d01*d01 + d02*d02 - d12*d12) / (2*d01);
                // Calculate y coordinate (positive y = above the line)
                double y3_2 = Math.Sqrt(d02*d02 - x3_2*x3_2);

                // Center the triangle at origin
                double centerX3 = (x3_0 + x3_1 + x3_2) / 3;
                double centerY3 = (y3_0 + y3_1 + y3_2) / 3;

                positions.Add((x3_0 - centerX3, y3_0 - centerY3, elements[0].Diameter));
                positions.Add((x3_1 - centerX3, y3_1 - centerY3, elements[1].Diameter));
                positions.Add((x3_2 - centerX3, y3_2 - centerY3, elements[2].Diameter));

                DebugLogger.Log($"[CENTER LAYER] 3 cables: d0={elements[0].Diameter:F2}, d1={elements[1].Diameter:F2}, d2={elements[2].Diameter:F2}");
                DebugLogger.Log($"[CENTER LAYER] Positions: ({x3_0 - centerX3:F2}, {y3_0 - centerY3:F2}), ({x3_1 - centerX3:F2}, {y3_1 - centerY3:F2}), ({x3_2 - centerX3:F2}, {y3_2 - centerY3:F2})");
                break;

            case 4:
                // Four cables arranged in square/diamond pattern
                // Each cable touches exactly 2 neighbors (not all at center)
                double r4_0 = elements[0].Diameter / 2;
                double r4_1 = elements[1].Diameter / 2;
                double r4_2 = elements[2].Diameter / 2;
                double r4_3 = elements[3].Diameter / 2;

                // For 4 cables of same diameter in a square: distance from center = r * sqrt(2)
                // Position at 45°, 135°, 225°, 315° so each touches 2 neighbors
                double sqrt2 = Math.Sqrt(2);
                double dist4_0 = r4_0 * sqrt2;
                double dist4_1 = r4_1 * sqrt2;
                double dist4_2 = r4_2 * sqrt2;
                double dist4_3 = r4_3 * sqrt2;

                // 45° = top-right
                positions.Add((dist4_0 / sqrt2, dist4_0 / sqrt2, elements[0].Diameter));
                // 135° = top-left
                positions.Add((-dist4_1 / sqrt2, dist4_1 / sqrt2, elements[1].Diameter));
                // 225° = bottom-left
                positions.Add((-dist4_2 / sqrt2, -dist4_2 / sqrt2, elements[2].Diameter));
                // 315° = bottom-right
                positions.Add((dist4_3 / sqrt2, -dist4_3 / sqrt2, elements[3].Diameter));

                DebugLogger.Log($"[CENTER LAYER] 4 cables: d0={elements[0].Diameter:F2}, d1={elements[1].Diameter:F2}, d2={elements[2].Diameter:F2}, d3={elements[3].Diameter:F2}");
                DebugLogger.Log($"[CENTER LAYER] Square pattern - each cable touches 2 neighbors");
                break;

            case 5:
                // Pentagon arrangement
                double d5 = elements.Max(e => e.Diameter);
                double r5 = d5 / (2 * Math.Sin(Math.PI / 5));
                for (int i = 0; i < 5; i++)
                {
                    double angle = (i * 2 * Math.PI / 5) - (Math.PI / 2);
                    positions.Add((r5 * Math.Cos(angle), r5 * Math.Sin(angle), elements[i].Diameter));
                }
                break;

            case 6:
                // Hexagonal arrangement (no center)
                double d6 = elements.Max(e => e.Diameter);
                double r6 = d6;
                for (int i = 0; i < 6; i++)
                {
                    double angle = i * Math.PI / 3;
                    positions.Add((r6 * Math.Cos(angle), r6 * Math.Sin(angle), elements[i].Diameter));
                }
                break;

            case 7:
                // 1 center + 6 around
                positions.Add((0, 0, elements[0].Diameter));
                double d7 = elements.Max(e => e.Diameter);
                double r7 = d7;
                for (int i = 1; i < 7; i++)
                {
                    double angle = (i - 1) * Math.PI / 3;
                    positions.Add((r7 * Math.Cos(angle), r7 * Math.Sin(angle), elements[i].Diameter));
                }
                break;

            default:
                // For larger counts, use concentric arrangement
                positions = CalculateLargeCountPositions(elements);
                break;
        }

        return positions;
    }

    /// <summary>
    /// Calculate positions for large element counts using concentric packing
    /// </summary>
    private static List<(double X, double Y, double Diameter)> CalculateLargeCountPositions(
        List<LayerElement> elements)
    {
        var positions = new List<(double X, double Y, double Diameter)>();
        var remaining = new List<LayerElement>(elements);
        double d = elements.Max(e => e.Diameter);

        // Start with center
        if (remaining.Count > 0)
        {
            positions.Add((0, 0, remaining[0].Diameter));
            remaining.RemoveAt(0);
        }

        // Add concentric rings
        double ringRadius = d;
        while (remaining.Count > 0)
        {
            int ringCount = CalculateMaxCablesInLayer(ringRadius * 2 - d, d);
            ringCount = Math.Min(ringCount, remaining.Count);

            for (int i = 0; i < ringCount && remaining.Count > 0; i++)
            {
                double angle = i * 2 * Math.PI / ringCount;
                positions.Add((ringRadius * Math.Cos(angle), ringRadius * Math.Sin(angle), remaining[0].Diameter));
                remaining.RemoveAt(0);
            }

            ringRadius += d;
        }

        return positions;
    }

    /// <summary>
    /// Calculate the pitch circle radius for a layer
    /// </summary>
    public static double CalculateLayerPitchRadius(CableAssembly assembly, int layerNumber)
    {
        if (layerNumber == 0) return 0;

        double radius = 0;

        DebugLogger.Log($"[PITCH RADIUS] Calculating for layer {layerNumber}");

        for (int i = 0; i < layerNumber && i < assembly.Layers.Count; i++)
        {
            var layer = assembly.Layers[i];

            if (i == 0)
            {
                // Center layer - calculate its outer radius from actual positions
                var elements = layer.GetElements();
                if (elements.Count == 1)
                {
                    radius = elements[0].Diameter / 2;
                    DebugLogger.Log($"[PITCH RADIUS] Layer 0 (single): radius = {radius:F2}mm");
                }
                else
                {
                    var positions = CalculateCenterLayerPositions(elements);
                    if (positions.Count > 0)
                    {
                        radius = positions.Max(p => Math.Sqrt(p.X * p.X + p.Y * p.Y) + p.Diameter / 2);
                        DebugLogger.Log($"[PITCH RADIUS] Layer 0 (bundle): outer radius = {radius:F2}mm");
                    }
                }
            }
            else
            {
                // Calculate actual outer radius from this layer's cable positions
                // IMPORTANT: Use the assembly-aware method that respects UsePartialLayerOptimization
                var positions = CalculateCablePositions(assembly, i);
                if (positions.Count > 0)
                {
                    double outerRadius = positions.Max(p => Math.Sqrt(p.X * p.X + p.Y * p.Y) + p.Diameter / 2);
                    double prevRadius = radius;
                    radius = outerRadius;
                    DebugLogger.Log($"[PITCH RADIUS] Layer {i}: Calculated from actual positions (respects optimization), radius {prevRadius:F2} -> {radius:F2}mm");
                }
            }

            // Add tape wrap if present
            if (layer.TapeWrap != null)
            {
                double prevRadius = radius;
                radius += layer.TapeWrap.EffectiveThickness;
                DebugLogger.Log($"[PITCH RADIUS] Layer {i}: Adding tape={layer.TapeWrap.EffectiveThickness:F2}mm, radius {prevRadius:F2} -> {radius:F2}mm");
            }
        }

        // Add half the next layer's cable diameter for pitch circle
        if (layerNumber < assembly.Layers.Count)
        {
            double halfDiameter = assembly.Layers[layerNumber].MaxCableDiameter / 2;
            double prevRadius = radius;
            radius += halfDiameter;
            DebugLogger.Log($"[PITCH RADIUS] Final: Adding half of layer {layerNumber} diameter ({halfDiameter:F2}mm), radius {prevRadius:F2} -> {radius:F2}mm");
        }

        DebugLogger.Log($"[PITCH RADIUS] Layer {layerNumber} final pitch radius = {radius:F2}mm");
        return radius;
    }

    /// <summary>
    /// Suggest optimal filler configuration for an assembly (all layers)
    /// </summary>
    public static void OptimizeFillers(CableAssembly assembly)
    {
        for (int i = 1; i < assembly.Layers.Count; i++)
        {
            OptimizeFillers(assembly, i);
        }
    }

    /// <summary>
    /// Suggest optimal filler configuration for a specific layer
    /// </summary>
    public static void OptimizeFillers(CableAssembly assembly, int layerNumber)
    {
        if (layerNumber <= 0 || layerNumber >= assembly.Layers.Count)
            return; // Can't optimize layer 0 or invalid layer

        var layer = assembly.Layers[layerNumber];
        var cableCount = layer.Cables.Count;
        var cableDiameter = layer.MaxCableDiameter;

        // Calculate inner diameter (previous layer's outer) recursively
        // This ensures we use actual optimized positions for all previous layers
        double innerRadius = 0;

        for (int j = 0; j < layerNumber; j++)
        {
            var prevLayer = assembly.Layers[j];

            // Get actual positions for this layer (respects UsePartialLayerOptimization)
            var positions = CalculateCablePositions(assembly, j);

            if (positions.Count > 0)
            {
                // Calculate the outer radius of this layer from its actual positions
                innerRadius = positions.Max(p => Math.Sqrt(p.X * p.X + p.Y * p.Y) + p.Diameter / 2);
            }

            // Add tape wrap if present
            if (prevLayer.TapeWrap != null)
            {
                innerRadius += prevLayer.TapeWrap.EffectiveThickness;
            }
        }

        double innerDiameter = innerRadius * 2;

        // Calculate fillers needed
        try
        {
            var (fillerCount, fillerDiameter) = CalculateFillers(
                innerDiameter, cableDiameter, cableCount, layer.FillerDiameter);

            layer.FillerCount = fillerCount;
            if (fillerCount > 0)
            {
                layer.FillerDiameter = fillerDiameter;
            }
        }
        catch (ArgumentException)
        {
            // Layer is overfilled - would need to redistribute cables
        }
    }

    /// <summary>
    /// Validate that an assembly is geometrically feasible
    /// </summary>
    public static List<string> ValidateAssembly(CableAssembly assembly)
    {
        var issues = new List<string>();

        if (assembly.Layers.Count == 0)
        {
            issues.Add("Assembly has no layers defined");
            return issues;
        }

        double currentDiameter = 0;

        for (int i = 0; i < assembly.Layers.Count; i++)
        {
            var layer = assembly.Layers[i];

            if (layer.Cables.Count == 0 && layer.FillerCount == 0)
            {
                issues.Add($"Layer {i} has no cables or fillers");
                continue;
            }

            if (i == 0)
            {
                var elements = layer.GetElements();

                // Check for different sized cables in center layer
                if (layer.Cables.Count >= 2)
                {
                    var uniqueDiameters = layer.Cables.Select(c => c.OuterDiameter).Distinct().Count();

                    // Warning for 2-3 different sized cables
                    if (uniqueDiameters > 1 && layer.Cables.Count <= 3)
                    {
                        issues.Add($"WARNING: Layer 0 has {layer.Cables.Count} cables with different sizes. This may impact cable flexibility and reliability. Consider moving extra cables to Layer 1 for better performance.");
                    }
                    // Error for 4+ cables with different sizes
                    else if (uniqueDiameters > 1 && layer.Cables.Count > 3)
                    {
                        issues.Add($"ERROR: Layer 0 (center layer): Maximum 3 cables of different sizes allowed. Found {layer.Cables.Count} cables with {uniqueDiameters} different sizes. Either reduce to 3 cables or use 4 cables of the same size.");
                    }
                    // Error for more than 4 cables total (even same size)
                    else if (layer.Cables.Count > 4)
                    {
                        issues.Add($"ERROR: Layer 0 (center layer): Maximum 4 cables allowed, found {layer.Cables.Count}. Please reduce cable count.");
                    }
                }

                var positions = CalculateCenterLayerPositions(elements);
                if (positions.Count > 0)
                {
                    currentDiameter = 2 * positions.Max(p => Math.Sqrt(p.X * p.X + p.Y * p.Y) + p.Diameter / 2);
                }
            }
            else
            {
                // Check if cables fit
                var maxCables = CalculateMaxCablesInLayer(currentDiameter, layer.MaxCableDiameter);
                var totalElements = layer.TotalElements;

                if (totalElements > maxCables)
                {
                    issues.Add($"Layer {i}: {totalElements} elements exceed maximum capacity of {maxCables}");
                }
                else if (totalElements < maxCables - 1)
                {
                    issues.Add($"Layer {i}: {totalElements} elements leave gaps (capacity is {maxCables}). Consider adding {maxCables - totalElements} fillers.");
                }

                currentDiameter += 2 * layer.MaxCableDiameter;
            }

            if (layer.TapeWrap != null)
            {
                currentDiameter += 2 * layer.TapeWrap.EffectiveThickness;
            }
        }

        // Validate heat shrinks
        foreach (var hs in assembly.HeatShrinks)
        {
            if (!hs.WillFitOver(assembly.CoreBundleDiameter))
            {
                issues.Add($"Heat shrink {hs.PartNumber} will not fit over core diameter {assembly.CoreBundleDiameter:F2}mm");
            }
        }

        // Validate over-braids
        foreach (var braid in assembly.OverBraids)
        {
            if (!braid.CanFitOver(assembly.CoreBundleDiameter))
            {
                issues.Add($"Over-braid {braid.PartNumber} cannot fit over diameter {assembly.CoreBundleDiameter:F2}mm");
            }
        }

        return issues;
    }

    /// <summary>
    /// Check if a proposed cable position overlaps with any existing cables
    /// </summary>
    private static bool OverlapsAny(
        (double x, double y) position,
        double diameter,
        List<(double X, double Y, double Diameter, Cable Cable)> existingCables,
        double tolerance = 0.01)
    {
        foreach (var cable in existingCables)
        {
            double distance = Math.Sqrt(
                Math.Pow(position.x - cable.X, 2) +
                Math.Pow(position.y - cable.Y, 2));

            double minDistance = (diameter + cable.Diameter) / 2 + tolerance;

            if (distance < minDistance)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Calculate the optimal position for a cable in the valley between two adjacent cables
    /// Uses circle tangency geometry to find where the new cable touches both adjacent cables
    /// </summary>
    /// <summary>
    /// Public wrapper for calculating valley position between two cables
    /// </summary>
    public static (double x, double y)? CalculateValleyPosition(
        double cable1X, double cable1Y, double cable1Diameter,
        double cable2X, double cable2Y, double cable2Diameter,
        double valleyDiameter)
    {
        // Calculate inner boundary as the max radius of the two cables
        double innerBoundary = Math.Max(
            Math.Sqrt(cable1X * cable1X + cable1Y * cable1Y) + cable1Diameter / 2,
            Math.Sqrt(cable2X * cable2X + cable2Y * cable2Y) + cable2Diameter / 2);

        return CalculateValleyPosition(
            (cable1X, cable1Y, cable1Diameter, null!),
            (cable2X, cable2Y, cable2Diameter, null!),
            valleyDiameter,
            innerBoundary);
    }

    private static (double x, double y)? CalculateValleyPosition(
        (double X, double Y, double Diameter, Cable Cable) cable1,
        (double X, double Y, double Diameter, Cable Cable) cable2,
        double valleyDiameter,
        double innerBoundaryRadius)
    {
        // Get radii
        double r1 = cable1.Diameter / 2;  // Radius of cable 1
        double r2 = cable2.Diameter / 2;  // Radius of cable 2
        double r3 = valleyDiameter / 2;   // Radius of new cable to place

        // Distance between centers of cable1 and cable2
        double d12 = Math.Sqrt(Math.Pow(cable2.X - cable1.X, 2) + Math.Pow(cable2.Y - cable1.Y, 2));

        // For the new cable to touch both cables externally (tangent), the distances are:
        // dist(center3, center1) = r1 + r3  (touching externally)
        // dist(center3, center2) = r2 + r3  (touching externally)

        double d13 = r1 + r3;  // Required distance from cable1 center to new cable center
        double d23 = r2 + r3;  // Required distance from cable2 center to new cable center

        // Use triangle formula to find position
        // We have a triangle with sides d12, d13, d23
        // Check if such a triangle can exist
        if (d13 + d23 < d12 || d12 + d13 < d23 || d12 + d23 < d13)
        {
            DebugLogger.Log($"[DEBUG] CalculateValleyPosition: Triangle inequality violated - cables too far apart");
            return null;  // No valid position (cables too far apart)
        }

        // Calculate angle from cable1 to cable2
        double angleBetween = Math.Atan2(cable2.Y - cable1.Y, cable2.X - cable1.X);

        // Use law of cosines to find angle at cable1
        // d23^2 = d12^2 + d13^2 - 2*d12*d13*cos(angle)
        double cosAngle = (d12 * d12 + d13 * d13 - d23 * d23) / (2 * d12 * d13);

        // Clamp to valid range for acos
        cosAngle = Math.Max(-1, Math.Min(1, cosAngle));
        double angleOffset = Math.Acos(cosAngle);

        // The valley position is along the angle that makes angleOffset with the line to cable2
        // We want the position that nestles into the valley (lower radius)
        double angle3 = angleBetween - angleOffset;

        // Calculate position
        double x3 = cable1.X + d13 * Math.Cos(angle3);
        double y3 = cable1.Y + d13 * Math.Sin(angle3);

        // Check if this position is at least at the inner boundary
        double radius3 = Math.Sqrt(x3 * x3 + y3 * y3);

        // Verify the position is valid (touching both cables)
        double checkDist1 = Math.Sqrt(Math.Pow(x3 - cable1.X, 2) + Math.Pow(y3 - cable1.Y, 2));
        double checkDist2 = Math.Sqrt(Math.Pow(x3 - cable2.X, 2) + Math.Pow(y3 - cable2.Y, 2));

        DebugLogger.Log($"[DEBUG] CalculateValleyPosition: valley r={r3:F2}, cable1 r={r1:F2}, cable2 r={r2:F2}");
        DebugLogger.Log($"[DEBUG] CalculateValleyPosition: d12={d12:F2}, d13={d13:F2}, d23={d23:F2}");
        DebugLogger.Log($"[DEBUG] CalculateValleyPosition: Position ({x3:F2}, {y3:F2}) at radius {radius3:F2}");
        DebugLogger.Log($"[DEBUG] CalculateValleyPosition: Actual distances: d1={checkDist1:F2} (expect {d13:F2}), d2={checkDist2:F2} (expect {d23:F2})");

        return (x3, y3);
    }

    /// <summary>
    /// Find the best valley position for a cable among all available valleys
    /// </summary>
    private static (double x, double y)? FindBestValley(
        List<(double X, double Y, double Diameter, Cable Cable)> existingCables,
        double newCableDiameter,
        double innerBoundaryRadius)
    {
        if (existingCables.Count < 2)
            return null;

        // Sort cables by angle to ensure we check adjacent pairs
        var sortedCables = existingCables
            .Select(c => (
                Cable: c,
                Angle: Math.Atan2(c.Y, c.X)
            ))
            .OrderBy(c => c.Angle)
            .Select(c => c.Cable)
            .ToList();

        DebugLogger.Log($"[DEBUG] FindBestValley: Checking {sortedCables.Count} valleys for cable diameter {newCableDiameter:F2}mm");

        // Try positions between each pair of adjacent cables
        List<(double x, double y, double score)> validPositions = new();

        for (int i = 0; i < sortedCables.Count; i++)
        {
            int j = (i + 1) % sortedCables.Count;

            var pos = CalculateValleyPosition(
                sortedCables[i],
                sortedCables[j],
                newCableDiameter,
                innerBoundaryRadius);

            if (pos.HasValue)
            {
                DebugLogger.Log($"[DEBUG] Valley {i}: Found position at ({pos.Value.x:F2}, {pos.Value.y:F2})");

                // Exclude the two cables we're nestling between from overlap check (they're supposed to touch!)
                // Compare by position since sortedCables contains new tuple instances
                var cable1 = sortedCables[i];
                var cable2 = sortedCables[j];
                var cablesToCheck = existingCables.Where(c =>
                    !(Math.Abs(c.X - cable1.X) < 0.001 && Math.Abs(c.Y - cable1.Y) < 0.001) &&
                    !(Math.Abs(c.X - cable2.X) < 0.001 && Math.Abs(c.Y - cable2.Y) < 0.001)).ToList();

                DebugLogger.Log($"[DEBUG] Valley {i}: Excluding cables at ({cable1.X:F2},{cable1.Y:F2}) and ({cable2.X:F2},{cable2.Y:F2}) from overlap check");
                DebugLogger.Log($"[DEBUG] Valley {i}: Checking {cablesToCheck.Count} cables for overlap (was {existingCables.Count} total)");

                if (!OverlapsAny(pos.Value, newCableDiameter, cablesToCheck))
                {
                    // Score based on how close to inner boundary (prefer inner positions)
                    double radius = Math.Sqrt(pos.Value.x * pos.Value.x + pos.Value.y * pos.Value.y);
                    double score = 1.0 / (radius + 1.0); // Lower radius = higher score

                    validPositions.Add((pos.Value.x, pos.Value.y, score));
                    DebugLogger.Log($"[DEBUG] Valley {i}: Valid (touching cables {i} and {j}), score={score:F4}");
                }
                else
                {
                    DebugLogger.Log($"[DEBUG] Valley {i}: Position overlaps OTHER cables (not {i},{j})");
                }
            }
            else
            {
                DebugLogger.Log($"[DEBUG] Valley {i}: No valid position calculated");
            }
        }

        DebugLogger.Log($"[DEBUG] FindBestValley: Found {validPositions.Count} valid positions");

        // Return the best position (highest score)
        if (validPositions.Count > 0)
        {
            var best = validPositions.OrderByDescending(p => p.score).First();
            return (best.x, best.y);
        }

        return null;
    }

    /// <summary>
    /// Optimize layer packing by placing cables in valleys between cables from the previous layer
    /// This is a synchronous wrapper for the async version to maintain backward compatibility
    /// </summary>
    public static List<(double X, double Y, double Diameter, Cable Cable)> OptimizeLayerPacking(
        List<Cable> currentLayerCables,
        List<(double X, double Y, double Diameter)> previousLayerPositions)
    {
        // Use Task.Run to avoid blocking the UI thread, then wait for result
        return OptimizeLayerPackingAsync(currentLayerCables, previousLayerPositions).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async version: Optimize layer packing by placing cables in valleys between cables from the previous layer
    /// Uses parallel processing to check multiple valley positions simultaneously
    /// </summary>
    public static async Task<List<(double X, double Y, double Diameter, Cable Cable)>> OptimizeLayerPackingAsync(
        List<Cable> currentLayerCables,
        List<(double X, double Y, double Diameter)> previousLayerPositions)
    {
        DebugLogger.Log($"[DEBUG] OptimizeLayerPackingAsync: {currentLayerCables.Count} cables to place, {previousLayerPositions.Count} previous positions");

        if (currentLayerCables.Count == 0)
            return new List<(double, double, double, Cable)>();

        // Use Task.Run to offload computation to thread pool
        return await Task.Run(() =>
        {
            var positions = new List<(double X, double Y, double Diameter, Cable Cable)>();

            // Convert previous layer positions to the format needed for valley detection
            var prevCables = previousLayerPositions
                .Select(p => (p.X, p.Y, p.Diameter, (Cable?)null!))
                .ToList();

            // Find the inner boundary (outermost extent of previous layer)
            double innerBoundaryRadius = 0;
            if (prevCables.Count > 0)
            {
                innerBoundaryRadius = prevCables.Max(c =>
                    Math.Sqrt(c.X * c.X + c.Y * c.Y) + c.Diameter / 2);
            }
            DebugLogger.Log($"[DEBUG] OptimizeLayerPackingAsync: Inner boundary radius = {innerBoundaryRadius:F2}mm");

            // Lock for thread-safe modifications to prevCables
            var prevCablesLock = new object();

            // Try to place each cable in a valley between previous layer cables
            foreach (var cable in currentLayerCables)
            {
                DebugLogger.Log($"[DEBUG] Placing cable diameter {cable.OuterDiameter:F2}mm");
                (double x, double y)? valleyPos = null;

                // Try to find a valley position
                if (prevCables.Count >= 2)
                {
                    // Create a snapshot for valley finding to avoid race conditions
                    List<(double X, double Y, double Diameter, Cable Cable)> prevCablesSnapshot;
                    lock (prevCablesLock)
                    {
                        prevCablesSnapshot = new List<(double, double, double, Cable)>(prevCables);
                    }

                    valleyPos = FindBestValley(prevCablesSnapshot, cable.OuterDiameter, innerBoundaryRadius);
                }

                if (valleyPos.HasValue)
                {
                    // Place in valley
                    DebugLogger.Log($"[DEBUG] Found valley at ({valleyPos.Value.x:F2}, {valleyPos.Value.y:F2})");
                    positions.Add((valleyPos.Value.x, valleyPos.Value.y, cable.OuterDiameter, cable));

                    // Add this cable to the list so subsequent cables avoid it
                    lock (prevCablesLock)
                    {
                        prevCables.Add((valleyPos.Value.x, valleyPos.Value.y, cable.OuterDiameter, cable));
                    }
                }
                else
                {
                    // No valley found - place on standard pitch circle
                    DebugLogger.Log($"[DEBUG] No valley found - using standard pitch circle");
                    double pitchRadius = innerBoundaryRadius + cable.OuterDiameter / 2;

                    // Calculate angle to avoid existing cables in this layer
                    double bestAngle = 0;
                    if (positions.Count > 0)
                    {
                        // Evenly distribute around circle
                        bestAngle = 2 * Math.PI * positions.Count / currentLayerCables.Count;
                    }

                    double x = pitchRadius * Math.Cos(bestAngle);
                    double y = pitchRadius * Math.Sin(bestAngle);

                    positions.Add((x, y, cable.OuterDiameter, cable));
                    lock (prevCablesLock)
                    {
                        prevCables.Add((x, y, cable.OuterDiameter, cable));
                    }
                }
            }

            return positions;
        });
    }
}
