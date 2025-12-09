using Avalonia.Controls;
using Avalonia.Interactivity;
using CableConcentricityCalculator.Models;

namespace CableConcentricityCalculator.Gui.Views;

public partial class CustomCableDialog : Window
{
    private static readonly Dictionary<string, (double Diameter, double Insulation)> AwgSizes = new()
    {
        { "30 AWG", (0.254, 0.10) },
        { "28 AWG", (0.320, 0.10) },
        { "26 AWG", (0.405, 0.15) },
        { "24 AWG", (0.511, 0.15) },
        { "22 AWG", (0.644, 0.20) },
        { "20 AWG", (0.812, 0.20) },
        { "18 AWG", (1.024, 0.25) },
        { "16 AWG", (1.291, 0.30) },
        { "14 AWG", (1.628, 0.35) },
        { "12 AWG", (2.053, 0.40) },
        { "10 AWG", (2.588, 0.45) }
    };

    public Cable? CreatedCable { get; private set; }

    public CustomCableDialog()
    {
        InitializeComponent();

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
            if (AwgSizes.TryGetValue(gauge, out var sizes))
            {
                ConductorDiaUpDown.Value = (decimal)sizes.Diameter;
                InsulationThickUpDown.Value = (decimal)sizes.Insulation;
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
        return count switch
        {
            1 => elementDiameter,
            2 => 2 * elementDiameter,
            3 => 2.155 * elementDiameter,
            4 => 2.414 * elementDiameter,
            5 => 2.701 * elementDiameter,
            6 => 3 * elementDiameter,
            7 => 3 * elementDiameter,
            _ => CalculateGeneralBundleDiameter(count, elementDiameter)
        };
    }

    private static double CalculateGeneralBundleDiameter(int count, double elementDiameter)
    {
        double totalArea = count * Math.PI * Math.Pow(elementDiameter / 2, 2);
        double bundleArea = totalArea / 0.785;
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
        string[] coreColors = { "White", "Black", "Red", "Green", "Blue", "Yellow", "Orange", "Brown", "Violet", "Gray" };

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

        Close(CreatedCable);
    }
}
