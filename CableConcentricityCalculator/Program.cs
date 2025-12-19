using CableConcentricityCalculator.Models;
using CableConcentricityCalculator.Reports;
using CableConcentricityCalculator.Services;
using Spectre.Console;

namespace CableConcentricityCalculator;

class Program
{
    static void Main(string[] args)
    {
        // Handle command line arguments
        if (args.Length > 0)
        {
            HandleCommandLineArgs(args);
            return;
        }

        // No args - show help and guidance
        ShowWelcomeAndHelp();
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
            LoadAndGeneratePdf(args[1], args.Length > 2 ? args[2] : null);
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

    static void ShowWelcomeAndHelp()
    {
        AnsiConsole.Write(new FigletText("Cable Designer").Color(Color.Blue));
        AnsiConsole.MarkupLine("[grey]Concentric Cable Harness Assembly Calculator[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[yellow]For interactive cable design, please use the GUI application:[/]");
        AnsiConsole.MarkupLine("[blue]  dotnet run --project CableConcentricityCalculator.Gui[/]");
        AnsiConsole.WriteLine();

        ShowHelp();
    }

    static void ShowHelp()
    {
        var table = new Table();
        table.AddColumn("Argument");
        table.AddColumn("Description");

        table.AddRow("--load, -l [[file]] [[output]]", "Load assembly from JSON and generate PDF");
        table.AddRow("--help, -h", "Show this help");
        table.AddRow("(no args)", "Show this help (use GUI for interactive mode)");

        AnsiConsole.Write(table);
    }

    static void LoadAndGeneratePdf(string inputPath, string? outputPath)
    {
        if (!File.Exists(inputPath))
        {
            AnsiConsole.MarkupLine($"[red]File not found: {inputPath}[/]");
            return;
        }

        try
        {
            var assembly = ConfigurationService.LoadAssembly(inputPath);
            AnsiConsole.MarkupLine($"[green]Loaded assembly: {assembly.PartNumber}[/]");

            DisplayAssemblySummary(assembly);

            outputPath ??= Path.ChangeExtension(inputPath, ".pdf");

            AnsiConsole.Status()
                .Start("Generating PDF report...", ctx =>
                {
                    PdfReportGenerator.GenerateReport(assembly, outputPath);
                });

            AnsiConsole.MarkupLine($"[green]✓[/] PDF report generated: [link]{outputPath}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
        }
    }

    static void DisplayAssemblySummary(CableAssembly assembly)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Property")
            .AddColumn("Value");

        table.AddRow("Part Number", assembly.PartNumber);
        table.AddRow("Name", assembly.Name);
        table.AddRow("Layers", assembly.Layers.Count.ToString());
        table.AddRow("Total Conductors", assembly.TotalConductorCount.ToString());
        table.AddRow("Overall Diameter", $"{assembly.OverallDiameter:F2} mm");

        if (assembly.HeatShrinks.Count > 0)
            table.AddRow("Heat Shrinks", assembly.HeatShrinks.Count.ToString());

        if (assembly.OverBraids.Count > 0)
            table.AddRow("Over-Braids", assembly.OverBraids.Count.ToString());

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }
}
