namespace CableConcentricityCalculator.Models;

/// <summary>
/// Represents a single core within a cable conductor
/// </summary>
public class CableCore
{
    /// <summary>
    /// Unique identifier for this core
    /// </summary>
    public string CoreId { get; set; } = string.Empty;

    /// <summary>
    /// Core conductor diameter in mm
    /// </summary>
    public double ConductorDiameter { get; set; }

    /// <summary>
    /// Core insulation thickness in mm
    /// </summary>
    public double InsulationThickness { get; set; }

    /// <summary>
    /// Insulation color
    /// </summary>
    public string InsulationColor { get; set; } = "Natural";

    /// <summary>
    /// AWG/gauge of the conductor
    /// </summary>
    public string Gauge { get; set; } = string.Empty;

    /// <summary>
    /// Conductor material (Copper, Tinned Copper, Silver Plated, etc.)
    /// </summary>
    public string ConductorMaterial { get; set; } = "Copper";

    /// <summary>
    /// Overall diameter of this core including insulation
    /// </summary>
    public double OverallDiameter => ConductorDiameter + (2 * InsulationThickness);

    /// <summary>
    /// Cross-sectional area of conductor in mm²
    /// </summary>
    public double ConductorArea => Math.PI * Math.Pow(ConductorDiameter / 2, 2);
}
