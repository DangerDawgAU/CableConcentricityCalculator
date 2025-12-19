using Avalonia.Controls;
using Avalonia.Interactivity;
using CableConcentricityCalculator.Models;
using CableConcentricityCalculator.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CableConcentricityCalculator.Gui.Views;

public partial class AddToLibraryDialog : Window
{
    public bool ItemAdded { get; private set; }

    public AddToLibraryDialog()
    {
        InitializeComponent();

        var cancelButton = this.FindControl<Button>("CancelButton")!;
        var addButton = this.FindControl<Button>("AddButton")!;

        cancelButton.Click += (_, _) => Close();
        addButton.Click += OnAddClick;
    }

    private void OnAddClick(object? sender, RoutedEventArgs e)
    {
        var tabControl = this.FindControl<TabControl>("TabControl")!;
        var selectedIndex = tabControl.SelectedIndex;

        try
        {
            switch (selectedIndex)
            {
                case 0: // Cable
                    AddCable();
                    break;
                case 1: // Heat Shrink
                    AddHeatShrink();
                    break;
                case 2: // Over-Braid
                    AddOverBraid();
                    break;
            }

            ItemAdded = true;
            Close();
        }
        catch (Exception ex)
        {
            // Show error to user
            var errorDialog = new Window
            {
                Title = "Error",
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(16),
                    Spacing = 16,
                    Children =
                    {
                        new TextBlock { Text = "Failed to add item to library:", FontWeight = Avalonia.Media.FontWeight.Bold },
                        new TextBlock { Text = ex.Message, TextWrapping = Avalonia.Media.TextWrapping.Wrap }
                    }
                }
            };
            errorDialog.ShowDialog(this);
        }
    }

    private void AddCable()
    {
        var partNumber = this.FindControl<TextBox>("CablePartNumberBox")!.Text;
        var manufacturer = this.FindControl<TextBox>("CableManufacturerBox")!.Text;
        var conductorCount = (int)this.FindControl<NumericUpDown>("CableConductorCountUpDown")!.Value!;
        var awgCombo = this.FindControl<ComboBox>("CableAwgCombo")!;
        var conductorDiameter = (double)this.FindControl<NumericUpDown>("CableConductorDiameterUpDown")!.Value!;
        var conductorCsa = (double)this.FindControl<NumericUpDown>("CableConductorCsaUpDown")!.Value!;
        var coreColorsText = this.FindControl<TextBox>("CableCoreColorsBox")!.Text;
        var randomColors = this.FindControl<CheckBox>("CableRandomColorsCheck")!.IsChecked == true;
        var insulationThickness = (double)this.FindControl<NumericUpDown>("CableInsulationThicknessUpDown")!.Value!;
        var autoInsulation = this.FindControl<CheckBox>("CableAutoInsulationCheck")!.IsChecked == true;
        var shieldingIndex = this.FindControl<ComboBox>("CableShieldingCombo")!.SelectedIndex;
        var cableOd = (double)this.FindControl<NumericUpDown>("CableOdUpDown")!.Value!;
        var cableColor = (this.FindControl<ComboBox>("CableColorCombo")!.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Black";

        // Validate
        if (string.IsNullOrWhiteSpace(partNumber))
            throw new InvalidOperationException("Part number is required");
        if (string.IsNullOrWhiteSpace(manufacturer))
            throw new InvalidOperationException("Manufacturer is required");

        // Determine conductor diameter
        double finalConductorDiameter = 0;
        string gauge = "";

        // AWG takes precedence
        if (awgCombo.SelectedIndex > 0)
        {
            gauge = (awgCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            if (CableLibrary.AwgSizes.TryGetValue(gauge, out var awgData))
            {
                finalConductorDiameter = awgData.ConductorDia;
                if (autoInsulation)
                    insulationThickness = awgData.InsulationThick;
            }
        }
        // Then diameter
        else if (conductorDiameter > 0)
        {
            finalConductorDiameter = conductorDiameter;
            gauge = CableLibrary.GetAwgFromDiameter(conductorDiameter);
        }
        // Finally CSA
        else if (conductorCsa > 0)
        {
            // Convert CSA to diameter: d = sqrt(4*A/π)
            finalConductorDiameter = Math.Sqrt(4 * conductorCsa / Math.PI);
            gauge = CableLibrary.GetAwgFromDiameter(finalConductorDiameter);
        }
        else
        {
            throw new InvalidOperationException("You must specify AWG, diameter, or cross-sectional area");
        }

        // Auto-generate insulation if requested
        if (autoInsulation && insulationThickness <= 0)
        {
            insulationThickness = 0.1 + (finalConductorDiameter * 0.3);
        }

        // Determine core colors
        List<string> coreColors;
        if (randomColors)
        {
            coreColors = GenerateRandomColors(conductorCount);
        }
        else if (!string.IsNullOrWhiteSpace(coreColorsText))
        {
            coreColors = coreColorsText.Split(',')
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToList();

            // If not enough colors, pad with repeats
            while (coreColors.Count < conductorCount)
            {
                coreColors.AddRange(coreColors.Take(conductorCount - coreColors.Count));
            }
        }
        else
        {
            coreColors = GenerateRandomColors(conductorCount);
        }

        // Determine shielding
        var hasShield = shieldingIndex > 0;
        var shieldType = shieldingIndex switch
        {
            1 => ShieldType.Foil,
            2 => ShieldType.Braid,
            3 => ShieldType.FoilAndBraid,
            _ => ShieldType.None
        };

        // Create cores
        var cores = new List<CableCore>();
        for (int i = 0; i < conductorCount; i++)
        {
            cores.Add(new CableCore
            {
                CoreId = (i + 1).ToString(),
                ConductorDiameter = finalConductorDiameter,
                InsulationThickness = insulationThickness,
                InsulationColor = coreColors[i % coreColors.Count],
                Gauge = gauge,
                ConductorMaterial = "Copper"
            });
        }

        // Create cable
        var cable = new Cable
        {
            CableId = Guid.NewGuid().ToString("N")[..8],
            PartNumber = partNumber,
            Manufacturer = manufacturer,
            Name = partNumber,
            Type = conductorCount == 1 ? CableType.SingleCore : CableType.MultiCore,
            JacketColor = cableColor,
            JacketThickness = 0.3,
            HasShield = hasShield,
            ShieldType = shieldType,
            ShieldThickness = hasShield ? 0.15 : 0,
            ShieldCoverage = hasShield ? 85 : 0,
            SpecifiedOuterDiameter = cableOd > 0 ? cableOd : null,
            Cores = cores
        };

        // Add to library
        UserLibraryService.AddCable(cable);
    }

    private void AddHeatShrink()
    {
        var partNumber = this.FindControl<TextBox>("HeatShrinkPartNumberBox")!.Text;
        var manufacturer = this.FindControl<TextBox>("HeatShrinkManufacturerBox")!.Text;
        var unrecoveredDiameter = (double)this.FindControl<NumericUpDown>("HeatShrinkUnrecoveredDiameterUpDown")!.Value!;
        var recoveredDiameter = (double)this.FindControl<NumericUpDown>("HeatShrinkRecoveredDiameterUpDown")!.Value!;

        // Validate
        if (string.IsNullOrWhiteSpace(partNumber))
            throw new InvalidOperationException("Part number is required");
        if (string.IsNullOrWhiteSpace(manufacturer))
            throw new InvalidOperationException("Manufacturer is required");
        if (unrecoveredDiameter <= 0)
            throw new InvalidOperationException("Un-recovered diameter must be greater than 0");
        if (recoveredDiameter <= 0)
            throw new InvalidOperationException("Recovered diameter must be greater than 0");
        if (recoveredDiameter >= unrecoveredDiameter)
            throw new InvalidOperationException("Recovered diameter must be less than un-recovered diameter");

        var heatShrink = new HeatShrink
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            PartNumber = partNumber,
            Manufacturer = manufacturer,
            Name = partNumber,
            Material = "Polyolefin",
            SuppliedInnerDiameter = unrecoveredDiameter,
            RecoveredInnerDiameter = recoveredDiameter,
            RecoveredWallThickness = 0.8,
            ShrinkRatio = $"{(unrecoveredDiameter / recoveredDiameter):F1}:1",
            Color = "Black",
            TemperatureRating = 125
        };

        UserLibraryService.AddHeatShrink(heatShrink);
    }

    private void AddOverBraid()
    {
        var partNumber = this.FindControl<TextBox>("OverBraidPartNumberBox")!.Text;
        var manufacturer = this.FindControl<TextBox>("OverBraidManufacturerBox")!.Text;
        var unrecoveredDiameter = (double)this.FindControl<NumericUpDown>("OverBraidUnrecoveredDiameterUpDown")!.Value!;
        var recoveredDiameter = (double)this.FindControl<NumericUpDown>("OverBraidRecoveredDiameterUpDown")!.Value!;

        // Validate
        if (string.IsNullOrWhiteSpace(partNumber))
            throw new InvalidOperationException("Part number is required");
        if (string.IsNullOrWhiteSpace(manufacturer))
            throw new InvalidOperationException("Manufacturer is required");
        if (unrecoveredDiameter <= 0)
            throw new InvalidOperationException("Un-recovered diameter must be greater than 0");
        if (recoveredDiameter <= 0)
            throw new InvalidOperationException("Recovered diameter must be greater than 0");
        if (recoveredDiameter >= unrecoveredDiameter)
            throw new InvalidOperationException("Recovered diameter must be less than un-recovered diameter");

        var overBraid = new OverBraid
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            PartNumber = partNumber,
            Manufacturer = manufacturer,
            Name = partNumber,
            Type = BraidType.ExpandableSleeving,
            Material = "Tinned Copper",
            CoveragePercent = 85,
            NominalInnerDiameter = (unrecoveredDiameter + recoveredDiameter) / 2,
            MinInnerDiameter = recoveredDiameter,
            MaxInnerDiameter = unrecoveredDiameter,
            WallThickness = 0.5,
            IsShielding = true
        };

        UserLibraryService.AddOverBraid(overBraid);
    }

    private List<string> GenerateRandomColors(int count)
    {
        var standardColors = new[] { "White", "Black", "Red", "Green", "Blue", "Yellow", "Orange", "Brown", "Violet", "Gray", "Pink", "Tan" };
        var colors = new List<string>();

        for (int i = 0; i < count; i++)
        {
            colors.Add(standardColors[i % standardColors.Length]);
        }

        return colors;
    }
}
