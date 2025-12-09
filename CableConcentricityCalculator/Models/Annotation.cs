namespace CableConcentricityCalculator.Models;

/// <summary>
/// Represents a balloon annotation on the cable assembly diagram
/// </summary>
public class Annotation
{
    /// <summary>
    /// Unique identifier for this annotation
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Balloon number displayed in the circle
    /// </summary>
    public int BalloonNumber { get; set; }

    /// <summary>
    /// X position of the annotation point (relative to center, in mm)
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y position of the annotation point (relative to center, in mm)
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// X offset for the balloon from the annotation point
    /// </summary>
    public double BalloonOffsetX { get; set; } = 15;

    /// <summary>
    /// Y offset for the balloon from the annotation point
    /// </summary>
    public double BalloonOffsetY { get; set; } = -15;

    /// <summary>
    /// Reference text for the notes table
    /// </summary>
    public string ReferenceText { get; set; } = string.Empty;

    /// <summary>
    /// Detailed note text
    /// </summary>
    public string NoteText { get; set; } = string.Empty;

    /// <summary>
    /// Type of annotation
    /// </summary>
    public AnnotationType Type { get; set; } = AnnotationType.Note;

    /// <summary>
    /// Reference to a cable ID if this annotation points to a specific cable
    /// </summary>
    public string? LinkedCableId { get; set; }

    /// <summary>
    /// Reference to a core ID if this annotation points to a specific core
    /// </summary>
    public string? LinkedCoreId { get; set; }

    /// <summary>
    /// Layer number if this annotation points to a layer
    /// </summary>
    public int? LinkedLayerNumber { get; set; }

    /// <summary>
    /// Whether to show the leader line
    /// </summary>
    public bool ShowLeaderLine { get; set; } = true;

    /// <summary>
    /// Color of the balloon
    /// </summary>
    public string BalloonColor { get; set; } = "White";

    /// <summary>
    /// Text color
    /// </summary>
    public string TextColor { get; set; } = "Black";

    public override string ToString()
    {
        return $"[{BalloonNumber}] {ReferenceText}";
    }
}

/// <summary>
/// Types of annotations
/// </summary>
public enum AnnotationType
{
    Note,
    Dimension,
    PartCallout,
    Warning,
    Specification
}

/// <summary>
/// Represents an entry in the notes table
/// </summary>
public class NotesTableEntry
{
    /// <summary>
    /// Balloon/item number
    /// </summary>
    public int ItemNumber { get; set; }

    /// <summary>
    /// Reference designation
    /// </summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>
    /// Description or note text
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Part number if applicable
    /// </summary>
    public string PartNumber { get; set; } = string.Empty;

    /// <summary>
    /// Quantity if applicable
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Additional remarks
    /// </summary>
    public string Remarks { get; set; } = string.Empty;
}
