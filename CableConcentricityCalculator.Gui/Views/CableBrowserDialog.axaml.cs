using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CableConcentricityCalculator.Models;
using CableConcentricityCalculator.Services;

namespace CableConcentricityCalculator.Gui.Views;

public partial class CableBrowserDialog : Window
{
    private Dictionary<string, Cable> _allCables = new();
    private List<CableDisplayItem> _filteredCables = new();

    public List<Cable> SelectedCables { get; } = new();

    public CableBrowserDialog()
    {
        InitializeComponent();
        LoadCables();
        SetupFilters();

        CancelButton.Click += (_, _) => Close();
        AddButton.Click += OnAddClick;
        CustomButton.Click += OnCustomClick;
        CablesGrid.SelectionChanged += OnSelectionChanged;
    }

    private void LoadCables()
    {
        _allCables = CableLibrary.GetCompleteCableLibrary();
        ApplyFilters();
    }

    private void SetupFilters()
    {
        // Categories
        var categories = new List<string> { "All" };
        categories.AddRange(ConfigurationService.GetCableCategories().Where(c => c != "All"));
        CategoryCombo.ItemsSource = categories;
        CategoryCombo.SelectedIndex = 0;

        // Manufacturers
        var manufacturers = new List<string> { "All" };
        manufacturers.AddRange(_allCables.Values.Select(c => c.Manufacturer).Distinct().OrderBy(x => x));
        ManufacturerCombo.ItemsSource = manufacturers;
        ManufacturerCombo.SelectedIndex = 0;

        // Core counts
        var coreCounts = new List<string> { "All", "1", "2", "3", "4", "5+", "8+" };
        CoresCombo.ItemsSource = coreCounts;
        CoresCombo.SelectedIndex = 0;

        // Types
        var types = new List<string> { "All" };
        types.AddRange(Enum.GetNames<CableType>());
        TypeCombo.ItemsSource = types;
        TypeCombo.SelectedIndex = 0;
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
        var category = CategoryCombo.SelectedItem as string ?? "All";
        var manufacturer = ManufacturerCombo.SelectedItem as string ?? "All";
        var coresFilter = CoresCombo.SelectedItem as string ?? "All";
        var typeFilter = TypeCombo.SelectedItem as string ?? "All";
        var search = SearchBox.Text?.ToLowerInvariant() ?? "";

        var source = category == "All"
            ? _allCables
            : ConfigurationService.GetCablesByCategory(category);

        var filtered = source.Values.AsEnumerable();

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
            .Take(500)
            .Select(c => new CableDisplayItem(c))
            .OrderBy(c => c.PartNumber)
            .ToList();

        CablesGrid.ItemsSource = _filteredCables;
        ResultCountText.Text = $"{_filteredCables.Count} cables";
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        AddButton.IsEnabled = CablesGrid.SelectedItem != null;
    }

    private void OnCableDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (CablesGrid.SelectedItem is CableDisplayItem item)
        {
            AddCables(item.Cable, 1);
            Close(SelectedCables);
        }
    }

    private void OnAddClick(object? sender, RoutedEventArgs e)
    {
        if (CablesGrid.SelectedItem is CableDisplayItem item)
        {
            var quantity = (int)(QuantityUpDown.Value ?? 1);
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

    private async void OnCustomClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new CustomCableDialog();
        var result = await dialog.ShowDialog<Cable?>(this);

        if (result != null)
        {
            var quantity = (int)(QuantityUpDown.Value ?? 1);
            for (int i = 0; i < quantity; i++)
            {
                SelectedCables.Add(CloneCable(result));
            }
            Close(SelectedCables);
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
