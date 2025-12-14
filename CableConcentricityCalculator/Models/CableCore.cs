using CableConcentricityCalculator.Utilities;

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
    /// Signal name assigned to this conductor (e.g., "GND", "VCC", "TX", "RX")
    /// </summary>
    public string SignalName { get; set; } = string.Empty;

    /// <summary>
    /// Signal description or function
    /// </summary>
    public string SignalDescription { get; set; } = string.Empty;

    /// <summary>
    /// Pin/terminal designation at connector A
    /// </summary>
    public string PinA { get; set; } = string.Empty;

    /// <summary>
    /// Pin/terminal designation at connector B
    /// </summary>
    public string PinB { get; set; } = string.Empty;

    /// <summary>
    /// Wire label or marker
    /// </summary>
    public string WireLabel { get; set; } = string.Empty;

    /// <summary>
    /// Signal type for categorization
    /// </summary>
    public SignalType SignalType { get; set; } = SignalType.Unassigned;

    /// <summary>
    /// Overall diameter of this core including insulation
    /// </summary>
    public double OverallDiameter => ConductorDiameter + (2 * InsulationThickness);

    /// <summary>
    /// Cross-sectional area of conductor in mm²
    /// </summary>
    public double ConductorArea => CableUtilities.GetCircularArea(ConductorDiameter);

    /// <summary>
    /// Gets a display string for the signal assignment
    /// </summary>
    public string SignalDisplay => string.IsNullOrEmpty(SignalName) ? "(Unassigned)" : SignalName;
}

/// <summary>
/// Types of signals for categorization
/// </summary>
public enum SignalType
{
    Unassigned,
    Power,
    Ground,
    Digital,
    Analog,
    Communication,
    Video,
    Audio,
    Control,
    Shield,
    Spare
}
