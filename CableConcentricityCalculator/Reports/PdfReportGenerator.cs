using CableConcentricityCalculator.Models;
using CableConcentricityCalculator.Services;
using CableConcentricityCalculator.Visualization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CableConcentricityCalculator.Reports;

/// <summary>
/// Generates PDF reports for cable assemblies
/// </summary>
public class PdfReportGenerator
{
    /// <summary>
    /// Generate a complete PDF report for a cable assembly
    /// </summary>
    public static void GenerateReport(CableAssembly assembly, string outputPath)
    {
        // Set QuestPDF license (Community license for open source)
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, assembly));
                page.Content().Element(c => ComposeContent(c, assembly));
                page.Footer().Element(ComposeFooter);
            });
        });

        document.GeneratePdf(outputPath);
    }

    /// <summary>
    /// Generate report and return as byte array
    /// </summary>
    public static byte[] GenerateReportBytes(CableAssembly assembly)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, assembly));
                page.Content().Element(c => ComposeContent(c, assembly));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, CableAssembly assembly)
    {
        container.Column(headerCol =>
        {
            headerCol.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("CABLE ASSEMBLY SPECIFICATION")
                        .FontSize(16).Bold().FontColor(Colors.Blue.Darken2);

                    col.Item().Text($"{assembly.PartNumber} Rev {assembly.Revision}")
                        .FontSize(14).SemiBold();

                    col.Item().Text(assembly.Name)
                        .FontSize(11).Italic();
                });

                row.ConstantItem(120).Column(col =>
                {
                    col.Item().AlignRight().Text($"Date: {assembly.DesignDate:yyyy-MM-dd}");
                    col.Item().AlignRight().Text($"By: {assembly.DesignedBy}");
                    if (!string.IsNullOrEmpty(assembly.ProjectReference))
                    {
                        col.Item().AlignRight().Text($"Project: {assembly.ProjectReference}");
                    }
                });
            });

            headerCol.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Blue.Darken2);
        });
    }

    private static void ComposeContent(IContainer container, CableAssembly assembly)
    {
        container.PaddingTop(10).Column(col =>
        {
            // Cross-section diagram
            col.Item().Element(c => ComposeCrossSection(c, assembly));

            col.Item().PaddingTop(15);

            // Two-column layout for specifications
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(c => ComposeSpecifications(c, assembly));
                row.ConstantItem(20);
                row.RelativeItem().Element(c => ComposeLayerDetails(c, assembly));
            });

            col.Item().PaddingTop(15);

            // Bill of Materials
            col.Item().Element(c => ComposeBillOfMaterials(c, assembly));

            col.Item().PaddingTop(15);

            // Cable details table
            col.Item().Element(c => ComposeCableDetails(c, assembly));

            // Notes section
            if (!string.IsNullOrEmpty(assembly.Notes))
            {
                col.Item().PaddingTop(15).Element(c => ComposeNotes(c, assembly));
            }

            // Validation warnings
            var issues = ConcentricityCalculator.ValidateAssembly(assembly);
            if (issues.Count > 0)
            {
                col.Item().PaddingTop(15).Element(c => ComposeWarnings(c, issues));
            }
        });
    }

    private static void ComposeCrossSection(IContainer container, CableAssembly assembly)
    {
        container.Column(col =>
        {
            col.Item().Text("CROSS-SECTION VIEW").FontSize(11).Bold();

            col.Item().PaddingTop(5).AlignCenter().Width(280).Height(280).Element(imgContainer =>
            {
                imgContainer.Border(1).BorderColor(Colors.Grey.Lighten1)
                    .Background(Colors.White)
                    .Padding(5)
                    .Image(CableVisualizer.GenerateCrossSectionImage(assembly, 540, 540));
            });
        });
    }

    private static void ComposeSpecifications(IContainer container, CableAssembly assembly)
    {
        container.Column(col =>
        {
            col.Item().Text("SPECIFICATIONS").FontSize(11).Bold();

            col.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2);
                    c.RelativeColumn(2);
                });

                void AddRow(string label, string value)
                {
                    table.Cell().Element(cell =>
                    {
                        cell.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                            .Padding(3).Text(label).FontSize(9);
                    });
                    table.Cell().Element(cell =>
                    {
                        cell.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                            .Padding(3).AlignRight().Text(value).FontSize(9).SemiBold();
                    });
                }

                AddRow("Overall Diameter", $"{assembly.OverallDiameter:F2} mm");
                AddRow("Core Bundle Diameter", $"{assembly.CoreBundleDiameter:F2} mm");
                AddRow("With Braids Diameter", $"{assembly.DiameterWithBraids:F2} mm");
                AddRow("Total Cables", assembly.TotalCableCount.ToString());
                AddRow("Total Conductors", assembly.TotalConductorCount.ToString());
                AddRow("Total Fillers", assembly.TotalFillerCount.ToString());
                AddRow("Conductor Area", $"{assembly.TotalConductorArea:F2} mm²");
                AddRow("Cross-Section Area", $"{assembly.TotalCrossSectionalArea:F2} mm²");
                AddRow("Temperature Rating", $"{assembly.TemperatureRating}°C");
                AddRow("Voltage Rating", $"{assembly.VoltageRating} V");

                if (assembly.Length > 0)
                {
                    AddRow("Length", $"{assembly.Length} mm");
                }
            });

            if (assembly.ApplicableStandards.Count > 0)
            {
                col.Item().PaddingTop(10).Text("APPLICABLE STANDARDS").FontSize(10).Bold();
                col.Item().PaddingTop(3).Text(string.Join(", ", assembly.ApplicableStandards))
                    .FontSize(9);
            }
        });
    }

    private static void ComposeLayerDetails(IContainer container, CableAssembly assembly)
    {
        container.Column(col =>
        {
            col.Item().Text("LAYER STRUCTURE").FontSize(11).Bold();

            col.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(40);
                    c.RelativeColumn();
                    c.ConstantColumn(50);
                    c.ConstantColumn(55);
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(3)
                        .Text("Layer").FontSize(8).Bold());
                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(3)
                        .Text("Contents").FontSize(8).Bold());
                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(3)
                        .Text("Twist").FontSize(8).Bold());
                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(3)
                        .Text("Lay (mm)").FontSize(8).Bold());
                });

                foreach (var layer in assembly.Layers.OrderBy(l => l.LayerNumber))
                {
                    string twist = layer.TwistDirection switch
                    {
                        TwistDirection.RightHand => "RH (S)",
                        TwistDirection.LeftHand => "LH (Z)",
                        _ => "None"
                    };

                    int cableCount = layer.Cables.Count(c => !c.IsFiller);
                    int fillerCount = layer.FillerCount + layer.Cables.Count(c => c.IsFiller);

                    string contents = $"{cableCount} cable" + (cableCount != 1 ? "s" : "");
                    if (fillerCount > 0)
                        contents += $" + {fillerCount} filler" + (fillerCount != 1 ? "s" : "");

                    table.Cell().Element(cell => cell.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(3).Text($"L{layer.LayerNumber}").FontSize(9));
                    table.Cell().Element(cell => cell.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(3).Text(contents).FontSize(9));
                    table.Cell().Element(cell => cell.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(3).Text(twist).FontSize(9));
                    table.Cell().Element(cell => cell.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(3).AlignRight().Text(layer.LayLength > 0 ? layer.LayLength.ToString("F0") : "-").FontSize(9));
                }
            });

            // Tape wraps
            var layersWithTape = assembly.Layers.Where(l => l.TapeWrap != null).ToList();
            if (layersWithTape.Count > 0)
            {
                col.Item().PaddingTop(10).Text("TAPE WRAPS").FontSize(10).Bold();
                foreach (var layer in layersWithTape)
                {
                    col.Item().PaddingTop(2).Text(
                        $"After L{layer.LayerNumber}: {layer.TapeWrap!.Material} {layer.TapeWrap.Width}mm, {layer.TapeWrap.OverlapPercent}% overlap")
                        .FontSize(9);
                }
            }

            // Over-braids
            if (assembly.OverBraids.Count > 0)
            {
                col.Item().PaddingTop(10).Text("OVER-BRAIDS").FontSize(10).Bold();
                foreach (var braid in assembly.OverBraids)
                {
                    col.Item().PaddingTop(2).Text(
                        $"{braid.PartNumber}: {braid.Material}, {braid.CoveragePercent}% coverage")
                        .FontSize(9);
                }
            }

            // Heat shrinks
            if (assembly.HeatShrinks.Count > 0)
            {
                col.Item().PaddingTop(10).Text("HEAT SHRINK").FontSize(10).Bold();
                foreach (var hs in assembly.HeatShrinks)
                {
                    col.Item().PaddingTop(2).Text(
                        $"{hs.PartNumber}: {hs.Material} {hs.ShrinkRatio}, {hs.Color}")
                        .FontSize(9);
                }
            }
        });
    }

    private static void ComposeBillOfMaterials(IContainer container, CableAssembly assembly)
    {
        var bom = assembly.GetBillOfMaterials();

        container.Column(col =>
        {
            col.Item().Text("BILL OF MATERIALS").FontSize(11).Bold();

            col.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(30);
                    c.RelativeColumn(2);
                    c.RelativeColumn(3);
                    c.RelativeColumn(2);
                    c.ConstantColumn(40);
                    c.ConstantColumn(35);
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(3)
                        .Text("#").FontSize(8).Bold());
                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(3)
                        .Text("Part Number").FontSize(8).Bold());
                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(3)
                        .Text("Description").FontSize(8).Bold());
                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(3)
                        .Text("Manufacturer").FontSize(8).Bold());
                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(3)
                        .Text("Qty").FontSize(8).Bold());
                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(3)
                        .Text("Unit").FontSize(8).Bold());
                });

                foreach (var item in bom)
                {
                    table.Cell().Element(cell => cell.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(2).Text(item.ItemNumber.ToString()).FontSize(8));
                    table.Cell().Element(cell => cell.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(2).Text(item.PartNumber).FontSize(8));
                    table.Cell().Element(cell => cell.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(2).Text(item.Description).FontSize(8));
                    table.Cell().Element(cell => cell.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(2).Text(item.Manufacturer).FontSize(8));
                    table.Cell().Element(cell => cell.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(2).AlignRight().Text(item.Quantity.ToString()).FontSize(8));
                    table.Cell().Element(cell => cell.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(2).Text(item.Unit).FontSize(8));
                }
            });
        });
    }

    private static void ComposeCableDetails(IContainer container, CableAssembly assembly)
    {
        var allCables = assembly.Layers
            .SelectMany(l => l.Cables)
            .Where(c => !c.IsFiller)
            .GroupBy(c => c.PartNumber)
            .Select(g => g.First())
            .ToList();

        if (allCables.Count == 0) return;

        container.Column(col =>
        {
            col.Item().Text("CABLE SPECIFICATIONS").FontSize(11).Bold();

            col.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2);
                    c.RelativeColumn(1);
                    c.ConstantColumn(40);
                    c.ConstantColumn(45);
                    c.ConstantColumn(40);
                    c.ConstantColumn(50);
                    c.ConstantColumn(50);
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(3)
                        .Text("Part Number").FontSize(8).Bold());
                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(3)
                        .Text("Type").FontSize(8).Bold());
                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(3)
                        .Text("Cores").FontSize(8).Bold());
                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(3)
                        .Text("OD (mm)").FontSize(8).Bold());
                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(3)
                        .Text("Shield").FontSize(8).Bold());
                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(3)
                        .Text("Jacket").FontSize(8).Bold());
                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(3)
                        .Text("Gauge").FontSize(8).Bold());
                });

                foreach (var cable in allCables)
                {
                    table.Cell().Element(cell => cell.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(2).Text(cable.PartNumber).FontSize(8));
                    table.Cell().Element(cell => cell.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(2).Text(cable.Type.ToString()).FontSize(8));
                    table.Cell().Element(cell => cell.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(2).AlignCenter().Text(cable.Cores.Count.ToString()).FontSize(8));
                    table.Cell().Element(cell => cell.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(2).AlignRight().Text(cable.OuterDiameter.ToString("F2")).FontSize(8));
                    table.Cell().Element(cell => cell.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(2).Text(cable.HasShield ? cable.ShieldType.ToString() : "-").FontSize(8));
                    table.Cell().Element(cell => cell.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(2).Text(cable.JacketColor).FontSize(8));
                    table.Cell().Element(cell => cell.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(2).Text(cable.Cores.FirstOrDefault()?.Gauge ?? "-").FontSize(8));
                }
            });
        });
    }

    private static void ComposeNotes(IContainer container, CableAssembly assembly)
    {
        container.Column(col =>
        {
            col.Item().Text("NOTES").FontSize(11).Bold();
            col.Item().PaddingTop(5).Element(notesBox =>
            {
                notesBox.Border(1).BorderColor(Colors.Grey.Lighten1)
                    .Padding(8).Text(assembly.Notes).FontSize(9);
            });
        });
    }

    private static void ComposeWarnings(IContainer container, List<string> issues)
    {
        container.Column(col =>
        {
            col.Item().Element(warningBox =>
            {
                warningBox.Background(Colors.Yellow.Lighten3).Padding(5).Column(inner =>
                {
                    inner.Item().Text("⚠ DESIGN WARNINGS").FontSize(11).Bold()
                        .FontColor(Colors.Orange.Darken3);

                    foreach (var issue in issues)
                    {
                        inner.Item().PaddingTop(3).Text($"• {issue}").FontSize(9)
                            .FontColor(Colors.Orange.Darken2);
                    }
                });
            });
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            // Verification reminder
            col.Item().PaddingBottom(5).AlignCenter().Text(text =>
            {
                text.Span("⚠ IMPORTANT: ").FontSize(9).Bold().FontColor(Colors.Orange.Darken2);
                text.Span("Verify all measurements and specifications before committing to this design. ")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);
                text.Span("This document is for reference only and should be validated by a qualified engineer.")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);
            });

            col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
            col.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Cable Concentricity Calculator").FontSize(8);
                    text.Span(" | ").FontSize(8).FontColor(Colors.Grey.Medium);
                    text.Span($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8);
                });

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Page ").FontSize(8);
                    text.CurrentPageNumber().FontSize(8);
                    text.Span(" of ").FontSize(8);
                    text.TotalPages().FontSize(8);
                });
            });
        });
    }
}
