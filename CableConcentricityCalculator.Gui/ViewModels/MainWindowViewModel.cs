using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CableConcentricityCalculator.Models;
using CableConcentricityCalculator.Reports;
using CableConcentricityCalculator.Services;
using CableConcentricityCalculator.Visualization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CableConcentricityCalculator.Gui.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private CableAssembly _assembly;

    [ObservableProperty]
    private CableLayer? _selectedLayer;

    [ObservableProperty]
    private Cable? _selectedCable;

    [ObservableProperty]
    private byte[]? _crossSectionImage;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    [ObservableProperty]
    private string? _currentFilePath;

    [ObservableProperty]
    private ObservableCollection<CableLibraryItem> _cableLibrary;

    [ObservableProperty]
    private CableLibraryItem? _selectedLibraryCable;

    [ObservableProperty]
    private ObservableCollection<string> _validationMessages;

    [ObservableProperty]
    private InteractiveImageResult? _interactiveImage;

    [ObservableProperty]
    private VisualElement? _selectedElement;

    [ObservableProperty]
    private string _selectedElementInfo = "";

    [ObservableProperty]
    private bool _showElementInfo;

    [ObservableProperty]
    private string _selectedCableCategory = "All";

    [ObservableProperty]
    private ObservableCollection<NotesTableEntry> _notesTable;

    [ObservableProperty]
    private bool _showCrossSection = true;

    [ObservableProperty]
    private bool _showIsometric;

    [ObservableProperty]
    private byte[]? _isometricImage;

    public string[] CableCategories => ConfigurationService.GetCableCategories();

    public ObservableCollection<HeatShrink> AvailableHeatShrinks { get; } =
        new(HeatShrinkService.GetAvailableHeatShrinks());

    [ObservableProperty]
    private HeatShrink? _selectedHeatShrink;

    public ObservableCollection<OverBraid> AvailableOverBraids { get; } =
        new(OverBraidService.GetAllAvailableBraids());

    [ObservableProperty]
    private OverBraid? _selectedOverBraid;

    public string[] MDPCXColors => OverBraidService.MDPCXColors;

    public MainWindowViewModel()
    {
        _assembly = CreateNewAssembly();
        _cableLibrary = new ObservableCollection<CableLibraryItem>(LoadCableLibrary());
        _validationMessages = new ObservableCollection<string>();
        _notesTable = new ObservableCollection<NotesTableEntry>();
        UpdateCrossSectionImage();
        // Select the most appropriate heat shrink based on initial assembly
        SelectedHeatShrink = HeatShrinkService.SelectAppropriateHeatShrink(Assembly.DiameterWithBraids);
        // Select the most appropriate over-braid/sleeving based on initial assembly
        SelectedOverBraid = OverBraidService.SelectAppropriateMDPCXSleeving(Assembly.CoreBundleDiameter);
    }

    partial void OnSelectedCableCategoryChanged(string value)
    {
        RefreshCableLibrary();
    }

    private void RefreshCableLibrary()
    {
        var library = SelectedCableCategory == "All"
            ? ConfigurationService.CreateSampleCableLibrary()
            : ConfigurationService.GetCablesByCategory(SelectedCableCategory);

        CableLibrary.Clear();
        foreach (var item in library.Select(kvp => new CableLibraryItem
        {
            Key = kvp.Key,
            Cable = kvp.Value,
            DisplayName = $"{kvp.Value.PartNumber} - {kvp.Value.Name}"
        }).OrderBy(x => x.Key).Take(500)) // Limit to 500 items for performance
        {
            CableLibrary.Add(item);
        }
    }

    private static CableAssembly CreateNewAssembly()
    {
        return new CableAssembly
        {
            PartNumber = "NEW-001",
            Revision = "A",
            Name = "New Cable Assembly",
            DesignedBy = Environment.UserName,
            DesignDate = DateTime.Now
        };
    }

    private static CableLibraryItem[] LoadCableLibrary()
    {
        var library = ConfigurationService.CreateSampleCableLibrary();
        return library.Select(kvp => new CableLibraryItem
        {
            Key = kvp.Key,
            Cable = kvp.Value,
            DisplayName = $"{kvp.Value.PartNumber} - {kvp.Value.Name}"
        }).OrderBy(x => x.Key).ToArray();
    }

    partial void OnAssemblyChanged(CableAssembly value)
    {
        UpdateCumulativeLayerDiameters();
        UpdateCrossSectionImage();
        ValidateAssembly();
        // Update selected heat shrink based on new assembly diameter
        SelectedHeatShrink = HeatShrinkService.SelectAppropriateHeatShrink(value.DiameterWithBraids);
    }

    partial void OnSelectedLayerChanged(CableLayer? value)
    {
        SelectedCable = value?.Cables.FirstOrDefault();
    }

    /// <summary>
    /// Recalculate cumulative diameters for all layers based on the current assembly
    /// </summary>
    private void UpdateCumulativeLayerDiameters()
    {
        double cumulativeDiameter = 0;

        foreach (var layer in Assembly.Layers.OrderBy(l => l.LayerNumber))
        {
            if (layer.LayerNumber == 0)
            {
                // Center layer - calculate its own diameter
                var elements = layer.GetElements();
                if (elements.Count == 0)
                {
                    cumulativeDiameter = 0;
                }
                else if (elements.Count == 1)
                {
                    cumulativeDiameter = elements[0].Diameter;
                }
                else
                {
                    // Use max diameter for multi-element center layer
                    var maxDia = elements.Max(e => e.Diameter);
                    cumulativeDiameter = maxDia * 2; // Rough approximation
                }
            }
            else
            {
                // Add this layer's thickness (2x cable radius)
                var layerThickness = layer.LayerDiameter;
                cumulativeDiameter += 2 * layerThickness;
            }

            // Add tape wrap if present
            if (layer.TapeWrap != null)
            {
                cumulativeDiameter += 2 * layer.TapeWrap.EffectiveThickness;
            }

            layer.CumulativeDiameter = cumulativeDiameter;
        }
    }

    [RelayCommand]
    private void NewAssembly()
    {
        Assembly = CreateNewAssembly();
        CurrentFilePath = null;
        HasUnsavedChanges = false;
        SelectedLayer = null;
        StatusMessage = "Created new assembly";
    }

    [RelayCommand]
    private async Task LoadAssembly()
    {
        try
        {
            var window = Avalonia.Application.Current?.ApplicationLifetime
                is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow : null;

            if (window == null) return;

            var storageProvider = window.StorageProvider;
            var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Cable Assembly",
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new("JSON Files") { Patterns = new[] { "*.json" } },
                    new("All Files") { Patterns = new[] { "*" } }
                }
            });

            if (result.Count > 0)
            {
                var filePath = result[0].Path.LocalPath;
                Assembly = await ConfigurationService.LoadAssemblyAsync(filePath);
                CurrentFilePath = filePath;
                HasUnsavedChanges = false;
                SelectedLayer = Assembly.Layers.FirstOrDefault();
                StatusMessage = $"Loaded: {Assembly.PartNumber}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading file: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveAssembly()
    {
        if (string.IsNullOrEmpty(CurrentFilePath))
        {
            await SaveAssemblyAs();
            return;
        }

        try
        {
            await ConfigurationService.SaveAssemblyAsync(Assembly, CurrentFilePath);
            HasUnsavedChanges = false;
            StatusMessage = $"Saved: {CurrentFilePath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveAssemblyAs()
    {
        try
        {
            var window = Avalonia.Application.Current?.ApplicationLifetime
                is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow : null;

            if (window == null) return;

            var storageProvider = window.StorageProvider;
            var result = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Cable Assembly",
                DefaultExtension = "json",
                SuggestedFileName = $"{Assembly.PartNumber}.json",
                FileTypeChoices = new List<FilePickerFileType>
                {
                    new("JSON Files") { Patterns = new[] { "*.json" } }
                }
            });

            if (result != null)
            {
                var filePath = result.Path.LocalPath;
                await ConfigurationService.SaveAssemblyAsync(Assembly, filePath);
                CurrentFilePath = filePath;
                HasUnsavedChanges = false;
                StatusMessage = $"Saved: {filePath}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportPdf()
    {
        try
        {
            var window = Avalonia.Application.Current?.ApplicationLifetime
                is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow : null;

            if (window == null) return;

            var storageProvider = window.StorageProvider;
            var result = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export PDF Report",
                DefaultExtension = "pdf",
                SuggestedFileName = $"{Assembly.PartNumber}_Report.pdf",
                FileTypeChoices = new List<FilePickerFileType>
                {
                    new("PDF Files") { Patterns = new[] { "*.pdf" } }
                }
            });

            if (result != null)
            {
                var filePath = result.Path.LocalPath;
                await Task.Run(() => PdfReportGenerator.GenerateReport(Assembly, filePath));
                StatusMessage = $"PDF exported: {filePath}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting PDF: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportImage()
    {
        try
        {
            var window = Avalonia.Application.Current?.ApplicationLifetime
                is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow : null;

            if (window == null) return;

            var storageProvider = window.StorageProvider;
            var result = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Cross-Section Image",
                DefaultExtension = "png",
                SuggestedFileName = $"{Assembly.PartNumber}_CrossSection.png",
                FileTypeChoices = new List<FilePickerFileType>
                {
                    new("PNG Images") { Patterns = new[] { "*.png" } }
                }
            });

            if (result != null)
            {
                var filePath = result.Path.LocalPath;
                var imageBytes = CableVisualizer.GenerateCrossSectionImage(Assembly, 1200, 1200);
                await File.WriteAllBytesAsync(filePath, imageBytes);
                StatusMessage = $"Image exported: {filePath}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting image: {ex.Message}";
        }
    }

    [RelayCommand]
    private void AddLayer()
    {
        var newLayerNumber = Assembly.Layers.Count;
        var newLayer = new CableLayer
        {
            LayerNumber = newLayerNumber,
            TwistDirection = newLayerNumber == 0 ? TwistDirection.None :
                (newLayerNumber % 2 == 1 ? TwistDirection.RightHand : TwistDirection.LeftHand),
            LayLength = newLayerNumber == 0 ? 0 : 30
        };

        Assembly.Layers.Add(newLayer);
        SelectedLayer = newLayer;
        MarkChanged();
        UpdateCumulativeLayerDiameters();
        UpdateCrossSectionImage();
        StatusMessage = $"Added Layer {newLayerNumber}";
    }

    [RelayCommand]
    private void RemoveLayer()
    {
        if (SelectedLayer == null) return;

        var index = Assembly.Layers.IndexOf(SelectedLayer);
        Assembly.Layers.Remove(SelectedLayer);

        // Renumber remaining layers
        for (int i = 0; i < Assembly.Layers.Count; i++)
        {
            Assembly.Layers[i].LayerNumber = i;
        }

        SelectedLayer = Assembly.Layers.Count > 0
            ? Assembly.Layers[Math.Min(index, Assembly.Layers.Count - 1)]
            : null;

        MarkChanged();
        UpdateCumulativeLayerDiameters();
        UpdateCrossSectionImage();
        StatusMessage = "Layer removed";
    }

    [RelayCommand]
    private void AddCableFromLibrary()
    {
        if (SelectedLayer == null || SelectedLibraryCable == null) return;

        var clone = CloneCable(SelectedLibraryCable.Cable);
        SelectedLayer.Cables.Add(clone);
        SelectedCable = clone;
        MarkChanged();
        UpdateCumulativeLayerDiameters();
        UpdateCrossSectionImage();
        StatusMessage = $"Added {clone.PartNumber} to Layer {SelectedLayer.LayerNumber}";
    }

    [RelayCommand]
    private void AddMultipleCables(int count)
    {
        if (SelectedLayer == null || SelectedLibraryCable == null || count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            var clone = CloneCable(SelectedLibraryCable.Cable);
            SelectedLayer.Cables.Add(clone);
        }

        UpdateCumulativeLayerDiameters();
        MarkChanged();
        UpdateCrossSectionImage();
        StatusMessage = $"Added {count}x {SelectedLibraryCable.Cable.PartNumber} to Layer {SelectedLayer.LayerNumber}";
    }

    [RelayCommand]
    private void RemoveCable()
    {
        if (SelectedLayer == null || SelectedCable == null) return;

        var index = SelectedLayer.Cables.IndexOf(SelectedCable);
        SelectedLayer.Cables.Remove(SelectedCable);

        SelectedCable = SelectedLayer.Cables.Count > 0
            ? SelectedLayer.Cables[Math.Min(index, SelectedLayer.Cables.Count - 1)]
            : null;

        MarkChanged();
        UpdateCumulativeLayerDiameters();
        UpdateCrossSectionImage();
        StatusMessage = "Cable removed";
    }

    [RelayCommand]
    private void OptimizeFillers()
    {
        ConcentricityCalculator.OptimizeFillers(Assembly);
        MarkChanged();
        UpdateCrossSectionImage();
        ValidateAssembly();
        StatusMessage = "Fillers optimized";
    }

    [RelayCommand]
    private void ValidateAssembly()
    {
        ValidationMessages.Clear();
        var issues = ConcentricityCalculator.ValidateAssembly(Assembly);
        foreach (var issue in issues)
        {
            ValidationMessages.Add(issue);
        }

        StatusMessage = issues.Count == 0 ? "Validation passed" : $"{issues.Count} validation issue(s)";
    }

    [RelayCommand]
    private void AddHeatShrink()
    {
        if (SelectedHeatShrink == null)
        {
            StatusMessage = "Please select a heat shrink from the dropdown first";
            return;
        }

        // Create a copy of the selected heat shrink for this assembly
        var newHeatShrink = new HeatShrink
        {
            PartNumber = SelectedHeatShrink.PartNumber,
            Name = SelectedHeatShrink.Name,
            Manufacturer = SelectedHeatShrink.Manufacturer,
            Material = SelectedHeatShrink.Material,
            SuppliedInnerDiameter = SelectedHeatShrink.SuppliedInnerDiameter,
            RecoveredInnerDiameter = SelectedHeatShrink.RecoveredInnerDiameter,
            RecoveredWallThickness = SelectedHeatShrink.RecoveredWallThickness,
            ShrinkRatio = SelectedHeatShrink.ShrinkRatio,
            Color = SelectedHeatShrink.Color,
            TemperatureRating = SelectedHeatShrink.TemperatureRating,
            RecoveryTemperature = SelectedHeatShrink.RecoveryTemperature
        };

        Assembly.HeatShrinks.Add(newHeatShrink);
        MarkChanged();
        UpdateCrossSectionImage();
        StatusMessage = $"Added {newHeatShrink.PartNumber}";
    }

    [RelayCommand]
    private void RemoveHeatShrink(HeatShrink? hs)
    {
        if (hs == null) return;
        Assembly.HeatShrinks.Remove(hs);
        MarkChanged();
        UpdateCrossSectionImage();
        StatusMessage = "Removed heat shrink";
    }

    [RelayCommand]
    private void AddOverBraid()
    {
        if (SelectedOverBraid == null)
        {
            StatusMessage = "Please select an over-braid/sleeving from the dropdown first";
            return;
        }

        // Clone the selected over-braid to add to assembly
        var braid = new OverBraid
        {
            PartNumber = SelectedOverBraid.PartNumber,
            Name = SelectedOverBraid.Name,
            Manufacturer = SelectedOverBraid.Manufacturer,
            Type = SelectedOverBraid.Type,
            Material = SelectedOverBraid.Material,
            NominalInnerDiameter = SelectedOverBraid.NominalInnerDiameter,
            MinInnerDiameter = SelectedOverBraid.MinInnerDiameter,
            MaxInnerDiameter = SelectedOverBraid.MaxInnerDiameter,
            WallThickness = SelectedOverBraid.WallThickness,
            CoveragePercent = SelectedOverBraid.CoveragePercent,
            Color = SelectedOverBraid.Color,
            IsShielding = SelectedOverBraid.IsShielding,
            CarrierCount = SelectedOverBraid.CarrierCount,
            EndsPerCarrier = SelectedOverBraid.EndsPerCarrier,
            WireDiameter = SelectedOverBraid.WireDiameter,
            PicksPerInch = SelectedOverBraid.PicksPerInch,
            AppliedOverLayer = -1  // Outermost by default
        };

        Assembly.OverBraids.Add(braid);
        MarkChanged();
        UpdateCrossSectionImage();
        StatusMessage = $"Added {braid.Name}";
    }

    [RelayCommand]
    private void RemoveOverBraid(OverBraid? braid)
    {
        if (braid == null) return;
        Assembly.OverBraids.Remove(braid);
        MarkChanged();
        UpdateCrossSectionImage();
        StatusMessage = "Removed over-braid";
    }

    public void UpdateCrossSectionImage()
    {
        try
        {
            InteractiveImage = InteractiveVisualizer.GenerateInteractiveImage(Assembly, 600, 600);
            CrossSectionImage = InteractiveImage.ImageData;
        }
        catch
        {
            CrossSectionImage = null;
            InteractiveImage = null;
        }

        // Generate 3D views - Save STL file
        try
        {
            var stlBytes = Cable3DSTLVisualizer.GenerateSTL(Assembly);
            var outputDir = Path.Combine(Environment.CurrentDirectory, "output");
            Directory.CreateDirectory(outputDir);
            var stlPath = Path.Combine(outputDir, $"{Assembly.PartNumber}_3D.stl");
            File.WriteAllBytes(stlPath, stlBytes);
            
            // Generate a preview image from the STL (isometric view)
            IsometricImage = Cable3DVisualizer.GenerateIsometricCrossSection(Assembly, 800, 600);
            StatusMessage = $"STL saved: {stlPath}";
        }
        catch
        {
            IsometricImage = null;
            StatusMessage = "Error generating STL file";
        }
    }

    /// <summary>
    /// Handle click on the cross-section image
    /// </summary>
    public void HandleImageClick(float x, float y)
    {
        if (InteractiveImage == null) return;

        var element = InteractiveVisualizer.HitTest(InteractiveImage, x, y);
        SelectedElement = element;

        if (element != null)
        {
            ShowElementInfo = true;
            SelectedElementInfo = GetElementDescription(element);

            // If it's a cable, select it in the tree
            if (element.Cable != null)
            {
                foreach (var layer in Assembly.Layers)
                {
                    var cable = layer.Cables.FirstOrDefault(c => c.CableId == element.Cable.CableId);
                    if (cable != null)
                    {
                        SelectedLayer = layer;
                        SelectedCable = cable;
                        break;
                    }
                }
            }

            StatusMessage = $"Selected: {GetElementBriefDescription(element)}";
        }
        else
        {
            ShowElementInfo = false;
            SelectedElementInfo = "";
        }
    }

    private static string GetElementDescription(VisualElement element)
    {
        return element.Type switch
        {
            VisualElementType.Cable when element.Cable != null => GetCableDescription(element.Cable),
            VisualElementType.Core when element.Core != null => GetCoreDescription(element.Core),
            VisualElementType.Filler => $"Filler\nLayer: {element.LayerNumber}\nIndex: {element.ElementIndex}",
            VisualElementType.Annotation when element.Annotation != null =>
                $"Balloon #{element.Annotation.BalloonNumber}\n{element.Annotation.ReferenceText}\n{element.Annotation.NoteText}",
            _ => "Unknown element"
        };
    }

    private static string GetCableDescription(Cable cable)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Part Number: {cable.PartNumber}");
        sb.AppendLine($"Name: {cable.Name}");
        sb.AppendLine($"Manufacturer: {cable.Manufacturer}");
        sb.AppendLine($"Type: {cable.Type}");
        sb.AppendLine($"Overall Ø: {cable.OuterDiameter:F2} mm");
        sb.AppendLine($"Cores: {cable.Cores.Count}");
        if (cable.HasShield)
        {
            sb.AppendLine($"Shield: {cable.ShieldType} ({cable.ShieldCoverage}%)");
        }
        sb.AppendLine($"Jacket: {cable.JacketColor} ({cable.JacketThickness:F2} mm)");
        return sb.ToString();
    }

    private static string GetCoreDescription(CableCore core)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Core ID: {core.CoreId}");
        sb.AppendLine($"Gauge: {core.Gauge} AWG");
        sb.AppendLine($"Conductor Ø: {core.ConductorDiameter:F3} mm");
        sb.AppendLine($"Insulation: {core.InsulationColor} ({core.InsulationThickness:F2} mm)");
        sb.AppendLine($"Overall Ø: {core.OverallDiameter:F2} mm");
        sb.AppendLine($"Material: {core.ConductorMaterial}");
        if (!string.IsNullOrEmpty(core.SignalName))
        {
            sb.AppendLine($"Signal: {core.SignalName}");
            sb.AppendLine($"Type: {core.SignalType}");
        }
        if (!string.IsNullOrEmpty(core.PinA) || !string.IsNullOrEmpty(core.PinB))
        {
            sb.AppendLine($"Pins: {core.PinA} ↔ {core.PinB}");
        }
        return sb.ToString();
    }

    private static string GetElementBriefDescription(VisualElement element)
    {
        return element.Type switch
        {
            VisualElementType.Cable when element.Cable != null => $"{element.Cable.PartNumber}",
            VisualElementType.Core when element.Core != null => $"Core {element.Core.CoreId} ({element.Core.InsulationColor})",
            VisualElementType.Filler => $"Filler (Layer {element.LayerNumber})",
            VisualElementType.Annotation when element.Annotation != null => $"Note #{element.Annotation.BalloonNumber}",
            _ => "Element"
        };
    }

    [RelayCommand]
    private void DeleteSelectedElement()
    {
        if (SelectedElement == null) return;

        switch (SelectedElement.Type)
        {
            case VisualElementType.Cable when SelectedElement.Cable != null:
                foreach (var layer in Assembly.Layers)
                {
                    var cable = layer.Cables.FirstOrDefault(c => c.CableId == SelectedElement.Cable.CableId);
                    if (cable != null)
                    {
                        layer.Cables.Remove(cable);
                        break;
                    }
                }
                StatusMessage = $"Removed cable: {SelectedElement.Cable.PartNumber}";
                break;

            case VisualElementType.Annotation when SelectedElement.Annotation != null:
                Assembly.Annotations.Remove(SelectedElement.Annotation);
                UpdateNotesTable();
                StatusMessage = $"Removed annotation #{SelectedElement.Annotation.BalloonNumber}";
                break;

            case VisualElementType.Filler:
                // For fillers created via filler count, decrement the count
                if (SelectedLayer != null && SelectedLayer.FillerCount > 0)
                {
                    SelectedLayer.FillerCount--;
                }
                StatusMessage = "Removed filler";
                break;
        }

        SelectedElement = null;
        ShowElementInfo = false;
        MarkChanged();
        UpdateCrossSectionImage();
    }

    [RelayCommand]
    private void AddAnnotation()
    {
        var nextNumber = Assembly.Annotations.Count > 0
            ? Assembly.Annotations.Max(a => a.BalloonNumber) + 1
            : 1;

        var annotation = new Annotation
        {
            BalloonNumber = nextNumber,
            X = 0,
            Y = 0,
            ReferenceText = $"Note {nextNumber}",
            NoteText = "Enter note text here"
        };

        Assembly.Annotations.Add(annotation);
        UpdateNotesTable();
        MarkChanged();
        UpdateCrossSectionImage();
        StatusMessage = $"Added annotation #{nextNumber}";
    }

    [RelayCommand]
    private void RemoveAnnotation(Annotation? annotation)
    {
        if (annotation == null) return;
        Assembly.Annotations.Remove(annotation);
        UpdateNotesTable();
        MarkChanged();
        UpdateCrossSectionImage();
        StatusMessage = $"Removed annotation #{annotation.BalloonNumber}";
    }

    private void UpdateNotesTable()
    {
        NotesTable.Clear();
        foreach (var annotation in Assembly.Annotations.OrderBy(a => a.BalloonNumber))
        {
            NotesTable.Add(new NotesTableEntry
            {
                ItemNumber = annotation.BalloonNumber,
                Reference = annotation.ReferenceText,
                Description = annotation.NoteText
            });
        }
    }

    [RelayCommand]
    private void AssignSignal()
    {
        if (SelectedElement?.Core == null) return;
        // Signal assignment would typically open a dialog
        // For now, just mark it as needing assignment
        StatusMessage = $"Select signal for core {SelectedElement.Core.CoreId}";
    }

    public void SetCoreSignal(CableCore core, string signalName, SignalType signalType, string pinA = "", string pinB = "")
    {
        core.SignalName = signalName;
        core.SignalType = signalType;
        core.PinA = pinA;
        core.PinB = pinB;
        MarkChanged();
        StatusMessage = $"Assigned signal '{signalName}' to core {core.CoreId}";
    }

    public void AddCustomCable(Cable cable)
    {
        if (SelectedLayer == null)
        {
            StatusMessage = "Select a layer first";
            return;
        }

        SelectedLayer.Cables.Add(cable);
        SelectedCable = cable;
        MarkChanged();
        UpdateCrossSectionImage();
        StatusMessage = $"Added custom cable: {cable.PartNumber}";
    }

    public void MarkChanged()
    {
        HasUnsavedChanges = true;
        OnPropertyChanged(nameof(Assembly));
    }

    public void RefreshView()
    {
        OnPropertyChanged(nameof(Assembly));
        UpdateCrossSectionImage();
        ValidateAssembly();
    }

    private static Cable CloneCable(Cable source)
    {
        return new Cable
        {
            CableId = Guid.NewGuid().ToString("N")[..8],
            PartNumber = source.PartNumber,
            Manufacturer = source.Manufacturer,
            Name = source.Name,
            Type = source.Type,
            JacketThickness = source.JacketThickness,
            JacketColor = source.JacketColor,
            HasShield = source.HasShield,
            ShieldType = source.ShieldType,
            ShieldThickness = source.ShieldThickness,
            ShieldCoverage = source.ShieldCoverage,
            HasDrainWire = source.HasDrainWire,
            DrainWireDiameter = source.DrainWireDiameter,
            IsFiller = source.IsFiller,
            FillerMaterial = source.FillerMaterial,
            SpecifiedOuterDiameter = source.SpecifiedOuterDiameter,
            Cores = source.Cores.Select(c => new CableCore
            {
                CoreId = c.CoreId,
                ConductorDiameter = c.ConductorDiameter,
                InsulationThickness = c.InsulationThickness,
                InsulationColor = c.InsulationColor,
                Gauge = c.Gauge,
                ConductorMaterial = c.ConductorMaterial,
                SignalName = c.SignalName,
                SignalDescription = c.SignalDescription,
                SignalType = c.SignalType,
                PinA = c.PinA,
                PinB = c.PinB,
                WireLabel = c.WireLabel
            }).ToList()
        };
    }
}

public class CableLibraryItem
{
    public string Key { get; set; } = "";
    public Cable Cable { get; set; } = new();
    public string DisplayName { get; set; } = "";
}
