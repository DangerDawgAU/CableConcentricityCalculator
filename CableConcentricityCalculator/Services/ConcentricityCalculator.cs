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
    /// Calculate positions (x, y) of cables in a layer
    /// </summary>
    public static List<(double X, double Y, double Diameter)> CalculateCablePositions(
        CableLayer layer,
        double layerCenterRadius)
    {
        var positions = new List<(double X, double Y, double Diameter)>();
        var elements = layer.GetElements();

        if (elements.Count == 0) return positions;

        // For center layer (layer 0), special handling
        if (layer.LayerNumber == 0)
        {
            return CalculateCenterLayerPositions(elements);
        }

        // Calculate angular positions
        var angles = CalculateAngularPositions(elements.Count);

        // The pitch circle is at layerCenterRadius
        for (int i = 0; i < elements.Count; i++)
        {
            double x = layerCenterRadius * Math.Cos(angles[i]);
            double y = layerCenterRadius * Math.Sin(angles[i]);
            positions.Add((x, y, elements[i].Diameter));
        }

        return positions;
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
        double d = elements.Max(e => e.Diameter);

        switch (elements.Count)
        {
            case 2:
                positions.Add((-d / 2, 0, elements[0].Diameter));
                positions.Add((d / 2, 0, elements[1].Diameter));
                break;

            case 3:
                // Triangular arrangement
                double r3 = d / Math.Sqrt(3);
                for (int i = 0; i < 3; i++)
                {
                    double angle = (i * 2 * Math.PI / 3) - (Math.PI / 2);
                    positions.Add((r3 * Math.Cos(angle), r3 * Math.Sin(angle), elements[i].Diameter));
                }
                break;

            case 4:
                // Square arrangement
                double r4 = d / Math.Sqrt(2);
                for (int i = 0; i < 4; i++)
                {
                    double angle = (i * Math.PI / 2) + (Math.PI / 4);
                    positions.Add((r4 * Math.Cos(angle), r4 * Math.Sin(angle), elements[i].Diameter));
                }
                break;

            case 5:
                // Pentagon arrangement
                double r5 = d / (2 * Math.Sin(Math.PI / 5));
                for (int i = 0; i < 5; i++)
                {
                    double angle = (i * 2 * Math.PI / 5) - (Math.PI / 2);
                    positions.Add((r5 * Math.Cos(angle), r5 * Math.Sin(angle), elements[i].Diameter));
                }
                break;

            case 6:
                // Hexagonal arrangement (no center)
                double r6 = d;
                for (int i = 0; i < 6; i++)
                {
                    double angle = i * Math.PI / 3;
                    positions.Add((r6 * Math.Cos(angle), r6 * Math.Sin(angle), elements[i].Diameter));
                }
                break;

            case 7:
                // 1 center + 6 around
                positions.Add((0, 0, elements[0].Diameter));
                double r7 = d;
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

        for (int i = 0; i < layerNumber && i < assembly.Layers.Count; i++)
        {
            var layer = assembly.Layers[i];

            if (i == 0)
            {
                // Center layer - calculate its outer radius
                var elements = layer.GetElements();
                if (elements.Count == 1)
                {
                    radius = elements[0].Diameter / 2;
                }
                else
                {
                    var positions = CalculateCenterLayerPositions(elements);
                    if (positions.Count > 0)
                    {
                        radius = positions.Max(p => Math.Sqrt(p.X * p.X + p.Y * p.Y) + p.Diameter / 2);
                    }
                }
            }
            else
            {
                // Add this layer's cable diameter
                radius += layer.MaxCableDiameter;
            }

            // Add tape wrap if present
            if (layer.TapeWrap != null)
            {
                radius += layer.TapeWrap.EffectiveThickness;
            }
        }

        // Add half the next layer's cable diameter for pitch circle
        if (layerNumber < assembly.Layers.Count)
        {
            radius += assembly.Layers[layerNumber].MaxCableDiameter / 2;
        }

        return radius;
    }

    /// <summary>
    /// Suggest optimal filler configuration for an assembly
    /// </summary>
    public static void OptimizeFillers(CableAssembly assembly)
    {
        for (int i = 1; i < assembly.Layers.Count; i++)
        {
            var layer = assembly.Layers[i];
            var cableCount = layer.Cables.Count;
            var cableDiameter = layer.MaxCableDiameter;

            // Calculate inner diameter (previous layer's outer)
            double innerDiameter = 0;
            for (int j = 0; j < i; j++)
            {
                var prevLayer = assembly.Layers[j];
                if (j == 0)
                {
                    var elements = prevLayer.GetElements();
                    if (elements.Count > 0)
                    {
                        var positions = CalculateCenterLayerPositions(elements);
                        innerDiameter = positions.Count > 0
                            ? 2 * positions.Max(p => Math.Sqrt(p.X * p.X + p.Y * p.Y) + p.Diameter / 2)
                            : elements[0].Diameter;
                    }
                }
                else
                {
                    innerDiameter += 2 * prevLayer.MaxCableDiameter;
                }

                if (prevLayer.TapeWrap != null)
                {
                    innerDiameter += 2 * prevLayer.TapeWrap.EffectiveThickness;
                }
            }

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
}
