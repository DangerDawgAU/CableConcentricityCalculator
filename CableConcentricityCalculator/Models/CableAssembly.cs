using System.Collections.ObjectModel;
using CableConcentricityCalculator.Utilities;

namespace CableConcentricityCalculator.Models;

/// <summary>
/// Represents a complete cable assembly with all layers and components
/// </summary>
public class CableAssembly
{
    /// <summary>
    /// Unique identifier for this assembly
    /// </summary>
    public string AssemblyId { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();

    /// <summary>
    /// Assembly part number
    /// </summary>
    public string PartNumber { get; set; } = string.Empty;

    /// <summary>
    /// Assembly revision
    /// </summary>
    public string Revision { get; set; } = "A";

    /// <summary>
    /// Assembly name/description
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Project or customer reference
    /// </summary>
    public string ProjectReference { get; set; } = string.Empty;

    /// <summary>
    /// Designer name
    /// </summary>
    public string DesignedBy { get; set; } = string.Empty;

    /// <summary>
    /// Design date
    /// </summary>
    public DateTime DesignDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Concentric layers from center outward
    /// </summary>
    public ObservableCollection<CableLayer> Layers { get; set; } = new();

    /// <summary>
    /// Heat shrink tubing applications
    /// </summary>
    public ObservableCollection<HeatShrink> HeatShrinks { get; set; } = new();

    /// <summary>
    /// Over-braid applications
    /// </summary>
    public ObservableCollection<OverBraid> OverBraids { get; set; } = new();

    /// <summary>
    /// Diagram annotations and balloons
    /// </summary>
    public ObservableCollection<Annotation> Annotations { get; set; } = new();

    /// <summary>
    /// Outer jacket (if applicable)
    /// </summary>
    public OuterJacket? OuterJacket { get; set; }

    /// <summary>
    /// Overall assembly length in mm
    /// </summary>
    public double Length { get; set; }

    /// <summary>
    /// Design notes
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Temperature rating in Celsius
    /// </summary>
    public int TemperatureRating { get; set; } = 105;

    /// <summary>
    /// Voltage rating in V
    /// </summary>
    public int VoltageRating { get; set; } = 300;

    /// <summary>
    /// Applicable standards (e.g., MIL-DTL-27500, SAE AS22759)
    /// </summary>
    public List<string> ApplicableStandards { get; set; } = new();

    /// <summary>
    /// Calculate the core bundle diameter (all layers without outer finishes)
    /// </summary>
    public double CoreBundleDiameter
    {
        get
        {
            if (Layers.Count == 0) return 0;

            double diameter = 0;

            foreach (var layer in Layers.OrderBy(l => l.LayerNumber))
            {
                if (layer.LayerNumber == 0)
                {
                    // Center layer - could be single cable or bundle
                    diameter = CalculateCenterDiameter(layer);
                }
                else
                {
                    // Add this layer's contribution
                    var maxCableDia = layer.MaxCableDiameter;
                    var fillerDia = layer.FillerDiameter;
                    var effectiveDia = Math.Max(maxCableDia, fillerDia);

                    diameter += 2 * effectiveDia;
                }

                // Add tape wrap if present
                if (layer.TapeWrap != null)
                {
                    diameter += 2 * layer.TapeWrap.EffectiveThickness;
                }
            }

            return diameter;
        }
    }

    /// <summary>
    /// Calculate diameter with all over-braids
    /// </summary>
    public double DiameterWithBraids
    {
        get
        {
            double diameter = CoreBundleDiameter;

            foreach (var braid in OverBraids.OrderBy(b => b.AppliedOverLayer))
            {
                diameter += braid.DiameterAddition;
            }

            return diameter;
        }
    }

    /// <summary>
    /// Calculate overall outer diameter including all layers
    /// </summary>
    public double OverallDiameter
    {
        get
        {
            double diameter = DiameterWithBraids;

            // Add outer jacket if present
            if (OuterJacket != null)
            {
                diameter += 2 * OuterJacket.WallThickness;
            }

            // Add outermost heat shrink if applied to exterior
            var outerHeatShrink = HeatShrinks
                .Where(h => h.AppliedOverLayer == -1)
                .OrderByDescending(h => h.RecoveredWallThickness)
                .FirstOrDefault();

            if (outerHeatShrink != null)
            {
                diameter += outerHeatShrink.TotalWallAddition;
            }

            return diameter;
        }
    }

    /// <summary>
    /// Total cross-sectional area in mm²
    /// </summary>
    public double TotalCrossSectionalArea => CableUtilities.GetCircularArea(OverallDiameter);

    /// <summary>
    /// Total conductor count (all cores in all cables)
    /// </summary>
    public int TotalConductorCount =>
        Layers.SelectMany(l => l.Cables)
              .Where(c => !c.IsFiller)
              .Sum(c => c.Cores.Count);

    /// <summary>
    /// Total cable count (excluding fillers)
    /// </summary>
    public int TotalCableCount =>
        Layers.SelectMany(l => l.Cables)
              .Count(c => !c.IsFiller);

    /// <summary>
    /// Total filler count
    /// </summary>
    public int TotalFillerCount =>
        Layers.Sum(l => l.FillerCount) +
        Layers.SelectMany(l => l.Cables).Count(c => c.IsFiller);

    /// <summary>
    /// Total conductor cross-sectional area in mm²
    /// </summary>
    public double TotalConductorArea =>
        Layers.SelectMany(l => l.Cables)
              .Where(c => !c.IsFiller)
              .Sum(c => c.TotalConductorArea);

    /// <summary>
    /// Calculate the center diameter for layer 0
    /// </summary>
    private double CalculateCenterDiameter(CableLayer centerLayer)
    {
        var elements = centerLayer.GetElements();
        if (elements.Count == 0) return 0;
        if (elements.Count == 1) return elements[0].Diameter;

        // Calculate bundle diameter based on element count
        var maxDiameter = elements.Max(e => e.Diameter);
        return CalculateBundleDiameter(elements.Count, maxDiameter);
    }

    /// <summary>
    /// Standard bundle diameter calculation
    /// </summary>
    private static double CalculateBundleDiameter(int count, double elementDiameter)
    {
        return count switch
        {
            1 => elementDiameter,
            2 => 2 * elementDiameter,
            3 => 2.155 * elementDiameter,
            4 => 2.414 * elementDiameter,
            5 => 2.701 * elementDiameter,
            6 => 3 * elementDiameter,
            7 => 3 * elementDiameter,
            _ => CalculateGeneralBundleDiameter(count, elementDiameter)
        };
    }

    private static double CalculateGeneralBundleDiameter(int count, double elementDiameter)
    {
        double totalArea = count * CableUtilities.GetCircularArea(elementDiameter);
        double bundleArea = totalArea / CableUtilities.PackingEfficiency;
        return 2 * Math.Sqrt(bundleArea / Math.PI);
    }

    /// <summary>
    /// Get Bill of Materials
    /// </summary>
    public List<BomItem> GetBillOfMaterials()
    {
        var bom = new List<BomItem>();
        int itemNum = 1;

        // Add cables
        var cableGroups = Layers
            .SelectMany(l => l.Cables)
            .Where(c => !c.IsFiller)
            .GroupBy(c => c.PartNumber);

        foreach (var group in cableGroups)
        {
            var cable = group.First();
            bom.Add(new BomItem
            {
                ItemNumber = itemNum++,
                PartNumber = cable.PartNumber,
                Description = cable.Name,
                Manufacturer = cable.Manufacturer,
                Quantity = group.Count(),
                Unit = "EA",
                Category = "Cable"
            });
        }

        // Add fillers
        var totalFillers = Layers.Sum(l => l.FillerCount);
        if (totalFillers > 0)
        {
            var firstLayerWithFillers = Layers.First(l => l.FillerCount > 0);
            bom.Add(new BomItem
            {
                ItemNumber = itemNum++,
                PartNumber = "FILLER",
                Description = $"Filler Wire {firstLayerWithFillers.FillerDiameter}mm {firstLayerWithFillers.FillerMaterial}",
                Quantity = totalFillers,
                Unit = "EA",
                Category = "Filler"
            });
        }

        // Add tape wraps
        foreach (var layer in Layers.Where(l => l.TapeWrap != null))
        {
            bom.Add(new BomItem
            {
                ItemNumber = itemNum++,
                PartNumber = layer.TapeWrap!.PartNumber,
                Description = $"{layer.TapeWrap.Material} Tape {layer.TapeWrap.Width}mm",
                Quantity = 1,
                Unit = "ROLL",
                Category = "Tape"
            });
        }

        // Add over-braids
        foreach (var braid in OverBraids)
        {
            bom.Add(new BomItem
            {
                ItemNumber = itemNum++,
                PartNumber = braid.PartNumber,
                Description = braid.Name,
                Manufacturer = braid.Manufacturer,
                Quantity = 1,
                Unit = "EA",
                Category = "Braid"
            });
        }

        // Add heat shrinks
        foreach (var hs in HeatShrinks)
        {
            bom.Add(new BomItem
            {
                ItemNumber = itemNum++,
                PartNumber = hs.PartNumber,
                Description = hs.Name,
                Manufacturer = hs.Manufacturer,
                Quantity = 1,
                Unit = "EA",
                Category = "Heat Shrink"
            });
        }

        // Add outer jacket
        if (OuterJacket != null)
        {
            bom.Add(new BomItem
            {
                ItemNumber = itemNum++,
                PartNumber = OuterJacket.PartNumber,
                Description = OuterJacket.Name,
                Manufacturer = OuterJacket.Manufacturer,
                Quantity = 1,
                Unit = "EA",
                Category = "Jacket"
            });
        }

        return bom;
    }

    public override string ToString()
    {
        return $"{PartNumber} Rev {Revision} - {Name} ({OverallDiameter:F2}mm OD, {TotalConductorCount} conductors)";
    }
}

/// <summary>
/// Outer jacket for the complete assembly
/// </summary>
public class OuterJacket
{
    public string PartNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Material { get; set; } = "PVC";
    public double WallThickness { get; set; }
    public string Color { get; set; } = "Black";
    public bool IsPrintable { get; set; } = true;
    public string PrintText { get; set; } = string.Empty;
}

/// <summary>
/// Bill of Materials item
/// </summary>
public class BomItem
{
    public int ItemNumber { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Unit { get; set; } = "EA";
    public string Category { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
