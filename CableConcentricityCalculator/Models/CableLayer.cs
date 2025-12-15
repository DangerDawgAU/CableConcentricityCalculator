using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace CableConcentricityCalculator.Models;

/// <summary>
/// Represents a single concentric layer in the cable assembly
/// </summary>
public class CableLayer : INotifyPropertyChanged
{
    private ObservableCollection<Cable> _cables;

    public CableLayer()
    {
        _cables = new();
        _cables.CollectionChanged += Cables_CollectionChanged;
    }

    /// <summary>
    /// Layer number (0 = center, 1 = first layer around center, etc.)
    /// </summary>
    public int LayerNumber { get; set; }

    /// <summary>
    /// Cables in this layer
    /// </summary>
    public ObservableCollection<Cable> Cables
    {
        get => _cables;
        set
        {
            if (_cables != value)
            {
                // Unsubscribe from old collection
                if (_cables != null)
                    _cables.CollectionChanged -= Cables_CollectionChanged;

                _cables = value;

                // Subscribe to new collection
                if (_cables != null)
                    _cables.CollectionChanged += Cables_CollectionChanged;

                OnPropertyChanged(nameof(Cables));
                OnPropertyChanged(nameof(MaxCableDiameter));
                OnPropertyChanged(nameof(LayerDiameter));
                OnPropertyChanged(nameof(TotalConductorCount));
            }
        }
    }

    private void Cables_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(MaxCableDiameter));
        OnPropertyChanged(nameof(LayerDiameter));
        OnPropertyChanged(nameof(TotalConductorCount));
    }

    private TwistDirection _twistDirection = TwistDirection.RightHand;
    /// <summary>
    /// Twist direction for this layer
    /// </summary>
    public TwistDirection TwistDirection
    {
        get => _twistDirection;
        set
        {
            if (_twistDirection != value)
            {
                _twistDirection = value;
                OnPropertyChanged(nameof(TwistDirection));
            }
        }
    }

    private double _layLength = 50;
    /// <summary>
    /// Lay length (twist pitch) in mm - distance for one complete rotation
    /// </summary>
    public double LayLength
    {
        get => _layLength;
        set
        {
            if (_layLength != value)
            {
                _layLength = value;
                OnPropertyChanged(nameof(LayLength));
            }
        }
    }

    private int _fillerCount;
    /// <summary>
    /// Number of filler wires added for concentricity
    /// </summary>
    public int FillerCount
    {
        get => _fillerCount;
        set
        {
            if (_fillerCount != value)
            {
                _fillerCount = value;
                OnPropertyChanged(nameof(FillerCount));
            }
        }
    }

    private double _fillerDiameter;
    /// <summary>
    /// Filler wire diameter in mm
    /// </summary>
    public double FillerDiameter
    {
        get => _fillerDiameter;
        set
        {
            if (_fillerDiameter != value)
            {
                _fillerDiameter = value;
                OnPropertyChanged(nameof(FillerDiameter));
                OnPropertyChanged(nameof(LayerDiameter));
            }
        }
    }

    private string _fillerMaterial = "Nylon";
    /// <summary>
    /// Filler material
    /// </summary>
    public string FillerMaterial
    {
        get => _fillerMaterial;
        set
        {
            if (_fillerMaterial != value)
            {
                _fillerMaterial = value;
                OnPropertyChanged(nameof(FillerMaterial));
            }
        }
    }

    private string _fillerColor = "Natural";
    /// <summary>
    /// Filler color
    /// </summary>
    public string FillerColor
    {
        get => _fillerColor;
        set
        {
            if (_fillerColor != value)
            {
                _fillerColor = value;
                OnPropertyChanged(nameof(FillerColor));
            }
        }
    }

    private TapeWrap? _tapeWrap;
    /// <summary>
    /// Optional tape wrap over this layer
    /// </summary>
    public TapeWrap? TapeWrap
    {
        get => _tapeWrap;
        set
        {
            if (_tapeWrap != value)
            {
                _tapeWrap = value;
                OnPropertyChanged(nameof(TapeWrap));
            }
        }
    }

    private string _notes = string.Empty;
    /// <summary>
    /// Notes for this layer
    /// </summary>
    public string Notes
    {
        get => _notes;
        set
        {
            if (_notes != value)
            {
                _notes = value;
                OnPropertyChanged(nameof(Notes));
            }
        }
    }

    private bool _usePartialLayerOptimization;
    /// <summary>
    /// Enable partial layer optimization - places smaller cables in valleys between larger cables
    /// for improved space efficiency and reduced filler material usage
    /// </summary>
    public bool UsePartialLayerOptimization
    {
        get => _usePartialLayerOptimization;
        set
        {
            if (_usePartialLayerOptimization != value)
            {
                _usePartialLayerOptimization = value;
                OnPropertyChanged(nameof(UsePartialLayerOptimization));
                // Trigger recalculation when optimization changes
                OnPropertyChanged(nameof(LayerDiameter));
                OnPropertyChanged(nameof(CumulativeDiameter));
            }
        }
    }

    /// <summary>
    /// Maximum cable diameter in this layer
    /// </summary>
    public double MaxCableDiameter =>
        Cables.Count > 0 ? Cables.Max(c => c.OuterDiameter) : 0;

    /// <summary>
    /// Total number of conductors in this layer
    /// </summary>
    public int TotalConductorCount =>
        Cables.Where(c => !c.IsFiller).Sum(c => c.Cores.Count);

    /// <summary>
    /// Layer diameter contribution (diameter of this layer's elements only)
    /// </summary>
    public double LayerDiameter => MaxCableDiameter > 0 ? MaxCableDiameter : FillerDiameter;

    private double _cumulativeDiameter;
    /// <summary>
    /// Cumulative diameter from center to outer edge of this layer
    /// </summary>
    public double CumulativeDiameter
    {
        get => _cumulativeDiameter;
        set
        {
            if (_cumulativeDiameter != value)
            {
                _cumulativeDiameter = value;
                OnPropertyChanged(nameof(CumulativeDiameter));
            }
        }
    }

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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
