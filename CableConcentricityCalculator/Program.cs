using CableConcentricityCalculator.Models;
using CableConcentricityCalculator.Reports;
using CableConcentricityCalculator.Services;
using CableConcentricityCalculator.Visualization;
using Spectre.Console;

namespace CableConcentricityCalculator;

class Program
{
    private static CableAssembly? _currentAssembly;
    private static readonly Dictionary<string, Cable> _cableLibrary = ConfigurationService.CreateSampleCableLibrary();
    private static readonly Dictionary<string, HeatShrink> _heatShrinkLibrary = ConfigurationService.CreateSampleHeatShrinkLibrary();
    private static readonly Dictionary<string, OverBraid> _braidLibrary = ConfigurationService.CreateSampleOverBraidLibrary();

    static void Main(string[] args)
    {
        AnsiConsole.Write(new FigletText("Cable Designer").Color(Color.Blue));
        AnsiConsole.MarkupLine("[grey]Concentric Cable Harness Assembly Calculator[/]");
        AnsiConsole.WriteLine();

        // Handle command line arguments
        if (args.Length > 0)
        {
            HandleCommandLineArgs(args);
            return;
        }

        // Interactive mode
        RunInteractiveMode();
    }

    static void HandleCommandLineArgs(string[] args)
    {
        if (args[0] == "--load" || args[0] == "-l")
        {
            if (args.Length < 2)
            {
                AnsiConsole.MarkupLine("[red]Error: Please specify a file to load[/]");
                return;
            }
            LoadAndProcess(args[1], args.Length > 2 ? args[2] : null);
        }
        else if (args[0] == "--help" || args[0] == "-h")
        {
            ShowHelp();
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]Unknown argument: {args[0]}[/]");
            ShowHelp();
        }
    }

    static void ShowHelp()
    {
        var table = new Table();
        table.AddColumn("Argument");
        table.AddColumn("Description");

        table.AddRow("--load, -l <file> [output]", "Load assembly from JSON and generate PDF");
        table.AddRow("--help, -h", "Show this help");
        table.AddRow("[italic](no args)[/]", "Run interactive mode");

        AnsiConsole.Write(table);
    }

    static void LoadAndProcess(string inputPath, string? outputPath)
    {
        if (!File.Exists(inputPath))
        {
            AnsiConsole.MarkupLine($"[red]File not found: {inputPath}[/]");
            return;
        }

        try
        {
            _currentAssembly = ConfigurationService.LoadAssembly(inputPath);
            AnsiConsole.MarkupLine($"[green]Loaded assembly: {_currentAssembly.PartNumber}[/]");

            DisplayAssemblySummary();

            outputPath ??= Path.ChangeExtension(inputPath, ".pdf");

            AnsiConsole.Status()
                .Start("Generating PDF report...", ctx =>
                {
                    PdfReportGenerator.GenerateReport(_currentAssembly, outputPath);
                });

            AnsiConsole.MarkupLine($"[green]✓[/] PDF report generated: [link]{outputPath}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
        }
    }

    static void RunInteractiveMode()
    {
        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to do?")
                    .PageSize(12)
                    .AddChoices(new[]
                    {
                        "Create new cable assembly",
                        "Load existing assembly",
                        "View/Edit current assembly",
                        "Add cables to current assembly",
                        "Add layer to current assembly",
                        "Add heat shrink",
                        "Add over-braid",
                        "Configure outer jacket",
                        "Optimize fillers",
                        "Validate assembly",
                        "Generate PDF report",
                        "Save assembly",
                        "View cable library",
                        "Exit"
                    }));

            AnsiConsole.WriteLine();

            switch (choice)
            {
                case "Create new cable assembly":
                    CreateNewAssembly();
                    break;
                case "Load existing assembly":
                    LoadAssembly();
                    break;
                case "View/Edit current assembly":
                    ViewCurrentAssembly();
                    break;
                case "Add cables to current assembly":
                    AddCablesToAssembly();
                    break;
                case "Add layer to current assembly":
                    AddLayerToAssembly();
                    break;
                case "Add heat shrink":
                    AddHeatShrink();
                    break;
                case "Add over-braid":
                    AddOverBraid();
                    break;
                case "Configure outer jacket":
                    ConfigureOuterJacket();
                    break;
                case "Optimize fillers":
                    OptimizeFillers();
                    break;
                case "Validate assembly":
                    ValidateAssembly();
                    break;
                case "Generate PDF report":
                    GeneratePdfReport();
                    break;
                case "Save assembly":
                    SaveAssembly();
                    break;
                case "View cable library":
                    ViewCableLibrary();
                    break;
                case "Exit":
                    AnsiConsole.MarkupLine("[grey]Goodbye![/]");
                    return;
            }

            AnsiConsole.WriteLine();
        }
    }

    static void CreateNewAssembly()
    {
        var partNumber = AnsiConsole.Ask<string>("Assembly [blue]Part Number[/]:");
        var name = AnsiConsole.Ask<string>("Assembly [blue]Name/Description[/]:");
        var designer = AnsiConsole.Ask("Designed by:", Environment.UserName);
        var project = AnsiConsole.Ask("Project reference:", "");

        _currentAssembly = new CableAssembly
        {
            PartNumber = partNumber,
            Name = name,
            DesignedBy = designer,
            ProjectReference = project,
            DesignDate = DateTime.Now
        };

        // Add center layer
        if (AnsiConsole.Confirm("Add center layer now?"))
        {
            AddCablesToAssembly();
        }

        AnsiConsole.MarkupLine($"[green]Created assembly: {partNumber}[/]");
    }

    static void LoadAssembly()
    {
        var path = AnsiConsole.Ask<string>("Enter path to assembly JSON file:");

        if (!File.Exists(path))
        {
            AnsiConsole.MarkupLine("[red]File not found[/]");
            return;
        }

        try
        {
            _currentAssembly = ConfigurationService.LoadAssembly(path);
            AnsiConsole.MarkupLine($"[green]Loaded: {_currentAssembly.PartNumber}[/]");
            DisplayAssemblySummary();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error loading file: {ex.Message}[/]");
        }
    }

    static void ViewCurrentAssembly()
    {
        if (_currentAssembly == null)
        {
            AnsiConsole.MarkupLine("[yellow]No assembly loaded. Create or load one first.[/]");
            return;
        }

        DisplayAssemblySummary();

        // Show text diagram
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel(CableVisualizer.GenerateTextDiagram(_currentAssembly))
            .Header("Assembly Diagram")
            .BorderColor(Color.Blue));
    }

    static void DisplayAssemblySummary()
    {
        if (_currentAssembly == null) return;

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("Property");
        table.AddColumn("Value");

        table.AddRow("Part Number", _currentAssembly.PartNumber);
        table.AddRow("Revision", _currentAssembly.Revision);
        table.AddRow("Name", _currentAssembly.Name);
        table.AddRow("Overall Diameter", $"{_currentAssembly.OverallDiameter:F2} mm");
        table.AddRow("Core Bundle Diameter", $"{_currentAssembly.CoreBundleDiameter:F2} mm");
        table.AddRow("Total Cables", _currentAssembly.TotalCableCount.ToString());
        table.AddRow("Total Conductors", _currentAssembly.TotalConductorCount.ToString());
        table.AddRow("Total Fillers", _currentAssembly.TotalFillerCount.ToString());
        table.AddRow("Layers", _currentAssembly.Layers.Count.ToString());
        table.AddRow("Over-braids", _currentAssembly.OverBraids.Count.ToString());
        table.AddRow("Heat Shrinks", _currentAssembly.HeatShrinks.Count.ToString());

        AnsiConsole.Write(table);
    }

    static void AddCablesToAssembly()
    {
        if (_currentAssembly == null)
        {
            AnsiConsole.MarkupLine("[yellow]No assembly loaded. Create one first.[/]");
            return;
        }

        // Determine layer number
        int layerNumber = _currentAssembly.Layers.Count;
        var existingLayer = _currentAssembly.Layers.FirstOrDefault(l => l.LayerNumber == layerNumber);

        if (existingLayer == null)
        {
            existingLayer = new CableLayer { LayerNumber = layerNumber };
            _currentAssembly.Layers.Add(existingLayer);
        }

        AnsiConsole.MarkupLine($"[blue]Adding cables to Layer {layerNumber}[/]");

        // Choose cable source
        var source = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Cable source:")
                .AddChoices("From library", "Define custom cable"));

        if (source == "From library")
        {
            AddCablesFromLibrary(existingLayer);
        }
        else
        {
            AddCustomCable(existingLayer);
        }

        // Configure layer properties if not center
        if (layerNumber > 0)
        {
            ConfigureLayerProperties(existingLayer);
        }

        AnsiConsole.MarkupLine($"[green]Layer {layerNumber} now has {existingLayer.Cables.Count} cables[/]");
    }

    static void AddCablesFromLibrary(CableLayer layer)
    {
        var cableKeys = _cableLibrary.Keys.OrderBy(k => k).ToList();

        var selectedCables = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Select cables to add:")
                .PageSize(15)
                .InstructionsText("[grey](Use space to select, enter to confirm)[/]")
                .AddChoices(cableKeys));

        foreach (var key in selectedCables)
        {
            var count = AnsiConsole.Ask($"How many [blue]{key}[/] cables?", 1);

            for (int i = 0; i < count; i++)
            {
                var original = _cableLibrary[key];
                var clone = CloneCable(original);
                layer.Cables.Add(clone);
            }
        }
    }

    static void AddCustomCable(CableLayer layer)
    {
        var partNumber = AnsiConsole.Ask<string>("Part number:");
        var name = AnsiConsole.Ask<string>("Cable name:");

        var cableType = AnsiConsole.Prompt(
            new SelectionPrompt<CableType>()
                .Title("Cable type:")
                .AddChoices(Enum.GetValues<CableType>()));

        var coreCount = cableType == CableType.SingleCore ? 1 :
            AnsiConsole.Ask("Number of cores:", 2);

        var cable = new Cable
        {
            PartNumber = partNumber,
            Name = name,
            Type = cableType
        };

        // Define cores
        for (int i = 0; i < coreCount; i++)
        {
            AnsiConsole.MarkupLine($"[blue]Core {i + 1} of {coreCount}:[/]");

            var gauge = AnsiConsole.Ask("AWG gauge:", "22");
            var conductorDia = AnsiConsole.Ask("Conductor diameter (mm):", 0.644);
            var insulationThick = AnsiConsole.Ask("Insulation thickness (mm):", 0.20);
            var color = AnsiConsole.Ask("Insulation color:", "White");

            cable.Cores.Add(new CableCore
            {
                CoreId = (i + 1).ToString(),
                Gauge = gauge,
                ConductorDiameter = conductorDia,
                InsulationThickness = insulationThick,
                InsulationColor = color
            });
        }

        cable.JacketThickness = AnsiConsole.Ask("Jacket thickness (mm):", 0.25);
        cable.JacketColor = AnsiConsole.Ask("Jacket color:", "Black");

        if (AnsiConsole.Confirm("Does this cable have a shield?", false))
        {
            cable.HasShield = true;
            cable.ShieldType = AnsiConsole.Prompt(
                new SelectionPrompt<ShieldType>()
                    .Title("Shield type:")
                    .AddChoices(Enum.GetValues<ShieldType>().Where(s => s != ShieldType.None)));
            cable.ShieldThickness = AnsiConsole.Ask("Shield thickness (mm):", 0.15);
            cable.ShieldCoverage = AnsiConsole.Ask("Shield coverage (%):", 85.0);
        }

        var quantity = AnsiConsole.Ask("How many of this cable?", 1);

        for (int i = 0; i < quantity; i++)
        {
            layer.Cables.Add(CloneCable(cable));
        }
    }

    static void AddLayerToAssembly()
    {
        if (_currentAssembly == null)
        {
            AnsiConsole.MarkupLine("[yellow]No assembly loaded.[/]");
            return;
        }

        int newLayerNumber = _currentAssembly.Layers.Count;

        var newLayer = new CableLayer
        {
            LayerNumber = newLayerNumber,
            TwistDirection = newLayerNumber % 2 == 0 ? TwistDirection.LeftHand : TwistDirection.RightHand
        };

        _currentAssembly.Layers.Add(newLayer);

        AnsiConsole.MarkupLine($"[green]Added Layer {newLayerNumber}[/]");

        if (AnsiConsole.Confirm("Add cables to this layer now?"))
        {
            AddCablesFromLibrary(newLayer);
            ConfigureLayerProperties(newLayer);
        }
    }

    static void ConfigureLayerProperties(CableLayer layer)
    {
        layer.TwistDirection = AnsiConsole.Prompt(
            new SelectionPrompt<TwistDirection>()
                .Title("Twist direction:")
                .AddChoices(Enum.GetValues<TwistDirection>()));

        if (layer.TwistDirection != TwistDirection.None)
        {
            layer.LayLength = AnsiConsole.Ask("Lay length (mm):", 40.0);
        }

        if (AnsiConsole.Confirm("Add filler wires?", false))
        {
            layer.FillerCount = AnsiConsole.Ask("Number of fillers:", 0);
            if (layer.FillerCount > 0)
            {
                layer.FillerDiameter = AnsiConsole.Ask("Filler diameter (mm):", layer.MaxCableDiameter);
                layer.FillerMaterial = AnsiConsole.Ask("Filler material:", "Nylon");
            }
        }

        if (AnsiConsole.Confirm("Add tape wrap over this layer?", false))
        {
            layer.TapeWrap = new TapeWrap
            {
                Material = AnsiConsole.Ask("Tape material:", "PTFE"),
                Thickness = AnsiConsole.Ask("Tape thickness (mm):", 0.05),
                Width = AnsiConsole.Ask("Tape width (mm):", 12.7),
                OverlapPercent = AnsiConsole.Ask("Overlap (%):", 50.0),
                Color = AnsiConsole.Ask("Tape color:", "White")
            };
        }
    }

    static void AddHeatShrink()
    {
        if (_currentAssembly == null)
        {
            AnsiConsole.MarkupLine("[yellow]No assembly loaded.[/]");
            return;
        }

        var source = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Heat shrink source:")
                .AddChoices("From library", "Define custom"));

        HeatShrink hs;

        if (source == "From library")
        {
            var key = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select heat shrink:")
                    .AddChoices(_heatShrinkLibrary.Keys.OrderBy(k => k)));

            hs = _heatShrinkLibrary[key];
            hs = new HeatShrink
            {
                PartNumber = hs.PartNumber,
                Name = hs.Name,
                Manufacturer = hs.Manufacturer,
                Material = hs.Material,
                SuppliedInnerDiameter = hs.SuppliedInnerDiameter,
                RecoveredInnerDiameter = hs.RecoveredInnerDiameter,
                RecoveredWallThickness = hs.RecoveredWallThickness,
                ShrinkRatio = hs.ShrinkRatio,
                Color = hs.Color,
                TemperatureRating = hs.TemperatureRating
            };
        }
        else
        {
            hs = new HeatShrink
            {
                PartNumber = AnsiConsole.Ask<string>("Part number:"),
                Name = AnsiConsole.Ask<string>("Name:"),
                Material = AnsiConsole.Ask("Material:", "Polyolefin"),
                SuppliedInnerDiameter = AnsiConsole.Ask("Supplied ID (mm):", 12.0),
                RecoveredInnerDiameter = AnsiConsole.Ask("Recovered ID (mm):", 6.0),
                RecoveredWallThickness = AnsiConsole.Ask("Recovered wall (mm):", 0.8),
                ShrinkRatio = AnsiConsole.Ask("Shrink ratio:", "2:1"),
                Color = AnsiConsole.Ask("Color:", "Black")
            };
        }

        _currentAssembly.HeatShrinks.Add(hs);
        AnsiConsole.MarkupLine($"[green]Added heat shrink: {hs.PartNumber}[/]");
    }

    static void AddOverBraid()
    {
        if (_currentAssembly == null)
        {
            AnsiConsole.MarkupLine("[yellow]No assembly loaded.[/]");
            return;
        }

        var source = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Over-braid source:")
                .AddChoices("From library", "Define custom"));

        OverBraid braid;

        if (source == "From library")
        {
            var key = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select over-braid:")
                    .AddChoices(_braidLibrary.Keys.OrderBy(k => k)));

            var original = _braidLibrary[key];
            braid = new OverBraid
            {
                PartNumber = original.PartNumber,
                Name = original.Name,
                Manufacturer = original.Manufacturer,
                Type = original.Type,
                Material = original.Material,
                CoveragePercent = original.CoveragePercent,
                NominalInnerDiameter = original.NominalInnerDiameter,
                MinInnerDiameter = original.MinInnerDiameter,
                MaxInnerDiameter = original.MaxInnerDiameter,
                WallThickness = original.WallThickness,
                IsShielding = original.IsShielding,
                Color = original.Color
            };
        }
        else
        {
            braid = new OverBraid
            {
                PartNumber = AnsiConsole.Ask<string>("Part number:"),
                Name = AnsiConsole.Ask<string>("Name:"),
                Material = AnsiConsole.Ask("Material:", "Tinned Copper"),
                CoveragePercent = AnsiConsole.Ask("Coverage (%):", 85.0),
                WallThickness = AnsiConsole.Ask("Wall thickness (mm):", 0.5),
                IsShielding = AnsiConsole.Confirm("Is this a shielding braid?", true)
            };
        }

        _currentAssembly.OverBraids.Add(braid);
        AnsiConsole.MarkupLine($"[green]Added over-braid: {braid.PartNumber}[/]");
    }

    static void ConfigureOuterJacket()
    {
        if (_currentAssembly == null)
        {
            AnsiConsole.MarkupLine("[yellow]No assembly loaded.[/]");
            return;
        }

        _currentAssembly.OuterJacket = new OuterJacket
        {
            PartNumber = AnsiConsole.Ask<string>("Jacket part number:"),
            Name = AnsiConsole.Ask<string>("Jacket name:"),
            Material = AnsiConsole.Ask("Material:", "PVC"),
            WallThickness = AnsiConsole.Ask("Wall thickness (mm):", 0.5),
            Color = AnsiConsole.Ask("Color:", "Black")
        };

        AnsiConsole.MarkupLine("[green]Outer jacket configured[/]");
    }

    static void OptimizeFillers()
    {
        if (_currentAssembly == null)
        {
            AnsiConsole.MarkupLine("[yellow]No assembly loaded.[/]");
            return;
        }

        AnsiConsole.Status()
            .Start("Optimizing fillers...", ctx =>
            {
                ConcentricityCalculator.OptimizeFillers(_currentAssembly);
            });

        foreach (var layer in _currentAssembly.Layers.Where(l => l.FillerCount > 0))
        {
            AnsiConsole.MarkupLine($"Layer {layer.LayerNumber}: {layer.FillerCount} fillers @ {layer.FillerDiameter:F2}mm");
        }

        AnsiConsole.MarkupLine("[green]Filler optimization complete[/]");
    }

    static void ValidateAssembly()
    {
        if (_currentAssembly == null)
        {
            AnsiConsole.MarkupLine("[yellow]No assembly loaded.[/]");
            return;
        }

        var issues = ConcentricityCalculator.ValidateAssembly(_currentAssembly);

        if (issues.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]✓ Assembly is valid - no issues found[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]Found {issues.Count} issue(s):[/]");
            foreach (var issue in issues)
            {
                AnsiConsole.MarkupLine($"  [yellow]⚠[/] {issue}");
            }
        }
    }

    static void GeneratePdfReport()
    {
        if (_currentAssembly == null)
        {
            AnsiConsole.MarkupLine("[yellow]No assembly loaded.[/]");
            return;
        }

        var outputDir = Path.Combine(Environment.CurrentDirectory, "output");
        Directory.CreateDirectory(outputDir);

        var defaultPath = Path.Combine(outputDir, $"{_currentAssembly.PartNumber}_Report.pdf");
        var outputPath = AnsiConsole.Ask("Output path:", defaultPath);

        AnsiConsole.Status()
            .Start("Generating PDF report...", ctx =>
            {
                PdfReportGenerator.GenerateReport(_currentAssembly, outputPath);
            });

        AnsiConsole.MarkupLine($"[green]✓ Report saved: {outputPath}[/]");

        // Also generate cross-section image
        var imagePath = Path.ChangeExtension(outputPath, ".png");
        var imageBytes = CableVisualizer.GenerateCrossSectionImage(_currentAssembly);
        File.WriteAllBytes(imagePath, imageBytes);
        AnsiConsole.MarkupLine($"[green]✓ Cross-section image: {imagePath}[/]");
    }

    static void SaveAssembly()
    {
        if (_currentAssembly == null)
        {
            AnsiConsole.MarkupLine("[yellow]No assembly loaded.[/]");
            return;
        }

        var outputDir = Path.Combine(Environment.CurrentDirectory, "output");
        Directory.CreateDirectory(outputDir);

        var defaultPath = Path.Combine(outputDir, $"{_currentAssembly.PartNumber}.json");
        var outputPath = AnsiConsole.Ask("Output path:", defaultPath);

        ConfigurationService.SaveAssembly(_currentAssembly, outputPath);
        AnsiConsole.MarkupLine($"[green]✓ Assembly saved: {outputPath}[/]");
    }

    static void ViewCableLibrary()
    {
        var table = new Table();
        table.AddColumn("Key");
        table.AddColumn("Part Number");
        table.AddColumn("Type");
        table.AddColumn("Cores");
        table.AddColumn("OD (mm)");
        table.AddColumn("Color");

        foreach (var (key, cable) in _cableLibrary.OrderBy(kvp => kvp.Key).Take(20))
        {
            table.AddRow(
                key,
                cable.PartNumber,
                cable.Type.ToString(),
                cable.Cores.Count.ToString(),
                cable.OuterDiameter.ToString("F2"),
                cable.JacketColor);
        }

        AnsiConsole.Write(table);

        if (_cableLibrary.Count > 20)
        {
            AnsiConsole.MarkupLine($"[grey]... and {_cableLibrary.Count - 20} more cables[/]");
        }
    }

    static Cable CloneCable(Cable source)
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
                ConductorMaterial = c.ConductorMaterial
            }).ToList()
        };
    }
}
