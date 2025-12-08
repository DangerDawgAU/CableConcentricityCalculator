using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

    public MainWindowViewModel()
    {
        _assembly = CreateNewAssembly();
        _cableLibrary = new ObservableCollection<CableLibraryItem>(LoadCableLibrary());
        _validationMessages = new ObservableCollection<string>();
        UpdateCrossSectionImage();
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
        UpdateCrossSectionImage();
        ValidateAssembly();
    }

    partial void OnSelectedLayerChanged(CableLayer? value)
    {
        SelectedCable = value?.Cables.FirstOrDefault();
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
            var dialog = new Avalonia.Controls.OpenFileDialog
            {
                Title = "Open Cable Assembly",
                Filters = new System.Collections.Generic.List<Avalonia.Controls.FileDialogFilter>
                {
                    new() { Name = "JSON Files", Extensions = { "json" } },
                    new() { Name = "All Files", Extensions = { "*" } }
                }
            };

            var window = Avalonia.Application.Current?.ApplicationLifetime
                is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow : null;

            if (window == null) return;

            var result = await dialog.ShowAsync(window);
            if (result?.Length > 0)
            {
                Assembly = await ConfigurationService.LoadAssemblyAsync(result[0]);
                CurrentFilePath = result[0];
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
            var dialog = new Avalonia.Controls.SaveFileDialog
            {
                Title = "Save Cable Assembly",
                DefaultExtension = "json",
                Filters = new System.Collections.Generic.List<Avalonia.Controls.FileDialogFilter>
                {
                    new() { Name = "JSON Files", Extensions = { "json" } }
                },
                InitialFileName = $"{Assembly.PartNumber}.json"
            };

            var window = Avalonia.Application.Current?.ApplicationLifetime
                is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow : null;

            if (window == null) return;

            var result = await dialog.ShowAsync(window);
            if (!string.IsNullOrEmpty(result))
            {
                await ConfigurationService.SaveAssemblyAsync(Assembly, result);
                CurrentFilePath = result;
                HasUnsavedChanges = false;
                StatusMessage = $"Saved: {result}";
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
            var dialog = new Avalonia.Controls.SaveFileDialog
            {
                Title = "Export PDF Report",
                DefaultExtension = "pdf",
                Filters = new System.Collections.Generic.List<Avalonia.Controls.FileDialogFilter>
                {
                    new() { Name = "PDF Files", Extensions = { "pdf" } }
                },
                InitialFileName = $"{Assembly.PartNumber}_Report.pdf"
            };

            var window = Avalonia.Application.Current?.ApplicationLifetime
                is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow : null;

            if (window == null) return;

            var result = await dialog.ShowAsync(window);
            if (!string.IsNullOrEmpty(result))
            {
                await Task.Run(() => PdfReportGenerator.GenerateReport(Assembly, result));
                StatusMessage = $"PDF exported: {result}";
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
            var dialog = new Avalonia.Controls.SaveFileDialog
            {
                Title = "Export Cross-Section Image",
                DefaultExtension = "png",
                Filters = new System.Collections.Generic.List<Avalonia.Controls.FileDialogFilter>
                {
                    new() { Name = "PNG Images", Extensions = { "png" } }
                },
                InitialFileName = $"{Assembly.PartNumber}_CrossSection.png"
            };

            var window = Avalonia.Application.Current?.ApplicationLifetime
                is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow : null;

            if (window == null) return;

            var result = await dialog.ShowAsync(window);
            if (!string.IsNullOrEmpty(result))
            {
                var imageBytes = CableVisualizer.GenerateCrossSectionImage(Assembly, 1200, 1200);
                await File.WriteAllBytesAsync(result, imageBytes);
                StatusMessage = $"Image exported: {result}";
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
    private void LoadDemoAssembly()
    {
        Assembly = ConfigurationService.CreateSampleAssembly();
        CurrentFilePath = null;
        HasUnsavedChanges = false;
        SelectedLayer = Assembly.Layers.FirstOrDefault();
        StatusMessage = "Loaded demo assembly";
    }

    [RelayCommand]
    private void AddHeatShrink()
    {
        var hs = new HeatShrink
        {
            PartNumber = "HS-NEW",
            Name = "New Heat Shrink",
            Material = "Polyolefin",
            SuppliedInnerDiameter = 12,
            RecoveredInnerDiameter = 6,
            RecoveredWallThickness = 0.8,
            ShrinkRatio = "2:1",
            Color = "Black"
        };

        Assembly.HeatShrinks.Add(hs);
        MarkChanged();
        UpdateCrossSectionImage();
        StatusMessage = "Added heat shrink";
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
        var braid = new OverBraid
        {
            PartNumber = "BRAID-NEW",
            Name = "New Over-Braid",
            Material = "Tinned Copper",
            CoveragePercent = 85,
            WallThickness = 0.5,
            IsShielding = true,
            Color = "Silver"
        };

        Assembly.OverBraids.Add(braid);
        MarkChanged();
        UpdateCrossSectionImage();
        StatusMessage = "Added over-braid";
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
            CrossSectionImage = CableVisualizer.GenerateCrossSectionImage(Assembly, 600, 600);
        }
        catch
        {
            CrossSectionImage = null;
        }
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
            Cores = source.Cores.Select(c => new CableCore
            {
                CoreId = c.CoreId,
                ConductorDiameter = c.ConductorDiameter,
                InsulationThickness = c.InsulationThickness,
                InsulationColor = c.InsulationColor,
                Gauge = c.Gauge,
                ConductorMaterial = c.ConductorMaterial
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
