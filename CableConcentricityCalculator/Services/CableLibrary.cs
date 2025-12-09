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

        return library;
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
            var conductorDia = Math.Sqrt(mm2 / Math.PI) * 2;
            var pn = $"OLFLEX-CLASSIC-110-{cores}G{mm2:F1}";

            library[pn] = CreateMultiCoreCable(
                pn,
                $"OLFLEX CLASSIC 110 {cores}G{mm2}mm²",
                "LAPP",
                cores,
                conductorDia,
                0.6,
                "Gray",
                false);
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
            var conductorDia = Math.Sqrt(mm2 / Math.PI) * 2;
            var pn = $"OLFLEX-CLASSIC-100-{cores}x{mm2:F1}";

            library[pn] = CreateMultiCoreCable(
                pn,
                $"OLFLEX CLASSIC 100 {cores}x{mm2}mm²",
                "LAPP",
                cores,
                conductorDia,
                0.5,
                "Gray",
                false);
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
            var conductorDia = Math.Sqrt(mm2 / Math.PI) * 2;
            var pn = $"OLFLEX-CHAIN-809-{cores}G{mm2:F1}";

            library[pn] = CreateMultiCoreCable(
                pn,
                $"OLFLEX CHAIN 809 {cores}G{mm2}mm² Continuous Flex",
                "LAPP",
                cores,
                conductorDia,
                0.7,
                "Gray",
                false);
        }
    }

    private static void AddOlflexServo(Dictionary<string, Cable> library)
    {
        var sizes = new[] { 1.0, 1.5, 2.5, 4.0, 6.0, 10.0 };

        foreach (var mm2 in sizes)
        {
            var conductorDia = Math.Sqrt(mm2 / Math.PI) * 2;

            // 4-core power + control pairs
            var pn = $"OLFLEX-SERVO-700-4G{mm2:F1}";
            library[pn] = new Cable
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
                Cores = CreateColoredCores(4, conductorDia, 0.8)
            };
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
            var conductorDia = Math.Sqrt(mm2 / Math.PI) * 2;
            var pn = $"OLFLEX-HEAT-180-{cores}x{mm2:F1}";

            library[pn] = CreateMultiCoreCable(
                pn,
                $"OLFLEX HEAT 180 {cores}x{mm2}mm² Silicone",
                "LAPP",
                cores,
                conductorDia,
                0.8,
                "Brown",
                false);
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
            var conductorDia = Math.Sqrt(mm2 / Math.PI) * 2;
            var pn = $"OLFLEX-EB-{cores}x{mm2:F1}";

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

                library[pn] = CreateMultiCoreCable(
                    pn,
                    $"UNITRONIC LiYY {cores}x{mm2}mm² Data Cable",
                    "LAPP",
                    cores,
                    conductorDia,
                    0.3,
                    "Gray",
                    false);
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

                library[pn] = CreateMultiCoreCable(
                    pn,
                    $"UNITRONIC FD {cores}x{mm2}mm² Flexible Data",
                    "LAPP",
                    cores,
                    conductorDia,
                    0.35,
                    "Gray",
                    false);
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
        library["ETHERLINE-CAT5E-UTP"] = new Cable
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

        // Shielded Cat.5e
        library["ETHERLINE-CAT5E-SFUTP"] = new Cable
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

        // Flexible Cat.5e
        library["ETHERLINE-CAT5E-FD"] = new Cable
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
    }

    private static void AddEtherlineCat6(Dictionary<string, Cable> library)
    {
        library["ETHERLINE-CAT6-UTP"] = new Cable
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

        library["ETHERLINE-CAT6-SFUTP"] = new Cable
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
    }

    private static void AddEtherlineCat6A(Dictionary<string, Cable> library)
    {
        library["ETHERLINE-CAT6A-SFTP"] = new Cable
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

        library["ETHERLINE-CAT6A-FD"] = new Cable
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
    }

    private static void AddEtherlinePN(Dictionary<string, Cable> library)
    {
        // PROFINET Type A
        library["ETHERLINE-PN-TYPEA"] = new Cable
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

        // PROFINET Type B
        library["ETHERLINE-PN-TYPEB"] = new Cable
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

        // PROFINET FC
        library["ETHERLINE-PN-FC"] = new Cable
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
    /// Get complete merged cable library
    /// </summary>
    public static Dictionary<string, Cable> GetCompleteCableLibrary()
    {
        var library = new Dictionary<string, Cable>();

        foreach (var kvp in CreateMilW22759Library())
            library[kvp.Key] = kvp.Value;

        foreach (var kvp in CreateOlflexLibrary())
            library[kvp.Key] = kvp.Value;

        foreach (var kvp in CreateUnitronicLibrary())
            library[kvp.Key] = kvp.Value;

        foreach (var kvp in CreateEtherlineLibrary())
            library[kvp.Key] = kvp.Value;

        return library;
    }

    /// <summary>
    /// Get complete merged heat shrink library
    /// </summary>
    public static Dictionary<string, HeatShrink> GetCompleteHeatShrinkLibrary()
    {
        return CreateDR25Library();
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
