using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CableConcentricityCalculator.Models;
using CableConcentricityCalculator.Services;
using System.Diagnostics;

namespace CableConcentricityCalculator.Gui.Views;

public partial class CableBrowserDialog : Window
{
    private Dictionary<string, Cable> _allCables = new();
    private List<CableDisplayItem> _filteredCables = new();

    // Control references
    private readonly ComboBox _manufacturerCombo;
    private readonly ComboBox _coresCombo;
    private readonly ComboBox _awgCombo;
    private readonly ComboBox _typeCombo;
    private readonly TextBox _searchBox;
    private readonly TextBlock _resultCountText;
    private readonly DataGrid _cablesGrid;
    private readonly NumericUpDown _multipleQuantityUpDown;
    private readonly Button _useCableButton;
    private readonly Button _addMultipleButton;
    private readonly Button _cancelButton;
    // Info pane controls
    private readonly TextBlock _infoPartNumber;
    private readonly TextBlock _infoName;
    private readonly TextBlock _infoManufacturer;
    private readonly TextBlock _infoType;
    private readonly TextBlock _infoCores;
    private readonly TextBlock _infoOD;
    private readonly Button _googleSearchButton;

    public List<Cable> SelectedCables { get; } = new();

    public CableBrowserDialog()
    {
        InitializeComponent();

        // Find controls by name
        _manufacturerCombo = this.FindControl<ComboBox>("ManufacturerCombo")!;
        _coresCombo = this.FindControl<ComboBox>("CoresCombo")!;
        _awgCombo = this.FindControl<ComboBox>("AwgCombo")!;
        _typeCombo = this.FindControl<ComboBox>("TypeCombo")!;
        _searchBox = this.FindControl<TextBox>("SearchBox")!;
        _resultCountText = this.FindControl<TextBlock>("ResultCountText")!;
        _cablesGrid = this.FindControl<DataGrid>("CablesGrid")!;
        _multipleQuantityUpDown = this.FindControl<NumericUpDown>("MultipleQuantityUpDown")!;
        _useCableButton = this.FindControl<Button>("UseCableButton")!;
        _addMultipleButton = this.FindControl<Button>("AddMultipleButton")!;
        _cancelButton = this.FindControl<Button>("CancelButton")!;
        // Info pane controls
        _infoPartNumber = this.FindControl<TextBlock>("InfoPartNumber")!;
        _infoName = this.FindControl<TextBlock>("InfoName")!;
        _infoManufacturer = this.FindControl<TextBlock>("InfoManufacturer")!;
        _infoType = this.FindControl<TextBlock>("InfoType")!;
        _infoCores = this.FindControl<TextBlock>("InfoCores")!;
        _infoOD = this.FindControl<TextBlock>("InfoOD")!;
        _googleSearchButton = this.FindControl<Button>("GoogleSearchButton")!;

        LoadCables();
        SetupFilters();

        _cancelButton.Click += (_, _) => Close();
        _useCableButton.Click += OnUseCableClick;
        _addMultipleButton.Click += OnAddMultipleClick;
        _cablesGrid.SelectionChanged += OnSelectionChanged;
        _googleSearchButton.Click += (_, _) => OpenGoogleSearch();
    }

    private void LoadCables()
    {
        _allCables = CableLibrary.GetCompleteCableLibrary();
        ApplyFilters();
    }

    private void SetupFilters()
    {
        // Manufacturers
        var manufacturers = new List<string> { "All" };
        manufacturers.AddRange(_allCables.Values.Select(c => c.Manufacturer).Distinct().OrderBy(x => x));
        _manufacturerCombo.ItemsSource = manufacturers;
        _manufacturerCombo.SelectedIndex = 0;

        // Core counts
        var coreCounts = new List<string> { "All", "1", "2", "3", "4", "5+", "8+" };
        _coresCombo.ItemsSource = coreCounts;
        _coresCombo.SelectedIndex = 0;

        // AWG gauges
        var awgGauges = new List<string> { "All" };
        awgGauges.AddRange(_allCables.Values
            .SelectMany(c => c.Cores)
            .Select(core => core.Gauge)
            .Where(g => !string.IsNullOrEmpty(g))
            .Distinct()
            .OrderBy(g => int.TryParse(g, out int n) ? n : int.MaxValue)
            .ThenBy(x => x));
        _awgCombo.ItemsSource = awgGauges;
        _awgCombo.SelectedIndex = 0;

        // Types
        var types = new List<string> { "All" };
        types.AddRange(Enum.GetNames<CableType>());
        _typeCombo.ItemsSource = types;
        _typeCombo.SelectedIndex = 0;
    }

    private void OnFilterChanged(object? sender, SelectionChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void OnSearchChanged(object? sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var manufacturer = _manufacturerCombo.SelectedItem as string ?? "All";
        var coresFilter = _coresCombo.SelectedItem as string ?? "All";
        var awgFilter = _awgCombo.SelectedItem as string ?? "All";
        var typeFilter = _typeCombo.SelectedItem as string ?? "All";
        var search = _searchBox.Text?.ToLowerInvariant() ?? "";

        var filtered = _allCables.Values.AsEnumerable();

        // Apply manufacturer filter
        if (manufacturer != "All")
        {
            filtered = filtered.Where(c => c.Manufacturer == manufacturer);
        }

        // Apply cores filter
        if (coresFilter != "All")
        {
            filtered = coresFilter switch
            {
                "5+" => filtered.Where(c => c.Cores.Count >= 5),
                "8+" => filtered.Where(c => c.Cores.Count >= 8),
                _ when int.TryParse(coresFilter, out int n) => filtered.Where(c => c.Cores.Count == n),
                _ => filtered
            };
        }

        // Apply AWG filter
        if (awgFilter != "All")
        {
            filtered = filtered.Where(c => c.Cores.Any(core => core.Gauge == awgFilter));
        }

        // Apply type filter
        if (typeFilter != "All" && Enum.TryParse<CableType>(typeFilter, out var cableType))
        {
            filtered = filtered.Where(c => c.Type == cableType);
        }

        // Apply search filter
        if (!string.IsNullOrEmpty(search))
        {
            filtered = filtered.Where(c =>
                c.PartNumber.ToLowerInvariant().Contains(search) ||
                c.Name.ToLowerInvariant().Contains(search));
        }

        _filteredCables = filtered
            .Select(c => new CableDisplayItem(c))
            .OrderBy(c => c.PartNumber)
            .ToList();

        _cablesGrid.ItemsSource = _filteredCables;
        _resultCountText.Text = $"{_filteredCables.Count} cables";
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        bool hasSelection = _cablesGrid.SelectedItem != null;
        _useCableButton.IsEnabled = hasSelection;
        _addMultipleButton.IsEnabled = hasSelection;
        _googleSearchButton.IsEnabled = hasSelection;

        if (_cablesGrid.SelectedItem is CableDisplayItem item)
        {
            ShowInfoFor(item.Cable);
        }
    }

    private void ShowInfoFor(Cable cable)
    {
        if (cable == null) return;

        _infoPartNumber.Text = cable.PartNumber;
        _infoName.Text = cable.Name;
        _infoManufacturer.Text = cable.Manufacturer;
        _infoType.Text = cable.Type.ToString();
        _infoCores.Text = cable.Cores.Count.ToString();
        _infoOD.Text = $"{cable.OuterDiameter:F2} mm";
    }

    private void OpenGoogleSearch()
    {
        if (!(_cablesGrid.SelectedItem is CableDisplayItem item)) return;

        // Build search term based on manufacturer
        string searchTerm;
        if (item.Manufacturer.Equals("LAPP", StringComparison.OrdinalIgnoreCase))
        {
            // For LAPP cables: use "PartNumber Manufacturer" OR "PartNumber Name"
            // Use the Name if available, otherwise use Manufacturer
            if (!string.IsNullOrWhiteSpace(item.Name))
            {
                searchTerm = $"{item.PartNumber} {item.Name}";
            }
            else
            {
                searchTerm = $"{item.PartNumber} {item.Manufacturer}";
            }
        }
        else
        {
            // For MIL and other cables: use just the part number
            searchTerm = item.PartNumber;
        }

        var encodedSearch = Uri.EscapeDataString(searchTerm);
        string url = $"https://www.google.com/search?q={encodedSearch}";
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // ignore errors silently
        }
    }

    private void OnUseCableClick(object? sender, RoutedEventArgs e)
    {
        if (_cablesGrid.SelectedItem is CableDisplayItem item)
        {
            AddCables(item.Cable, 1);
            Close(SelectedCables);
        }
    }

    private void OnAddMultipleClick(object? sender, RoutedEventArgs e)
    {
        if (_cablesGrid.SelectedItem is CableDisplayItem item)
        {
            var quantity = (int)(_multipleQuantityUpDown.Value ?? 1);
            AddCables(item.Cable, quantity);
            Close(SelectedCables);
        }
    }

    private void AddCables(Cable template, int quantity)
    {
        for (int i = 0; i < quantity; i++)
        {
            SelectedCables.Add(CloneCable(template));
        }
    }

    private static Cable CloneCable(Cable source)
    {
        return new Cable
        {
            CableId = Guid.NewGuid().ToString("N")[..8],
            PartNumber = source.PartNumber,
            Manufacturer = source.Manufacturer,
            Name = source.Name,
            Description = source.Description,
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
                ConductorMaterial = c.ConductorMaterial
            }).ToList()
        };
    }
}

public class CableDisplayItem
{
    public Cable Cable { get; }
    public string PartNumber => Cable.PartNumber;
    public string Name => Cable.Name;
    public string Manufacturer => Cable.Manufacturer;
    public CableType Type => Cable.Type;

    public string ConductorDiameterDisplay
    {
        get
        {
            if (Cable.Cores.Count == 0) return "-";
            var dia = Cable.Cores.First().ConductorDiameter;
            var gauge = Cable.Cores.First().Gauge;
            return $"{dia:F2}mm ({gauge})";
        }
    }

    public string OuterDiameterDisplay => $"{Cable.OuterDiameter:F2}mm";

    public CableDisplayItem(Cable cable)
    {
        Cable = cable;
    }
}
