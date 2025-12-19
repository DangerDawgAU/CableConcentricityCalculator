using Avalonia.Controls;
using Avalonia.Interactivity;
using CableConcentricityCalculator.Models;
using CableConcentricityCalculator.Services;
using CableConcentricityCalculator.Utilities;

namespace CableConcentricityCalculator.Gui.Views;

public partial class CustomCableDialog : Window
{

    public Cable? CreatedCable { get; private set; }

    private readonly CheckBox _saveToLibraryCheck;

    public CustomCableDialog()
    {
        InitializeComponent();

        _saveToLibraryCheck = this.FindControl<CheckBox>("SaveToLibraryCheck")!;

        CancelButton.Click += (_, _) => Close();
        CreateButton.Click += OnCreateClick;

        GaugeCombo.SelectionChanged += OnGaugeChanged;
        CoreCountUpDown.ValueChanged += UpdateCalculatedOd;
        ConductorDiaUpDown.ValueChanged += UpdateCalculatedOd;
        InsulationThickUpDown.ValueChanged += UpdateCalculatedOd;
        JacketThickUpDown.ValueChanged += UpdateCalculatedOd;
        HasShieldCheck.IsCheckedChanged += UpdateCalculatedOd;
        ShieldThickUpDown.ValueChanged += UpdateCalculatedOd;

        UpdateCalculatedOd(null, null!);
    }

    private void OnGaugeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (GaugeCombo.SelectedItem is ComboBoxItem item && item.Content is string gauge)
        {
            // Remove " AWG" suffix to match CableLibrary keys
            var awgKey = gauge.Replace(" AWG", "");
            if (CableLibrary.AwgSizes.TryGetValue(awgKey, out var sizes))
            {
                ConductorDiaUpDown.Value = (decimal)sizes.ConductorDia;
                InsulationThickUpDown.Value = (decimal)sizes.InsulationThick;
            }
        }
        UpdateCalculatedOd(null, null!);
    }

    private void UpdateCalculatedOd(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        UpdateCalculatedOd(null, (RoutedEventArgs)null!);
    }

    private void UpdateCalculatedOd(object? sender, RoutedEventArgs? e)
    {
        try
        {
            int coreCount = (int)(CoreCountUpDown.Value ?? 1);
            double conductorDia = (double)(ConductorDiaUpDown.Value ?? 0.644m);
            double insulationThick = (double)(InsulationThickUpDown.Value ?? 0.20m);
            double jacketThick = (double)(JacketThickUpDown.Value ?? 0.30m);
            double shieldThick = (HasShieldCheck.IsChecked == true) ? (double)(ShieldThickUpDown.Value ?? 0.15m) : 0;

            double coreOd = conductorDia + 2 * insulationThick;
            double bundleDia = CalculateBundleDiameter(coreCount, coreOd);
            double totalOd = bundleDia + 2 * shieldThick + 2 * jacketThick;

            CalculatedOdText.Text = $"{totalOd:F2} mm";
        }
        catch
        {
            CalculatedOdText.Text = "-- mm";
        }
    }

    private static double CalculateBundleDiameter(int count, double elementDiameter)
    {
        // Geometric formulas for optimal wire bundle packing
        // Based on standard circular packing arrangements
        return count switch
        {
            1 => elementDiameter,                              // Single element
            2 => 2 * elementDiameter,                          // Two elements side-by-side
            3 => 2.155 * elementDiameter,                      // Triangle: 1 + 2/√3 ≈ 2.155
            4 => 2.414 * elementDiameter,                      // Square diagonal: 1 + √2 ≈ 2.414
            5 => 2.701 * elementDiameter,                      // Pentagon (empirical)
            6 => 2.155 * elementDiameter,                      // Hexagon (6 around perimeter, no center): same as triangle
            7 => 3 * elementDiameter,                          // 1 center + 6 surrounding (hexagon ring)
            _ => CalculateGeneralBundleDiameter(count, elementDiameter)
        };
    }

    private static double CalculateGeneralBundleDiameter(int count, double elementDiameter)
    {
        double totalArea = count * CableUtilities.GetCircularArea(elementDiameter);
        double bundleArea = totalArea / CableUtilities.PackingEfficiency;
        return 2 * Math.Sqrt(bundleArea / Math.PI);
    }

    private void OnCreateClick(object? sender, RoutedEventArgs e)
    {
        var partNumber = PartNumberBox.Text ?? "CUSTOM-001";
        var name = NameBox.Text ?? "Custom Cable";
        var manufacturer = ManufacturerBox.Text ?? "Custom";

        var cableType = (TypeCombo.SelectedIndex) switch
        {
            0 => CableType.SingleCore,
            1 => CableType.MultiCore,
            2 => CableType.TwistedPair,
            3 => CableType.Coaxial,
            4 => CableType.Filler,
            _ => CableType.SingleCore
        };

        var conductorMaterial = (ConductorMaterialCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Copper";
        var insulationColor = (InsulationColorCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "White";
        var jacketColor = (JacketColorCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Black";
        var gaugeStr = (GaugeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString()?.Replace(" AWG", "") ?? "22";

        int coreCount = (int)(CoreCountUpDown.Value ?? 1);
        double conductorDia = (double)(ConductorDiaUpDown.Value ?? 0.644m);
        double insulationThick = (double)(InsulationThickUpDown.Value ?? 0.20m);
        double jacketThick = (double)(JacketThickUpDown.Value ?? 0.30m);

        var shieldType = ShieldType.None;
        double shieldThick = 0;
        double shieldCoverage = 0;

        if (HasShieldCheck.IsChecked == true)
        {
            shieldType = (ShieldTypeCombo.SelectedIndex) switch
            {
                0 => ShieldType.Braid,
                1 => ShieldType.Foil,
                2 => ShieldType.FoilAndBraid,
                3 => ShieldType.Spiral,
                _ => ShieldType.Braid
            };
            shieldThick = (double)(ShieldThickUpDown.Value ?? 0.15m);
            shieldCoverage = (double)(ShieldCoverageUpDown.Value ?? 85);
        }

        // Create cores
        var cores = new List<CableCore>();
        string[] coreColors = { "White", "Black", "Red", "Green", "Blue", "Yellow", "Orange", "Brown", "Violet", "Pink" };

        for (int i = 0; i < coreCount; i++)
        {
            cores.Add(new CableCore
            {
                CoreId = (i + 1).ToString(),
                ConductorDiameter = conductorDia,
                InsulationThickness = insulationThick,
                InsulationColor = coreCount == 1 ? insulationColor : coreColors[i % coreColors.Length],
                Gauge = gaugeStr,
                ConductorMaterial = conductorMaterial
            });
        }

        CreatedCable = new Cable
        {
            PartNumber = partNumber,
            Name = name,
            Manufacturer = manufacturer,
            Type = cableType,
            JacketColor = jacketColor,
            JacketThickness = jacketThick,
            HasShield = HasShieldCheck.IsChecked == true,
            ShieldType = shieldType,
            ShieldThickness = shieldThick,
            ShieldCoverage = shieldCoverage,
            HasDrainWire = HasDrainWireCheck.IsChecked == true,
            DrainWireDiameter = HasDrainWireCheck.IsChecked == true ? 0.51 : 0,
            IsFiller = cableType == CableType.Filler,
            Cores = cores
        };

        // Save to user library if checkbox is checked
        if (_saveToLibraryCheck.IsChecked == true)
        {
            try
            {
                UserLibraryService.AddCable(CreatedCable);
            }
            catch (Exception ex)
            {
                // Log error but don't block cable creation
                Console.WriteLine($"Failed to save cable to library: {ex.Message}");
            }
        }

        Close(CreatedCable);
    }
}
