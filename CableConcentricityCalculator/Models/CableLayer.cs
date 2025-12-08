namespace CableConcentricityCalculator.Models;

/// <summary>
/// Represents a single concentric layer in the cable assembly
/// </summary>
public class CableLayer
{
    /// <summary>
    /// Layer number (0 = center, 1 = first layer around center, etc.)
    /// </summary>
    public int LayerNumber { get; set; }

    /// <summary>
    /// Cables in this layer
    /// </summary>
    public List<Cable> Cables { get; set; } = new();

    /// <summary>
    /// Twist direction for this layer
    /// </summary>
    public TwistDirection TwistDirection { get; set; } = TwistDirection.RightHand;

    /// <summary>
    /// Lay length (twist pitch) in mm - distance for one complete rotation
    /// </summary>
    public double LayLength { get; set; } = 50;

    /// <summary>
    /// Number of filler wires added for concentricity
    /// </summary>
    public int FillerCount { get; set; }

    /// <summary>
    /// Filler wire diameter in mm
    /// </summary>
    public double FillerDiameter { get; set; }

    /// <summary>
    /// Filler material
    /// </summary>
    public string FillerMaterial { get; set; } = "Nylon";

    /// <summary>
    /// Filler color
    /// </summary>
    public string FillerColor { get; set; } = "Natural";

    /// <summary>
    /// Optional tape wrap over this layer
    /// </summary>
    public TapeWrap? TapeWrap { get; set; }

    /// <summary>
    /// Notes for this layer
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Maximum cable diameter in this layer
    /// </summary>
    public double MaxCableDiameter =>
        Cables.Count > 0 ? Cables.Max(c => c.OuterDiameter) : 0;

    /// <summary>
    /// Total number of elements (cables + fillers) in this layer
    /// </summary>
    public int TotalElements => Cables.Count + FillerCount;

    /// <summary>
    /// Get all elements including fillers
    /// </summary>
    public List<LayerElement> GetElements()
    {
        var elements = new List<LayerElement>();

        foreach (var cable in Cables)
        {
            elements.Add(new LayerElement
            {
                IsFiller = cable.IsFiller,
                Cable = cable,
                Diameter = cable.OuterDiameter,
                Color = cable.JacketColor
            });
        }

        for (int i = 0; i < FillerCount; i++)
        {
            elements.Add(new LayerElement
            {
                IsFiller = true,
                Diameter = FillerDiameter,
                Color = FillerColor,
                Material = FillerMaterial
            });
        }

        return elements;
    }

    public override string ToString()
    {
        string dir = TwistDirection == TwistDirection.RightHand ? "RH" : "LH";
        string fillers = FillerCount > 0 ? $" +{FillerCount} fillers" : "";
        return $"Layer {LayerNumber}: {Cables.Count} cables{fillers} ({dir}, {LayLength}mm lay)";
    }
}

/// <summary>
/// Twist direction enum
/// </summary>
public enum TwistDirection
{
    /// <summary>
    /// Right-hand twist (clockwise when viewed from end)
    /// Also known as "S" twist
    /// </summary>
    RightHand,

    /// <summary>
    /// Left-hand twist (counter-clockwise when viewed from end)
    /// Also known as "Z" twist
    /// </summary>
    LeftHand,

    /// <summary>
    /// No twist (straight lay)
    /// </summary>
    None
}

/// <summary>
/// Represents a single element in a layer (either cable or filler)
/// </summary>
public class LayerElement
{
    public bool IsFiller { get; set; }
    public Cable? Cable { get; set; }
    public double Diameter { get; set; }
    public string Color { get; set; } = "Natural";
    public string Material { get; set; } = "Nylon";
    public double AngularPosition { get; set; } // Radians from 0
}

/// <summary>
/// Tape wrap applied over a layer
/// </summary>
public class TapeWrap
{
    public string Material { get; set; } = "PTFE";
    public double Thickness { get; set; } = 0.05;
    public double Width { get; set; } = 12.7;
    public double OverlapPercent { get; set; } = 50;
    public string Color { get; set; } = "White";
    public string PartNumber { get; set; } = string.Empty;

    /// <summary>
    /// Effective thickness considering overlap
    /// </summary>
    public double EffectiveThickness =>
        OverlapPercent >= 50 ? Thickness * 2 : Thickness * (1 + OverlapPercent / 100);
}
