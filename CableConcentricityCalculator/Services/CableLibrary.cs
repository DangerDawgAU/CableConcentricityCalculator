using CableConcentricityCalculator.Models;

namespace CableConcentricityCalculator.Services;

/// <summary>
/// Comprehensive cable library containing industry-standard cables
/// </summary>
public static class CableLibrary
{
    /// <summary>
    /// Standard AWG wire gauges with conductor diameters
    /// </summary>
    private static readonly Dictionary<string, (double ConductorDia, double InsulationThick)> AwgSizes = new()
    {
        { "30", (0.254, 0.10) },
        { "28", (0.320, 0.10) },
        { "26", (0.405, 0.15) },
        { "24", (0.511, 0.15) },
        { "22", (0.644, 0.20) },
        { "20", (0.812, 0.20) },
        { "18", (1.024, 0.25) },
        { "16", (1.291, 0.30) },
        { "14", (1.628, 0.35) },
        { "12", (2.053, 0.40) },
        { "10", (2.588, 0.45) },
        { "8", (3.264, 0.50) }
    };

    /// <summary>
    /// Standard wire colors
    /// </summary>
    private static readonly string[] StandardColors =
    {
        "White", "Black", "Red", "Green", "Blue", "Yellow", "Orange",
        "Brown", "Violet", "Gray", "Pink", "Tan"
    };

    /// <summary>
    /// Create complete MIL-W-22759 cable library
    /// </summary>
    public static Dictionary<string, Cable> CreateMilW22759Library()
    {
        var library = new Dictionary<string, Cable>();

        // MIL-W-22759/16 - PTFE insulated, silver plated copper, 200°C
        AddMilW22759Series(library, "16", "PTFE", "Silver Plated Copper", 200);

        // MIL-W-22759/32 - Cross-linked ETFE, tin plated copper, 150°C
        AddMilW22759Series(library, "32", "ETFE", "Tin Plated Copper", 150);

        // MIL-W-22759/33 - Cross-linked ETFE, silver plated copper, 150°C
        AddMilW22759Series(library, "33", "ETFE", "Silver Plated Copper", 150);

        // MIL-W-22759/34 - Cross-linked ETFE, nickel plated copper, 200°C
        AddMilW22759Series(library, "34", "ETFE", "Nickel Plated Copper", 200);

        // MIL-W-22759/41 - PTFE tape wrapped, silver plated copper, 200°C
        AddMilW22759Series(library, "41", "PTFE Tape", "Silver Plated Copper", 200);

        // MIL-W-22759/43 - PTFE tape wrapped, nickel plated copper, 260°C
        AddMilW22759Series(library, "43", "PTFE Tape", "Nickel Plated Copper", 260);

        // MIL-W-22759/44 - PTFE, nickel plated copper, high temp, 260°C
        AddMilW22759Series(library, "44", "PTFE", "Nickel Plated Copper", 260);

        // MIL-W-22759/86 - Dual wall PTFE/Polyimide, 200°C
        AddMilW22759Series(library, "86", "PTFE/Polyimide", "Silver Plated Copper", 200);

        // MIL-W-22759/87 - Dual wall, extra lightweight
        AddMilW22759Series(library, "87", "PTFE/Polyimide", "Silver Plated Copper", 200, true);

        // MIL-W-22759/90 - PTFE tape, high strength conductor
        AddMilW22759Series(library, "90", "PTFE Tape", "Silver Plated HSPC", 200);

        // MIL-W-22759/92 - ETFE, high strength conductor
        AddMilW22759Series(library, "92", "ETFE", "Silver Plated HSPC", 150);

        // Add twisted pairs, tris, and quads
        AddMilW22759TwistedCables(library);

        return library;
    }

    private static void AddMilW22759TwistedCables(Dictionary<string, Cable> library)
    {
        var twistedGauges = new[] { "26", "24", "22", "20" };
        var twistedSlashes = new[] { "16", "32", "34" }; // Common types for twisted cables
        var twistedConfigs = new[]
        {
            (2, "TP", "Twisted Pair"),
            (3, "TT", "Twisted Tri"),
            (4, "TQ", "Twisted Quad")
        };

        foreach (var slash in twistedSlashes)
        {
            var (insulation, conductor, tempRating) = slash switch
            {
                "16" => ("PTFE", "Silver Plated Copper", 200),
                "32" => ("ETFE", "Tin Plated Copper", 150),
                "34" => ("ETFE", "Nickel Plated Copper", 200),
                _ => ("PTFE", "Silver Plated Copper", 200)
            };

            foreach (var gauge in twistedGauges)
            {
                if (!AwgSizes.TryGetValue(gauge, out var size)) continue;

                foreach (var (coreCount, suffix, description) in twistedConfigs)
                {
                    var key = $"M22759/{slash}-{gauge}-{suffix}";
                    var coreColors = GetTwistedCoreColors(coreCount);

                    library[key] = new Cable
                    {
                        PartNumber = $"M22759/{slash}-{gauge}-{suffix}",
                        Name = $"{gauge} AWG {description} {insulation}",
                        Manufacturer = "MIL-SPEC",
                        Type = coreCount == 2 ? CableType.TwistedPair : CableType.MultiCore,
                        JacketColor = "Clear",
                        JacketThickness = 0.15,
                        HasShield = false,
                        Cores = CreateTwistedCores(coreCount, size.ConductorDia, size.InsulationThick, gauge, conductor, coreColors)
                    };

                    // Also add shielded versions
                    var shieldedKey = $"M22759/{slash}-{gauge}-{suffix}S";
                    library[shieldedKey] = new Cable
                    {
                        PartNumber = $"M22759/{slash}-{gauge}-{suffix}S",
                        Name = $"{gauge} AWG Shielded {description} {insulation}",
                        Manufacturer = "MIL-SPEC",
                        Type = coreCount == 2 ? CableType.TwistedPair : CableType.MultiCore,
                        JacketColor = "Clear",
                        JacketThickness = 0.20,
                        HasShield = true,
                        ShieldType = ShieldType.Braid,
                        ShieldThickness = 0.15,
                        ShieldCoverage = 85,
                        Cores = CreateTwistedCores(coreCount, size.ConductorDia, size.InsulationThick, gauge, conductor, coreColors)
                    };
                }
            }
        }
    }

    private static string[] GetTwistedCoreColors(int count)
    {
        return count switch
        {
            2 => new[] { "White", "Blue" },
            3 => new[] { "White", "Blue", "Orange" },
            4 => new[] { "White", "Blue", "Orange", "Green" },
            _ => new[] { "White", "Blue" }
        };
    }

    private static List<CableCore> CreateTwistedCores(int count, double conductorDia, double insulationThick,
        string gauge, string conductor, string[] colors)
    {
        var cores = new List<CableCore>();
        for (int i = 0; i < count; i++)
        {
            cores.Add(new CableCore
            {
                CoreId = (i + 1).ToString(),
                ConductorDiameter = conductorDia,
                InsulationThickness = insulationThick,
                InsulationColor = colors[i % colors.Length],
                Gauge = gauge,
                ConductorMaterial = conductor
            });
        }
        return cores;
    }

    private static void AddMilW22759Series(Dictionary<string, Cable> library, string slash,
        string insulation, string conductor, int tempRating, bool lightweight = false)
    {
        var gauges = lightweight
            ? new[] { "26", "24", "22", "20" }
            : new[] { "26", "24", "22", "20", "18", "16", "14", "12" };

        foreach (var gauge in gauges)
        {
            if (!AwgSizes.TryGetValue(gauge, out var size)) continue;

            var insulationThick = lightweight ? size.InsulationThick * 0.7 : size.InsulationThick;

            foreach (var color in StandardColors)
            {
                var colorCode = GetMilColorCode(color);
                var key = $"M22759/{slash}-{gauge}-{colorCode}";

                library[key] = new Cable
                {
                    PartNumber = $"M22759/{slash}-{gauge}-{colorCode}",
                    Name = $"{gauge} AWG {color} {insulation} Wire",
                    Manufacturer = "MIL-SPEC",
                    Type = CableType.SingleCore,
                    JacketColor = color,
                    JacketThickness = insulationThick,
                    Cores = new List<CableCore>
                    {
                        new()
                        {
                            CoreId = "1",
                            ConductorDiameter = size.ConductorDia,
                            InsulationThickness = insulationThick,
                            InsulationColor = color,
                            Gauge = gauge,
                            ConductorMaterial = conductor
                        }
                    }
                };
            }
        }
    }

    private static string GetMilColorCode(string color)
    {
        return color switch
        {
            "White" => "9",
            "Black" => "0",
            "Red" => "2",
            "Green" => "5",
            "Blue" => "6",
            "Yellow" => "4",
            "Orange" => "3",
            "Brown" => "1",
            "Violet" => "7",
            "Gray" => "8",
            "Pink" => "10",
            "Tan" => "11",
            _ => "0"
        };
    }

    /// <summary>
    /// Create LAPP OLFLEX cable library
    /// </summary>
    public static Dictionary<string, Cable> CreateOlflexLibrary()
    {
        var library = new Dictionary<string, Cable>();

        // OLFLEX CLASSIC 110 - PVC control cable
        AddOlflexClassic110(library);

        // OLFLEX CLASSIC 100 - Basic PVC cable
        AddOlflexClassic100(library);

        // OLFLEX CHAIN 809 - Continuous flex cable
        AddOlflexChain809(library);

        // OLFLEX SERVO 700 series - Servo motor cables
        AddOlflexServo(library);

        // OLFLEX HEAT 180 - High temperature cable
        AddOlflexHeat180(library);

        // OLFLEX EB - Intrinsically safe cable
        AddOlflexEB(library);

        return library;
    }

    /// <summary>
    /// Standard LAPP cross-section to conductor diameter mappings (mm²)
    /// Based on LAPP specifications for copper conductors
    /// </summary>
    private static readonly Dictionary<double, double> LappCrossectionToDiameter = new()
    {
        { 0.5, 0.80 },
        { 0.75, 0.98 },
        { 1.0, 1.13 },
        { 1.5, 1.38 },
        { 2.5, 1.78 },
        { 4.0, 2.26 },
        { 6.0, 2.76 },
        { 10.0, 3.57 },
        { 16.0, 4.52 },
        { 25.0, 5.64 }
    };

    /// <summary>
    /// LAPP OLFLEX cable outer diameter specifications (mm)
    /// Format: (cores, mm², outer_diameter)
    /// </summary>
    private static readonly Dictionary<(int, double), double> OlflexOuterDiameters = new()
    {
        // CLASSIC 110 - 0.5mm²
        { (2, 0.5), 6.5 },
        { (3, 0.5), 7.0 },
        { (4, 0.5), 7.5 },
        { (5, 0.5), 8.0 },
        { (7, 0.5), 8.8 },
        // CLASSIC 110 - 0.75mm²
        { (2, 0.75), 7.0 },
        { (3, 0.75), 7.5 },
        { (4, 0.75), 8.2 },
        { (5, 0.75), 8.8 },
        { (7, 0.75), 9.5 },
        // CLASSIC 110 - 1.0mm²
        { (2, 1.0), 7.5 },
        { (3, 1.0), 8.0 },
        { (4, 1.0), 8.8 },
        { (5, 1.0), 9.5 },
        { (7, 1.0), 10.5 },
        { (12, 1.0), 12.5 },
        { (18, 1.0), 14.5 },
        { (25, 1.0), 16.5 },
        // CLASSIC 110 - 1.5mm²
        { (2, 1.5), 8.0 },
        { (3, 1.5), 8.8 },
        { (4, 1.5), 9.5 },
        { (5, 1.5), 10.2 },
        { (7, 1.5), 11.5 },
        { (12, 1.5), 13.5 },
        { (18, 1.5), 15.5 },
        // CLASSIC 110 - 2.5mm²
        { (2, 2.5), 9.0 },
        { (3, 2.5), 9.8 },
        { (4, 2.5), 10.8 },
        { (5, 2.5), 11.5 },
        { (7, 2.5), 13.0 },
        // CLASSIC 110 - 4.0mm²
        { (3, 4.0), 11.0 },
        { (4, 4.0), 12.2 },
        { (5, 4.0), 13.0 },
    };

    /// <summary>
    /// OLFLEX CLASSIC 100 outer diameter specifications (mm)
    /// </summary>
    private static readonly Dictionary<(int, double), double> OlflexClassic100OuterDiameters = new()
    {
        // CLASSIC 100 - 0.5mm²
        { (2, 0.5), 5.8 },
        { (3, 0.5), 6.3 },
        { (4, 0.5), 6.8 },
        { (5, 0.5), 7.3 },
        // CLASSIC 100 - 0.75mm²
        { (2, 0.75), 6.3 },
        { (3, 0.75), 6.8 },
        { (4, 0.75), 7.3 },
        { (5, 0.75), 7.8 },
        // CLASSIC 100 - 1.0mm²
        { (2, 1.0), 6.8 },
        { (3, 1.0), 7.3 },
        { (4, 1.0), 8.0 },
        { (5, 1.0), 8.5 },
        // CLASSIC 100 - 1.5mm²
        { (2, 1.5), 7.5 },
        { (3, 1.5), 8.2 },
        { (4, 1.5), 8.8 },
        { (5, 1.5), 9.5 },
        // CLASSIC 100 - 2.5mm²
        { (3, 2.5), 9.0 },
        { (4, 2.5), 10.0 },
        { (5, 2.5), 10.8 },
    };

    /// <summary>
    /// OLFLEX SERVO 700 outer diameter specifications (mm)
    /// </summary>
    private static readonly Dictionary<double, double> OlflexServo700OuterDiameters = new()
    {
        // SERVO 700 4-core + control
        { 1.0, 11.0 },
        { 1.5, 12.5 },
        { 2.5, 14.5 },
        { 4.0, 16.5 },
        { 6.0, 19.0 },
        { 10.0, 23.0 }
    };

    /// <summary>
    /// OLFLEX HEAT 180 outer diameter specifications (mm)
    /// Format: (cores, mm²) → outer_diameter
    /// </summary>
    private static readonly Dictionary<(int, double), double> OlflexHeat180OuterDiameters = new()
    {
        // HEAT 180 - 0.5mm² (silicone jacket, slightly larger)
        { (2, 0.5), 7.5 },
        { (3, 0.5), 8.2 },
        { (4, 0.5), 9.0 },
        // HEAT 180 - 0.75mm²
        { (2, 0.75), 8.2 },
        { (3, 0.75), 9.0 },
        { (4, 0.75), 10.0 },
        // HEAT 180 - 1.0mm²
        { (2, 1.0), 9.0 },
        { (3, 1.0), 9.8 },
        { (4, 1.0), 11.0 },
        { (5, 1.0), 12.0 },
        // HEAT 180 - 1.5mm²
        { (2, 1.5), 10.0 },
        { (3, 1.5), 11.0 },
        { (4, 1.5), 12.5 }
    };

    /// <summary>
    /// OLFLEX EB (Intrinsically Safe) outer diameter specifications (mm)
    /// Format: (cores, mm²) → outer_diameter
    /// </summary>
    private static readonly Dictionary<(int, double), double> OlflexEBOuterDiameters = new()
    {
        // EB - 0.75mm²
        { (2, 0.75), 8.0 },
        { (3, 0.75), 8.8 },
        { (4, 0.75), 9.5 },
        // EB - 1.0mm²
        { (2, 1.0), 8.8 },
        { (3, 1.0), 9.5 },
        { (4, 1.0), 10.5 },
        // EB - 1.5mm²
        { (2, 1.5), 9.8 },
        { (3, 1.5), 10.8 },
        { (4, 1.5), 12.0 }
    };

    /// <summary>
    /// UNITRONIC LiYY/LiYCY outer diameter specifications (mm)
    /// Format: (cores, mm²) → outer_diameter
    /// </summary>
    private static readonly Dictionary<(int, double), double> UnitronicLiYYOuterDiameters = new()
    {
        // LiYY/LiYCY - 0.14mm²
        { (2, 0.14), 4.5 }, { (3, 0.14), 5.0 }, { (4, 0.14), 5.5 }, { (5, 0.14), 6.0 },
        { (6, 0.14), 6.5 }, { (7, 0.14), 7.0 }, { (8, 0.14), 7.5 }, { (10, 0.14), 8.5 },
        { (12, 0.14), 9.5 }, { (14, 0.14), 10.5 }, { (16, 0.14), 11.5 }, { (18, 0.14), 12.5 },
        { (20, 0.14), 13.5 }, { (25, 0.14), 15.5 },
        // LiYY/LiYCY - 0.25mm²
        { (2, 0.25), 5.2 }, { (3, 0.25), 5.8 }, { (4, 0.25), 6.5 }, { (5, 0.25), 7.2 },
        { (6, 0.25), 7.8 }, { (7, 0.25), 8.5 }, { (8, 0.25), 9.2 }, { (10, 0.25), 10.5 },
        { (12, 0.25), 11.8 }, { (14, 0.25), 13.0 }, { (16, 0.25), 14.5 }, { (18, 0.25), 16.0 },
        { (20, 0.25), 17.0 }, { (25, 0.25), 20.0 },
        // LiYY/LiYCY - 0.34mm²
        { (2, 0.34), 5.5 }, { (3, 0.34), 6.2 }, { (4, 0.34), 7.0 }, { (5, 0.34), 7.8 },
        { (6, 0.34), 8.5 }, { (7, 0.34), 9.2 }, { (8, 0.34), 10.0 }, { (10, 0.34), 11.5 },
        { (12, 0.34), 13.0 }, { (14, 0.34), 14.5 }, { (16, 0.34), 16.0 }, { (18, 0.34), 17.5 },
        { (20, 0.34), 19.0 }, { (25, 0.34), 22.0 },
        // LiYY/LiYCY - 0.5mm²
        { (2, 0.5), 6.2 }, { (3, 0.5), 7.0 }, { (4, 0.5), 7.8 }, { (5, 0.5), 8.8 },
        { (6, 0.5), 9.5 }, { (7, 0.5), 10.5 }, { (8, 0.5), 11.5 }, { (10, 0.5), 13.0 },
        { (12, 0.5), 14.8 }, { (14, 0.5), 16.5 }, { (16, 0.5), 18.0 }, { (18, 0.5), 20.0 },
        { (20, 0.5), 22.0 }, { (25, 0.5), 26.0 }
    };

    /// <summary>
    /// UNITRONIC FD (Flexible Data) outer diameter specifications (mm)
    /// Format: (cores, mm²) → outer_diameter
    /// </summary>
    private static readonly Dictionary<(int, double), double> UnitronicFDOuterDiameters = new()
    {
        // FD - 0.14mm²
        { (2, 0.14), 5.0 }, { (3, 0.14), 5.5 }, { (4, 0.14), 6.2 }, { (5, 0.14), 6.8 },
        { (7, 0.14), 8.0 }, { (10, 0.14), 9.5 }, { (12, 0.14), 10.8 },
        // FD - 0.25mm²
        { (2, 0.25), 5.8 }, { (3, 0.25), 6.5 }, { (4, 0.25), 7.2 }, { (5, 0.25), 8.0 },
        { (7, 0.25), 9.5 }, { (10, 0.25), 11.0 }, { (12, 0.25), 12.5 },
        // FD - 0.5mm²
        { (2, 0.5), 6.8 }, { (3, 0.5), 7.8 }, { (4, 0.5), 8.8 }, { (5, 0.5), 9.8 },
        { (7, 0.5), 11.5 }, { (10, 0.5), 13.5 }, { (12, 0.5), 15.0 }
    };

    /// <summary>
    /// MIL-C-27500 cable outer diameter specifications (mm) from MIL-DTL-27500
    /// Format: (wireGauge, conductorCount) → outer_diameter
    /// Based on PTFE tape jacket (style 06) dimensions from datasheet
    /// </summary>
    private static readonly Dictionary<(string, int), double> MilC27500OuterDiameters = new()
    {
        // 1-Conductor cables (rarely used in harnesses, but including for completeness)
        { ("8", 1), 6.71 },
        { ("10", 1), 5.13 },
        { ("12", 1), 4.52 },
        { ("14", 1), 4.04 },
        { ("16", 1), 3.71 },
        { ("18", 1), 3.40 },
        { ("20", 1), 3.20 },
        { ("22", 1), 2.97 },
        { ("24", 1), 2.69 },
        
        // 2-Conductor (Twisted Pair) cables
        { ("8", 2), 12.8 },
        { ("10", 2), 9.65 },
        { ("12", 2), 7.92 },
        { ("14", 2), 6.96 },
        { ("16", 2), 6.30 },
        { ("18", 2), 5.79 },
        { ("20", 2), 5.28 },
        { ("22", 2), 4.83 },
        { ("24", 2), 4.27 },
        
        // 3-Conductor (Twisted Trio) cables
        { ("8", 3), 13.6 },
        { ("10", 3), 10.3 },
        { ("12", 3), 8.74 },
        { ("14", 3), 7.40 },
        { ("16", 3), 6.69 },
        { ("18", 3), 6.14 },
        { ("20", 3), 5.60 },
        { ("22", 3), 5.10 },
        { ("24", 3), 4.50 },
        
        // 4-Conductor (Twisted Quad) cables
        { ("8", 4), 15.1 },
        { ("10", 4), 11.3 },
        { ("12", 4), 9.83 },
        { ("14", 4), 8.46 },
        { ("16", 4), 7.36 },
        { ("18", 4), 6.75 },
        { ("20", 4), 6.14 },
        { ("22", 4), 5.59 },
        { ("24", 4), 4.91 }
    };

    private static double? GetMilC27500OuterDiameter(string wireGauge, int conductorCount)
    {
        return MilC27500OuterDiameters.TryGetValue((wireGauge, conductorCount), out var diameter) ? diameter : null;
    }

    /// <summary>
    /// ETHERLINE (Ethernet) cable outer diameter specifications (mm)
    /// Format: part_number → outer_diameter
    /// </summary>
    private static readonly Dictionary<string, double> EtherlineOuterDiameters = new()
    {
        // Cat.5e cables
        { "ETHERLINE-CAT5E-UTP", 5.5 },
        { "ETHERLINE-CAT5E-SFUTP", 6.0 },
        { "ETHERLINE-CAT5E-FD", 6.5 },
        
        // Cat.6 cables (from datasheet: 500 S/FTP = 7.3mm, 500 F/UTP = 7.4mm)
        { "ETHERLINE-CAT6-UTP", 6.5 },
        { "ETHERLINE-CAT6-SFUTP", 7.3 },
        
        // Cat.6A cables
        { "ETHERLINE-CAT6A-SFTP", 7.8 },
        { "ETHERLINE-CAT6A-FD", 7.5 },
        
        // PROFINET cables
        { "ETHERLINE-PN-TYPEA", 6.8 },
        { "ETHERLINE-PN-TYPEB", 7.5 },
        { "ETHERLINE-PN-FC", 6.5 }
    };

    private static double? GetEtherlineOuterDiameter(string partNumber)
    {
        return EtherlineOuterDiameters.TryGetValue(partNumber, out var diameter) ? diameter : null;
    }

    private static double GetLappConductorDiameter(double crossSectionMm2)
    {
        if (LappCrossectionToDiameter.TryGetValue(crossSectionMm2, out var diameter))
            return diameter;
        
        // Fallback to calculated value if exact match not found
        return Math.Sqrt(crossSectionMm2 / Math.PI) * 2;
    }

    private static double GetOlflexOuterDiameter(int cores, double mm2, bool isClassic100 = false)
    {
        var dict = isClassic100 ? OlflexClassic100OuterDiameters : OlflexOuterDiameters;
        if (dict.TryGetValue((cores, mm2), out var diameter))
            return diameter;
        
        // Fallback calculation if exact specification not found
        // This is a conservative estimate
        var conductorDia = GetLappConductorDiameter(mm2);
        var coreOd = conductorDia + (isClassic100 ? 2 * 0.5 : 2 * 0.6);
        var bundleDia = coreOd * Math.Sqrt(cores) * 0.9; // approximate packing
        return bundleDia + (isClassic100 ? 2 * 0.5 : 2 * 0.6); // add jacket
    }

    private static double? GetOlflexServo700OuterDiameter(double mm2)
    {
        return OlflexServo700OuterDiameters.TryGetValue(mm2, out var diameter) ? diameter : null;
    }

    private static double? GetOlflexHeat180OuterDiameter(int cores, double mm2)
    {
        return OlflexHeat180OuterDiameters.TryGetValue((cores, mm2), out var diameter) ? diameter : null;
    }

    private static double? GetOlflexEBOuterDiameter(int cores, double mm2)
    {
        return OlflexEBOuterDiameters.TryGetValue((cores, mm2), out var diameter) ? diameter : null;
    }

    private static double? GetUnitronicLiYYOuterDiameter(int cores, double mm2)
    {
        return UnitronicLiYYOuterDiameters.TryGetValue((cores, mm2), out var diameter) ? diameter : null;
    }

    private static double? GetUnitronicFDOuterDiameter(int cores, double mm2)
    {
        return UnitronicFDOuterDiameters.TryGetValue((cores, mm2), out var diameter) ? diameter : null;
    }

    private static void AddOlflexClassic110(Dictionary<string, Cable> library)
    {
        var configs = new[]
        {
            (2, 0.5), (3, 0.5), (4, 0.5), (5, 0.5), (7, 0.5),
            (2, 0.75), (3, 0.75), (4, 0.75), (5, 0.75), (7, 0.75),
            (2, 1.0), (3, 1.0), (4, 1.0), (5, 1.0), (7, 1.0), (12, 1.0), (18, 1.0), (25, 1.0),
            (2, 1.5), (3, 1.5), (4, 1.5), (5, 1.5), (7, 1.5), (12, 1.5), (18, 1.5),
            (2, 2.5), (3, 2.5), (4, 2.5), (5, 2.5), (7, 2.5),
            (3, 4.0), (4, 4.0), (5, 4.0)
        };

        foreach (var (cores, mm2) in configs)
        {
            var conductorDia = GetLappConductorDiameter(mm2);
            var pn = $"OLFLEX-CLASSIC-110-{cores}G{mm2:F1}";
            var od = GetOlflexOuterDiameter(cores, mm2, isClassic100: false);

            var cable = CreateMultiCoreCable(
                pn,
                $"OLFLEX CLASSIC 110 {cores}G{mm2}mm²",
                "LAPP",
                cores,
                conductorDia,
                0.6,
                "Gray",
                false);
            cable.SpecifiedOuterDiameter = od;
            library[pn] = cable;
        }
    }

    private static void AddOlflexClassic100(Dictionary<string, Cable> library)
    {
        var configs = new[]
        {
            (2, 0.5), (3, 0.5), (4, 0.5), (5, 0.5),
            (2, 0.75), (3, 0.75), (4, 0.75), (5, 0.75),
            (2, 1.0), (3, 1.0), (4, 1.0), (5, 1.0),
            (2, 1.5), (3, 1.5), (4, 1.5), (5, 1.5),
            (3, 2.5), (4, 2.5), (5, 2.5)
        };

        foreach (var (cores, mm2) in configs)
        {
            var conductorDia = GetLappConductorDiameter(mm2);
            var pn = $"OLFLEX-CLASSIC-100-{cores}x{mm2:F1}";
            var od = GetOlflexOuterDiameter(cores, mm2, isClassic100: true);

            var cable = CreateMultiCoreCable(
                pn,
                $"OLFLEX CLASSIC 100 {cores}x{mm2}mm²",
                "LAPP",
                cores,
                conductorDia,
                0.5,
                "Gray",
                false);
            cable.SpecifiedOuterDiameter = od;
            library[pn] = cable;
        }
    }

    private static void AddOlflexChain809(Dictionary<string, Cable> library)
    {
        var configs = new[]
        {
            (3, 0.5), (4, 0.5), (5, 0.5),
            (3, 0.75), (4, 0.75), (5, 0.75),
            (3, 1.0), (4, 1.0), (5, 1.0), (7, 1.0), (12, 1.0),
            (3, 1.5), (4, 1.5), (5, 1.5), (7, 1.5)
        };

        foreach (var (cores, mm2) in configs)
        {
            var conductorDia = GetLappConductorDiameter(mm2);
            var pn = $"OLFLEX-CHAIN-809-{cores}G{mm2:F1}";
            var od = GetOlflexOuterDiameter(cores, mm2, isClassic100: false) + 0.2; // Chain has slightly thicker jacket

            var cable = CreateMultiCoreCable(
                pn,
                $"OLFLEX CHAIN 809 {cores}G{mm2}mm² Continuous Flex",
                "LAPP",
                cores,
                conductorDia,
                0.7,
                "Gray",
                false);
            cable.SpecifiedOuterDiameter = od;
            library[pn] = cable;
        }
    }

    private static void AddOlflexServo(Dictionary<string, Cable> library)
    {
        var sizes = new[] { 1.0, 1.5, 2.5, 4.0, 6.0, 10.0 };

        foreach (var mm2 in sizes)
        {
            var conductorDia = GetLappConductorDiameter(mm2);
            var od = GetOlflexServo700OuterDiameter(mm2) ?? 11.0; // Fallback to minimum

            // 4-core power + control pairs
            var pn = $"OLFLEX-SERVO-700-4G{mm2:F1}";
            var cable = new Cable
            {
                PartNumber = pn,
                Name = $"OLFLEX SERVO 700 4G{mm2}mm² + Control Pairs",
                Manufacturer = "LAPP",
                Type = CableType.MultiCore,
                HasShield = true,
                ShieldType = ShieldType.Braid,
                ShieldThickness = 0.2,
                ShieldCoverage = 85,
                JacketColor = "Orange",
                JacketThickness = 1.2,
                Cores = CreateColoredCores(4, conductorDia, 0.8),
                SpecifiedOuterDiameter = od
            };
            library[pn] = cable;
        }
    }

    private static void AddOlflexHeat180(Dictionary<string, Cable> library)
    {
        var configs = new[]
        {
            (2, 0.5), (3, 0.5), (4, 0.5),
            (2, 0.75), (3, 0.75), (4, 0.75),
            (2, 1.0), (3, 1.0), (4, 1.0), (5, 1.0),
            (2, 1.5), (3, 1.5), (4, 1.5)
        };

        foreach (var (cores, mm2) in configs)
        {
            var conductorDia = GetLappConductorDiameter(mm2);
            var pn = $"OLFLEX-HEAT-180-{cores}x{mm2:F1}";
            var od = GetOlflexHeat180OuterDiameter(cores, mm2);

            var cable = CreateMultiCoreCable(
                pn,
                $"OLFLEX HEAT 180 {cores}x{mm2}mm² Silicone",
                "LAPP",
                cores,
                conductorDia,
                0.8,
                "Brown",
                false);
            if (od.HasValue)
                cable.SpecifiedOuterDiameter = od.Value;
            library[pn] = cable;
        }
    }

    private static void AddOlflexEB(Dictionary<string, Cable> library)
    {
        var configs = new[]
        {
            (2, 0.75), (3, 0.75), (4, 0.75),
            (2, 1.0), (3, 1.0), (4, 1.0),
            (2, 1.5), (3, 1.5), (4, 1.5)
        };

        foreach (var (cores, mm2) in configs)
        {
            var conductorDia = GetLappConductorDiameter(mm2);
            var pn = $"OLFLEX-EB-{cores}x{mm2:F1}";
            var od = GetOlflexEBOuterDiameter(cores, mm2);

            var cable = CreateMultiCoreCable(
                pn,
                $"OLFLEX EB {cores}x{mm2}mm² Intrinsically Safe",
                "LAPP",
                cores,
                conductorDia,
                0.5,
                "Blue",
                true);
            cable.ShieldType = ShieldType.FoilAndBraid;
            cable.ShieldThickness = 0.25;
            if (od.HasValue)
                cable.SpecifiedOuterDiameter = od.Value;
            library[pn] = cable;
        }
    }

    /// <summary>
    /// Create LAPP UNITRONIC cable library
    /// </summary>
    public static Dictionary<string, Cable> CreateUnitronicLibrary()
    {
        var library = new Dictionary<string, Cable>();

        // UNITRONIC LiYY - Basic data cable
        AddUnitronicLiYY(library);

        // UNITRONIC LiYCY - Shielded data cable
        AddUnitronicLiYCY(library);

        // UNITRONIC BUS - Industrial bus cables
        AddUnitronicBus(library);

        // UNITRONIC FD - Flex data cables
        AddUnitronicFD(library);

        return library;
    }

    private static void AddUnitronicLiYY(Dictionary<string, Cable> library)
    {
        var coreConfigs = new[] { 2, 3, 4, 5, 6, 7, 8, 10, 12, 14, 16, 18, 20, 25 };

        foreach (var cores in coreConfigs)
        {
            foreach (var mm2 in new[] { 0.14, 0.25, 0.34, 0.5 })
            {
                var conductorDia = Math.Sqrt(mm2 / Math.PI) * 2;
                var pn = $"UNITRONIC-LIYY-{cores}x{mm2:F2}";
                var od = GetUnitronicLiYYOuterDiameter(cores, mm2);

                var cable = CreateMultiCoreCable(
                    pn,
                    $"UNITRONIC LiYY {cores}x{mm2}mm² Data Cable",
                    "LAPP",
                    cores,
                    conductorDia,
                    0.3,
                    "Gray",
                    false);
                if (od.HasValue)
                    cable.SpecifiedOuterDiameter = od.Value;
                library[pn] = cable;
            }
        }
    }

    private static void AddUnitronicLiYCY(Dictionary<string, Cable> library)
    {
        var coreConfigs = new[] { 2, 3, 4, 5, 6, 7, 8, 10, 12, 14, 16, 18, 20, 25 };

        foreach (var cores in coreConfigs)
        {
            foreach (var mm2 in new[] { 0.14, 0.25, 0.34, 0.5 })
            {
                var conductorDia = Math.Sqrt(mm2 / Math.PI) * 2;
                var pn = $"UNITRONIC-LIYCY-{cores}x{mm2:F2}";
                var od = GetUnitronicLiYYOuterDiameter(cores, mm2);

                var cable = CreateMultiCoreCable(
                    pn,
                    $"UNITRONIC LiYCY {cores}x{mm2}mm² Shielded Data",
                    "LAPP",
                    cores,
                    conductorDia,
                    0.3,
                    "Gray",
                    true);
                cable.ShieldType = ShieldType.Braid;
                cable.ShieldThickness = 0.15;
                cable.ShieldCoverage = 85;
                if (od.HasValue)
                    cable.SpecifiedOuterDiameter = od.Value;
                library[pn] = cable;
            }
        }
    }

    private static void AddUnitronicBus(Dictionary<string, Cable> library)
    {
        // PROFIBUS
        library["UNITRONIC-BUS-PROFIBUS"] = new Cable
        {
            PartNumber = "UNITRONIC-BUS-PROFIBUS",
            Name = "UNITRONIC BUS PB FC 1x2x0.64",
            Manufacturer = "LAPP",
            Type = CableType.TwistedPair,
            HasShield = true,
            ShieldType = ShieldType.FoilAndBraid,
            ShieldThickness = 0.2,
            ShieldCoverage = 90,
            JacketColor = "Violet",
            JacketThickness = 0.8,
            Cores = new List<CableCore>
            {
                new() { CoreId = "A", ConductorDiameter = 0.64, InsulationThickness = 0.5, InsulationColor = "Green", Gauge = "22" },
                new() { CoreId = "B", ConductorDiameter = 0.64, InsulationThickness = 0.5, InsulationColor = "Red", Gauge = "22" }
            }
        };

        // DeviceNet
        library["UNITRONIC-BUS-DEVICENET"] = new Cable
        {
            PartNumber = "UNITRONIC-BUS-DEVICENET",
            Name = "UNITRONIC BUS DN 2x2x0.34",
            Manufacturer = "LAPP",
            Type = CableType.MultiCore,
            HasShield = true,
            ShieldType = ShieldType.FoilAndBraid,
            ShieldThickness = 0.2,
            JacketColor = "Gray",
            JacketThickness = 1.0,
            Cores = new List<CableCore>
            {
                new() { CoreId = "V+", ConductorDiameter = 0.66, InsulationThickness = 0.5, InsulationColor = "Red", Gauge = "22" },
                new() { CoreId = "V-", ConductorDiameter = 0.66, InsulationThickness = 0.5, InsulationColor = "Black", Gauge = "22" },
                new() { CoreId = "CAN_H", ConductorDiameter = 0.42, InsulationThickness = 0.3, InsulationColor = "White", Gauge = "26" },
                new() { CoreId = "CAN_L", ConductorDiameter = 0.42, InsulationThickness = 0.3, InsulationColor = "Blue", Gauge = "26" }
            }
        };

        // CANopen
        library["UNITRONIC-BUS-CANOPEN"] = new Cable
        {
            PartNumber = "UNITRONIC-BUS-CANOPEN",
            Name = "UNITRONIC BUS CAN 2x2x0.34",
            Manufacturer = "LAPP",
            Type = CableType.MultiCore,
            HasShield = true,
            ShieldType = ShieldType.Braid,
            ShieldThickness = 0.15,
            JacketColor = "Green",
            JacketThickness = 0.9,
            Cores = new List<CableCore>
            {
                new() { CoreId = "CAN_H", ConductorDiameter = 0.42, InsulationThickness = 0.3, InsulationColor = "White", Gauge = "26" },
                new() { CoreId = "CAN_L", ConductorDiameter = 0.42, InsulationThickness = 0.3, InsulationColor = "Blue", Gauge = "26" }
            }
        };
    }

    private static void AddUnitronicFD(Dictionary<string, Cable> library)
    {
        var coreConfigs = new[] { 2, 3, 4, 5, 7, 10, 12 };

        foreach (var cores in coreConfigs)
        {
            foreach (var mm2 in new[] { 0.14, 0.25, 0.5 })
            {
                var conductorDia = Math.Sqrt(mm2 / Math.PI) * 2;
                var pn = $"UNITRONIC-FD-{cores}x{mm2:F2}";
                var od = GetUnitronicFDOuterDiameter(cores, mm2);

                var cable = CreateMultiCoreCable(
                    pn,
                    $"UNITRONIC FD {cores}x{mm2}mm² Flexible Data",
                    "LAPP",
                    cores,
                    conductorDia,
                    0.35,
                    "Gray",
                    false);
                if (od.HasValue)
                    cable.SpecifiedOuterDiameter = od.Value;
                library[pn] = cable;
            }
        }
    }

    /// <summary>
    /// Create LAPP ETHERLINE cable library
    /// </summary>
    public static Dictionary<string, Cable> CreateEtherlineLibrary()
    {
        var library = new Dictionary<string, Cable>();

        // ETHERLINE Cat.5e
        AddEtherlineCat5e(library);

        // ETHERLINE Cat.6
        AddEtherlineCat6(library);

        // ETHERLINE Cat.6A
        AddEtherlineCat6A(library);

        // ETHERLINE PN - PROFINET
        AddEtherlinePN(library);

        return library;
    }

    private static void AddEtherlineCat5e(Dictionary<string, Cable> library)
    {
        // Standard Cat.5e
        var cable1 = new Cable
        {
            PartNumber = "ETHERLINE-CAT5E-UTP",
            Name = "ETHERLINE Cat.5e 4x2xAWG26 UTP",
            Manufacturer = "LAPP",
            Type = CableType.TwistedPair,
            HasShield = false,
            JacketColor = "Gray",
            JacketThickness = 0.6,
            Cores = CreateEthernetCores("26")
        };
        if (GetEtherlineOuterDiameter("ETHERLINE-CAT5E-UTP") is var od1 && od1.HasValue)
            cable1.SpecifiedOuterDiameter = od1.Value;
        library["ETHERLINE-CAT5E-UTP"] = cable1;

        // Shielded Cat.5e
        var cable2 = new Cable
        {
            PartNumber = "ETHERLINE-CAT5E-SFUTP",
            Name = "ETHERLINE Cat.5e 4x2xAWG26 SF/UTP",
            Manufacturer = "LAPP",
            Type = CableType.TwistedPair,
            HasShield = true,
            ShieldType = ShieldType.FoilAndBraid,
            ShieldThickness = 0.2,
            ShieldCoverage = 85,
            JacketColor = "Gray",
            JacketThickness = 0.7,
            Cores = CreateEthernetCores("26")
        };
        if (GetEtherlineOuterDiameter("ETHERLINE-CAT5E-SFUTP") is var od2 && od2.HasValue)
            cable2.SpecifiedOuterDiameter = od2.Value;
        library["ETHERLINE-CAT5E-SFUTP"] = cable2;

        // Flexible Cat.5e
        var cable3 = new Cable
        {
            PartNumber = "ETHERLINE-CAT5E-FD",
            Name = "ETHERLINE Cat.5e FD 4x2xAWG26/7",
            Manufacturer = "LAPP",
            Type = CableType.TwistedPair,
            HasShield = true,
            ShieldType = ShieldType.Braid,
            ShieldThickness = 0.15,
            JacketColor = "Orange",
            JacketThickness = 0.7,
            Cores = CreateEthernetCores("26")
        };
        if (GetEtherlineOuterDiameter("ETHERLINE-CAT5E-FD") is var od3 && od3.HasValue)
            cable3.SpecifiedOuterDiameter = od3.Value;
        library["ETHERLINE-CAT5E-FD"] = cable3;
    }

    private static void AddEtherlineCat6(Dictionary<string, Cable> library)
    {
        var cable1 = new Cable
        {
            PartNumber = "ETHERLINE-CAT6-UTP",
            Name = "ETHERLINE Cat.6 4x2xAWG23 UTP",
            Manufacturer = "LAPP",
            Type = CableType.TwistedPair,
            HasShield = false,
            JacketColor = "Gray",
            JacketThickness = 0.6,
            Cores = CreateEthernetCores("23")
        };
        if (GetEtherlineOuterDiameter("ETHERLINE-CAT6-UTP") is var od1 && od1.HasValue)
            cable1.SpecifiedOuterDiameter = od1.Value;
        library["ETHERLINE-CAT6-UTP"] = cable1;

        var cable2 = new Cable
        {
            PartNumber = "ETHERLINE-CAT6-SFUTP",
            Name = "ETHERLINE Cat.6 4x2xAWG23 SF/UTP",
            Manufacturer = "LAPP",
            Type = CableType.TwistedPair,
            HasShield = true,
            ShieldType = ShieldType.FoilAndBraid,
            ShieldThickness = 0.2,
            ShieldCoverage = 90,
            JacketColor = "Gray",
            JacketThickness = 0.8,
            Cores = CreateEthernetCores("23")
        };
        if (GetEtherlineOuterDiameter("ETHERLINE-CAT6-SFUTP") is var od2 && od2.HasValue)
            cable2.SpecifiedOuterDiameter = od2.Value;
        library["ETHERLINE-CAT6-SFUTP"] = cable2;
    }

    private static void AddEtherlineCat6A(Dictionary<string, Cable> library)
    {
        var cable1 = new Cable
        {
            PartNumber = "ETHERLINE-CAT6A-SFTP",
            Name = "ETHERLINE Cat.6A 4x2xAWG23 S/FTP",
            Manufacturer = "LAPP",
            Type = CableType.TwistedPair,
            HasShield = true,
            ShieldType = ShieldType.FoilAndBraid,
            ShieldThickness = 0.25,
            ShieldCoverage = 90,
            JacketColor = "Blue",
            JacketThickness = 0.9,
            Cores = CreateEthernetCores("23")
        };
        if (GetEtherlineOuterDiameter("ETHERLINE-CAT6A-SFTP") is var od1 && od1.HasValue)
            cable1.SpecifiedOuterDiameter = od1.Value;
        library["ETHERLINE-CAT6A-SFTP"] = cable1;

        var cable2 = new Cable
        {
            PartNumber = "ETHERLINE-CAT6A-FD",
            Name = "ETHERLINE Cat.6A FD 4x2xAWG26/7",
            Manufacturer = "LAPP",
            Type = CableType.TwistedPair,
            HasShield = true,
            ShieldType = ShieldType.FoilAndBraid,
            ShieldThickness = 0.2,
            JacketColor = "Yellow",
            JacketThickness = 0.9,
            Cores = CreateEthernetCores("26")
        };
        if (GetEtherlineOuterDiameter("ETHERLINE-CAT6A-FD") is var od2 && od2.HasValue)
            cable2.SpecifiedOuterDiameter = od2.Value;
        library["ETHERLINE-CAT6A-FD"] = cable2;
    }

    private static void AddEtherlinePN(Dictionary<string, Cable> library)
    {
        // PROFINET Type A
        var cableA = new Cable
        {
            PartNumber = "ETHERLINE-PN-TYPEA",
            Name = "ETHERLINE PN Cat.5e Type A",
            Manufacturer = "LAPP",
            Type = CableType.TwistedPair,
            HasShield = true,
            ShieldType = ShieldType.FoilAndBraid,
            ShieldThickness = 0.2,
            JacketColor = "Green",
            JacketThickness = 0.8,
            Cores = new List<CableCore>
            {
                new() { CoreId = "1+", ConductorDiameter = 0.57, InsulationThickness = 0.4, InsulationColor = "Yellow", Gauge = "22" },
                new() { CoreId = "1-", ConductorDiameter = 0.57, InsulationThickness = 0.4, InsulationColor = "Orange", Gauge = "22" },
                new() { CoreId = "2+", ConductorDiameter = 0.57, InsulationThickness = 0.4, InsulationColor = "White", Gauge = "22" },
                new() { CoreId = "2-", ConductorDiameter = 0.57, InsulationThickness = 0.4, InsulationColor = "Blue", Gauge = "22" }
            }
        };
        if (GetEtherlineOuterDiameter("ETHERLINE-PN-TYPEA") is var odA && odA.HasValue)
            cableA.SpecifiedOuterDiameter = odA.Value;
        library["ETHERLINE-PN-TYPEA"] = cableA;

        // PROFINET Type B
        var cableB = new Cable
        {
            PartNumber = "ETHERLINE-PN-TYPEB",
            Name = "ETHERLINE PN Cat.5e Type B",
            Manufacturer = "LAPP",
            Type = CableType.TwistedPair,
            HasShield = true,
            ShieldType = ShieldType.FoilAndBraid,
            ShieldThickness = 0.2,
            JacketColor = "Green",
            JacketThickness = 1.0,
            Cores = new List<CableCore>
            {
                new() { CoreId = "1+", ConductorDiameter = 0.64, InsulationThickness = 0.5, InsulationColor = "Yellow", Gauge = "22" },
                new() { CoreId = "1-", ConductorDiameter = 0.64, InsulationThickness = 0.5, InsulationColor = "Orange", Gauge = "22" },
                new() { CoreId = "2+", ConductorDiameter = 0.64, InsulationThickness = 0.5, InsulationColor = "White", Gauge = "22" },
                new() { CoreId = "2-", ConductorDiameter = 0.64, InsulationThickness = 0.5, InsulationColor = "Blue", Gauge = "22" }
            }
        };
        if (GetEtherlineOuterDiameter("ETHERLINE-PN-TYPEB") is var odB && odB.HasValue)
            cableB.SpecifiedOuterDiameter = odB.Value;
        library["ETHERLINE-PN-TYPEB"] = cableB;

        // PROFINET FC
        var cableFC = new Cable
        {
            PartNumber = "ETHERLINE-PN-FC",
            Name = "ETHERLINE PN FC Cat.5e Flexible",
            Manufacturer = "LAPP",
            Type = CableType.TwistedPair,
            HasShield = true,
            ShieldType = ShieldType.FoilAndBraid,
            ShieldThickness = 0.2,
            JacketColor = "Green",
            JacketThickness = 0.9,
            Cores = new List<CableCore>
            {
                new() { CoreId = "1+", ConductorDiameter = 0.51, InsulationThickness = 0.4, InsulationColor = "Yellow", Gauge = "24" },
                new() { CoreId = "1-", ConductorDiameter = 0.51, InsulationThickness = 0.4, InsulationColor = "Orange", Gauge = "24" },
                new() { CoreId = "2+", ConductorDiameter = 0.51, InsulationThickness = 0.4, InsulationColor = "White", Gauge = "24" },
                new() { CoreId = "2-", ConductorDiameter = 0.51, InsulationThickness = 0.4, InsulationColor = "Blue", Gauge = "24" }
            }
        };
        if (GetEtherlineOuterDiameter("ETHERLINE-PN-FC") is var odFC && odFC.HasValue)
            cableFC.SpecifiedOuterDiameter = odFC.Value;
        library["ETHERLINE-PN-FC"] = cableFC;
    }

    private static List<CableCore> CreateEthernetCores(string gauge)
    {
        var conductorDia = gauge switch
        {
            "23" => 0.573,
            "24" => 0.511,
            "26" => 0.405,
            _ => 0.511
        };

        return new List<CableCore>
        {
            new() { CoreId = "1+", ConductorDiameter = conductorDia, InsulationThickness = 0.25, InsulationColor = "White", Gauge = gauge },
            new() { CoreId = "1-", ConductorDiameter = conductorDia, InsulationThickness = 0.25, InsulationColor = "Orange", Gauge = gauge },
            new() { CoreId = "2+", ConductorDiameter = conductorDia, InsulationThickness = 0.25, InsulationColor = "White", Gauge = gauge },
            new() { CoreId = "2-", ConductorDiameter = conductorDia, InsulationThickness = 0.25, InsulationColor = "Green", Gauge = gauge },
            new() { CoreId = "3+", ConductorDiameter = conductorDia, InsulationThickness = 0.25, InsulationColor = "White", Gauge = gauge },
            new() { CoreId = "3-", ConductorDiameter = conductorDia, InsulationThickness = 0.25, InsulationColor = "Blue", Gauge = gauge },
            new() { CoreId = "4+", ConductorDiameter = conductorDia, InsulationThickness = 0.25, InsulationColor = "White", Gauge = gauge },
            new() { CoreId = "4-", ConductorDiameter = conductorDia, InsulationThickness = 0.25, InsulationColor = "Brown", Gauge = gauge }
        };
    }

    /// <summary>
    /// Create complete DR25 heat shrink library
    /// </summary>
    public static Dictionary<string, HeatShrink> CreateDR25Library()
    {
        var library = new Dictionary<string, HeatShrink>();

        // Full DR-25 range from 1.2mm to 102mm
        var dr25Sizes = new (double Supplied, double Recovered, double Wall)[]
        {
            (1.2, 0.6, 0.25),
            (1.6, 0.8, 0.30),
            (2.4, 1.2, 0.35),
            (3.2, 1.6, 0.40),
            (4.8, 2.4, 0.45),
            (6.4, 3.2, 0.50),
            (7.9, 4.0, 0.55),
            (9.5, 4.8, 0.60),
            (12.7, 6.4, 0.70),
            (15.9, 7.9, 0.75),
            (19.1, 9.5, 0.80),
            (25.4, 12.7, 0.90),
            (31.8, 15.9, 1.00),
            (38.1, 19.1, 1.10),
            (50.8, 25.4, 1.20),
            (63.5, 31.8, 1.30),
            (76.2, 38.1, 1.40),
            (88.9, 44.5, 1.50),
            (101.6, 50.8, 1.60)
        };

        foreach (var (supplied, recovered, wall) in dr25Sizes)
        {
            var pn = $"DR-25-{supplied:F1}";
            library[pn] = new HeatShrink
            {
                PartNumber = pn,
                Name = $"DR-25 {supplied:F1}mm (2:1)",
                Manufacturer = "TE Connectivity/Raychem",
                Material = "Modified Polyolefin",
                SuppliedInnerDiameter = supplied,
                RecoveredInnerDiameter = recovered,
                RecoveredWallThickness = wall,
                ShrinkRatio = "2:1",
                Color = "Black",
                TemperatureRating = 135,
                RecoveryTemperature = 120
            };

            // Add clear variant
            var pnClear = $"DR-25-{supplied:F1}-CLR";
            library[pnClear] = new HeatShrink
            {
                PartNumber = pnClear,
                Name = $"DR-25 {supplied:F1}mm Clear (2:1)",
                Manufacturer = "TE Connectivity/Raychem",
                Material = "Modified Polyolefin",
                SuppliedInnerDiameter = supplied,
                RecoveredInnerDiameter = recovered,
                RecoveredWallThickness = wall,
                ShrinkRatio = "2:1",
                Color = "Clear",
                TemperatureRating = 135,
                RecoveryTemperature = 120
            };
        }

        // DR-25 with adhesive lining (DR-25-HM)
        var adhesiveSizes = new[] { 6.4, 9.5, 12.7, 19.1, 25.4, 38.1, 50.8 };
        foreach (var supplied in adhesiveSizes)
        {
            var recovered = supplied / 2;
            var wall = supplied * 0.06;
            var pn = $"DR-25-HM-{supplied:F1}";
            library[pn] = new HeatShrink
            {
                PartNumber = pn,
                Name = $"DR-25-HM {supplied:F1}mm Adhesive Lined",
                Manufacturer = "TE Connectivity/Raychem",
                Material = "Modified Polyolefin",
                SuppliedInnerDiameter = supplied,
                RecoveredInnerDiameter = recovered,
                RecoveredWallThickness = wall,
                ShrinkRatio = "2:1",
                Color = "Black",
                TemperatureRating = 135,
                RecoveryTemperature = 120,
                HasAdhesiveLining = true,
                AdhesiveThickness = 0.3
            };
        }

        return library;
    }

    /// <summary>
    /// Create MIL-DTL-27500 cable library - Military spec twisted pair, trio, and quad cables
    /// Format: M27500-[COMPONENT_WIRE_TYPE][RC][COMPONENT_WIRE_SIZE][NUMBER_OF_WIRES][SHIELD_MATERIAL][JACKET_MATERIAL]
    /// Example: M27500-8RC2S06 = M22759/8 wire type, RC wire, 2 wires, S shield (silver), 06 jacket code
    /// </summary>
    public static Dictionary<string, Cable> CreateMilC27500Library()
    {
        var library = new Dictionary<string, Cable>();

        // Component wire types (from MIL-W-22759 variants)
        var componentWireTypes = new[]
        {
            ("VA", "M22759/5 (PTFE, Silver)", "Silver Plated Copper", 0.10, 200),
            ("WA", "M22759/6 (PTFE)", "Copper", 0.10, 200),
            ("SA", "M22759/7 (ETFE, Silver)", "Silver Plated Copper", 0.15, 150),
            ("TA", "M22759/8 (ETFE)", "Tin Plated Copper", 0.15, 150),
            ("LE", "M22759/9 (Polyimide)", "Copper", 0.10, 200),
            ("LH", "M22759/10 (Polyimide)", "Copper", 0.15, 200),
            ("RC", "M22759/11 (ETFE)", "Copper", 0.10, 150),
            ("RI", "M22759/12 (ETFE)", "Copper", 0.15, 150)
        };

        // Wire sizes (gauge)
        var wireSizes = new[] { "26", "24", "22", "20" };

        // Number of conductors (2=pair, 3=trio, 4=quad)
        var conductorCounts = new[] { 2, 3, 4 };

        // Shield material codes
        var shieldMaterials = new[]
        {
            ("U", "No Shield", false, "None"),
            ("N", "Nickel-plated copper, round", true, "Nickel"),
            ("S", "Silver-plated copper, round", true, "Silver"),
            ("T", "Tin-plated copper, round", true, "Tin"),
            ("C", "Heavy nickel-plated copper, round", true, "Nickel"),
            ("F", "Stainless steel, round", true, "Stainless"),
            ("P", "Nickel-plated high-strength copper, round", true, "Nickel"),
            ("M", "Silver-plated high-strength copper, round", true, "Silver"),
            ("G", "Silver-plated copper, flat", true, "Silver"),
            ("J", "Tin-plated copper, flat", true, "Tin"),
        };

        // Jacket material codes (from MIL-DTL-27500 table)
        var jacketMaterials = new[]
        {
            ("00", "No jacket", "Clear", 0.0),
            ("15", "ETFE, extruded, clear", "Clear", 0.20),
            ("14", "ETFE, extruded, white", "White", 0.20),
            ("05", "FEP, extruded, clear", "Clear", 0.25),
            ("09", "FEP, extruded, white", "White", 0.25),
            ("02", "Nylon, extruded, clear", "Clear", 0.15),
            ("21", "PFA, extruded, clear", "Clear", 0.20),
            ("20", "PFA, extruded, white", "White", 0.20),
            ("06", "PTFE, tape, white", "White", 0.30),
            ("01", "PVC, extruded, white", "White", 0.25)
        };

        foreach (var (wireTypeCode, wireTypeDesc, conductor, defaultInsulation, tempRating) in componentWireTypes)
        {
            if (!AwgSizes.TryGetValue("22", out var sizeRef)) continue; // Use 22 as reference for insulation

            foreach (var wireSize in wireSizes)
            {
                if (!AwgSizes.TryGetValue(wireSize, out var size)) continue;

                foreach (var coreCount in conductorCounts)
                {
                    foreach (var (shieldCode, shieldDesc, hasShield, shieldMat) in shieldMaterials)
                    {
                        foreach (var (jacketCode, jacketDesc, jacketColor, jacketThickness) in jacketMaterials)
                        {
                            // Build part number: M27500-[WIRE_TYPE][SIZE][CORES][SHIELD][JACKET]
                            var partNumber = $"M27500-{wireTypeCode}{wireSize}{coreCount}{shieldCode}{jacketCode}";
                            var name = $"MIL-DTL-27500 {wireSize} AWG {coreCount}-Conductor {shieldDesc}";

                            var cableType = coreCount switch
                            {
                                2 => CableType.TwistedPair,
                                _ => CableType.MultiCore
                            };

                            var cores = CreateMilC27500Cores(coreCount, wireSize, size.ConductorDia, size.InsulationThick);

                            // Calculate jacket thickness - use provided value or default based on shielding
                            var finalJacketThickness = jacketThickness > 0 ? jacketThickness : (hasShield ? 0.30 : 0.20);

                            var cable = new Cable
                            {
                                PartNumber = partNumber,
                                Name = name,
                                Manufacturer = "MIL-SPEC",
                                Type = cableType,
                                JacketColor = jacketColor,
                                JacketThickness = finalJacketThickness,
                                HasShield = hasShield,
                                ShieldType = hasShield ? ShieldType.Braid : ShieldType.None,
                                ShieldThickness = hasShield ? 0.15 : 0,
                                ShieldCoverage = hasShield ? 90 : 0,
                                Cores = cores
                            };

                            // Set specified outer diameter from MIL-DTL-27500 datasheet
                            var od = GetMilC27500OuterDiameter(wireSize, coreCount);
                            if (od.HasValue)
                                cable.SpecifiedOuterDiameter = od.Value;

                            library[partNumber] = cable;
                        }
                    }
                }
            }
        }

        return library;
    }

    private static List<CableCore> CreateMilC27500Cores(int count, string gauge, double conductorDia, double insulationThick)
    {
        var colors = count switch
        {
            2 => new[] { "White", "Black" },
            3 => new[] { "White", "Black", "Red" },
            4 => new[] { "White", "Black", "Red", "Green" },
            _ => new[] { "White" }
        };

        var cores = new List<CableCore>();
        for (int i = 0; i < count; i++)
        {
            cores.Add(new CableCore
            {
                CoreId = (i + 1).ToString(),
                ConductorDiameter = conductorDia,
                InsulationThickness = insulationThick,
                InsulationColor = colors[i % colors.Length],
                Gauge = gauge,
                ConductorMaterial = "Copper"
            });
        }
        return cores;
    }

    /// <summary>
    /// Get complete merged cable library
    /// First attempts to load from JSON, then falls back to programmatic generation
    /// </summary>
    public static Dictionary<string, Cable> GetCompleteCableLibrary()
    {
        // Load from JSON library
        var library = LibraryLoader.LoadCableLibrary();

        if (library.Count == 0)
        {
            throw new InvalidOperationException(
                "Cable library not found. Please ensure CableLibrary.json exists in the Libraries folder.");
        }

        return library;
    }

    /// <summary>
    /// Get complete merged heat shrink library - loads from JSON
    /// </summary>
    public static Dictionary<string, HeatShrink> GetCompleteHeatShrinkLibrary()
    {
        // Load from JSON library
        var library = LibraryLoader.LoadHeatShrinkLibrary();

        if (library.Count == 0)
        {
            throw new InvalidOperationException(
                "Heat shrink library not found. Please ensure HeatShrinkLibrary.json exists in the Libraries folder.");
        }

        return library;
    }

    // Helper methods
    private static Cable CreateMultiCoreCable(string partNumber, string name, string manufacturer,
        int coreCount, double conductorDiameter, double insulationThickness, string jacketColor, bool shielded)
    {
        return new Cable
        {
            PartNumber = partNumber,
            Name = name,
            Manufacturer = manufacturer,
            Type = CableType.MultiCore,
            HasShield = shielded,
            ShieldType = shielded ? ShieldType.Braid : ShieldType.None,
            ShieldThickness = shielded ? 0.15 : 0,
            JacketColor = jacketColor,
            JacketThickness = Math.Max(0.5, 0.3 + coreCount * 0.02),
            Cores = CreateColoredCores(coreCount, conductorDiameter, insulationThickness)
        };
    }

    private static List<CableCore> CreateColoredCores(int count, double conductorDiameter, double insulationThickness)
    {
        var dinColors = new[] { "Green/Yellow", "Blue", "Brown", "Black", "Gray", "White", "Red", "Orange", "Violet", "Pink" };
        var cores = new List<CableCore>();

        for (int i = 0; i < count; i++)
        {
            cores.Add(new CableCore
            {
                CoreId = (i + 1).ToString(),
                ConductorDiameter = conductorDiameter,
                InsulationThickness = insulationThickness,
                InsulationColor = dinColors[i % dinColors.Length],
                Gauge = GetAwgFromDiameter(conductorDiameter),
                ConductorMaterial = "Copper"
            });
        }

        return cores;
    }

    private static string GetAwgFromDiameter(double diameter)
    {
        return diameter switch
        {
            <= 0.28 => "30",
            <= 0.35 => "28",
            <= 0.45 => "26",
            <= 0.55 => "24",
            <= 0.70 => "22",
            <= 0.90 => "20",
            <= 1.15 => "18",
            <= 1.45 => "16",
            <= 1.80 => "14",
            <= 2.30 => "12",
            <= 2.90 => "10",
            _ => "8"
        };
    }
}
