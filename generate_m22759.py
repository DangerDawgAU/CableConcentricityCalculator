import json
import uuid

# All M22759 cable specifications from the PDF
# Format: (gauge, strands, strand_gauge, conductor_dia, insulation_min, insulation_max, resistance)

m22759_specs = {
    # M22759/5 - Silver-plated, Mineral-filled PTFE, 200°C, 600V
    "5": {
        "conductor": "Silver Plated Copper",
        "insulation": "Mineral-filled PTFE",
        "temp": 200,
        "voltage": 600,
        "specs": [
            ("8", 133, 29, 4.11, 6.12, 6.48, 0.658),
            ("10", 37, 26, 2.74, 4.37, 4.73, 1.19),
            ("12", 19, 25, 2.18, 3.89, 4.24, 1.81),
            ("14", 19, 27, 1.70, 3.45, 3.81, 2.88),
            ("16", 19, 29, 1.35, 3.05, 3.30, 4.52),
            ("18", 19, 30, 1.19, 2.67, 2.92, 5.79),
            ("20", 19, 32, 0.97, 2.29, 2.54, 9.19),
            ("22", 19, 34, 0.76, 2.03, 2.29, 15.1),
            ("24", 19, 36, 0.61, 1.78, 2.03, 24.3)
        ]
    },
    # M22759/6 - Nickel-plated, Mineral-filled PTFE, 260°C, 600V
    "6": {
        "conductor": "Nickel Plated Copper",
        "insulation": "Mineral-filled PTFE",
        "temp": 260,
        "voltage": 600,
        "specs": [
            ("8", 133, 29, 4.14, 6.12, 6.48, 0.694),
            ("10", 37, 26, 2.77, 4.37, 4.73, 1.24),
            ("12", 19, 25, 2.18, 3.89, 4.24, 1.89),
            ("14", 19, 27, 1.70, 3.45, 3.81, 3.00),
            ("16", 19, 29, 1.35, 3.05, 3.30, 4.76),
            ("18", 19, 30, 1.19, 2.67, 2.92, 6.10),
            ("20", 19, 32, 0.97, 2.29, 2.54, 9.77),
            ("22", 19, 34, 0.76, 2.03, 2.29, 16.0),
            ("24", 19, 36, 0.61, 1.78, 2.03, 25.9)
        ]
    },
    # M22759/7 - Silver-plated, Mineral-filled PTFE (light weight), 200°C, 600V
    "7": {
        "conductor": "Silver Plated Copper",
        "insulation": "Mineral-filled PTFE (light weight)",
        "temp": 200,
        "voltage": 600,
        "specs": [
            ("8", 133, 29, 4.11, 5.46, 5.72, 0.658),
            ("10", 37, 26, 2.74, 3.91, 4.11, 1.19),
            ("12", 19, 25, 2.18, 3.33, 3.48, 1.81),
            ("14", 19, 27, 1.70, 2.84, 3.00, 2.88),
            ("16", 19, 29, 1.35, 2.51, 2.67, 4.52),
            ("18", 19, 30, 1.19, 2.29, 2.39, 5.79),
            ("20", 19, 32, 0.97, 2.03, 2.13, 9.19),
            ("22", 19, 34, 0.76, 1.80, 1.91, 15.1),
            ("24", 19, 36, 0.61, 1.52, 1.63, 24.3)
        ]
    },
    # M22759/8 - Nickel-plated, Mineral-filled PTFE (light weight), 260°C, 600V
    "8": {
        "conductor": "Nickel Plated Copper",
        "insulation": "Mineral-filled PTFE (light weight)",
        "temp": 260,
        "voltage": 600,
        "specs": [
            ("8", 133, 29, 4.14, 5.46, 5.72, 0.694),
            ("10", 37, 26, 2.77, 3.91, 4.11, 1.24),
            ("12", 19, 25, 2.18, 3.33, 3.48, 1.89),
            ("14", 19, 27, 1.70, 2.84, 3.00, 3.00),
            ("16", 19, 29, 1.35, 2.51, 2.67, 4.76),
            ("18", 19, 30, 1.19, 2.29, 2.39, 6.10),
            ("20", 19, 32, 0.97, 2.03, 2.13, 9.77),
            ("22", 19, 34, 0.76, 1.80, 1.91, 16.0),
            ("24", 19, 36, 0.61, 1.52, 1.63, 25.9)
        ]
    },
    # M22759/9 - Silver-plated, Extruded PTFE, 200°C, 1000V
    "9": {
        "conductor": "Silver Plated Copper",
        "insulation": "Extruded PTFE",
        "temp": 200,
        "voltage": 1000,
        "specs": [
            ("8", 133, 29, 4.11, 5.13, 5.38, 0.658),
            ("10", 37, 26, 2.74, 3.48, 3.68, 1.19),
            ("12", 19, 25, 2.18, 2.95, 3.15, 1.81),
            ("14", 19, 27, 1.70, 2.46, 2.62, 2.88),
            ("16", 19, 29, 1.35, 2.11, 2.21, 4.52),
            ("18", 19, 30, 1.19, 1.93, 2.03, 5.79),
            ("20", 19, 32, 0.97, 1.68, 1.78, 9.19),
            ("22", 19, 34, 0.76, 1.47, 1.57, 15.1),
            ("24", 19, 36, 0.61, 1.30, 1.40, 24.3),
            ("26", 19, 38, 0.48, 1.17, 1.27, 38.4),
            ("28", 7, 36, 0.38, 1.04, 1.14, 63.8)
        ]
    },
    # M22759/10 - Nickel-plated, Extruded PTFE, 260°C, 1000V
    "10": {
        "conductor": "Nickel Plated Copper",
        "insulation": "Extruded PTFE",
        "temp": 260,
        "voltage": 1000,
        "specs": [
            ("8", 133, 29, 4.14, 5.13, 5.38, 0.694),
            ("10", 37, 26, 2.77, 3.48, 3.68, 1.24),
            ("12", 19, 25, 2.18, 2.95, 3.15, 1.89),
            ("14", 19, 27, 1.70, 2.46, 2.62, 3.00),
            ("16", 19, 29, 1.35, 2.11, 2.21, 4.76),
            ("18", 19, 30, 1.19, 1.93, 2.03, 6.10),
            ("20", 19, 32, 0.97, 1.68, 1.78, 9.77),
            ("22", 19, 34, 0.76, 1.47, 1.57, 16.0),
            ("24", 19, 36, 0.61, 1.30, 1.40, 25.9),
            ("26", 19, 38, 0.48, 1.17, 1.27, 42.2),
            ("28", 7, 36, 0.38, 1.04, 1.14, 67.9)
        ]
    },
    # M22759/11 - Silver-plated, Extruded PTFE (medium weight), 200°C, 600V
    "11": {
        "conductor": "Silver Plated Copper",
        "insulation": "Extruded PTFE (medium weight)",
        "temp": 200,
        "voltage": 600,
        "specs": [
            ("8", 133, 29, 4.11, 5.03, 5.23, 0.658),
            ("10", 37, 26, 2.74, 3.43, 3.63, 1.19),
            ("12", 19, 25, 2.18, 2.74, 2.90, 1.81),
            ("14", 19, 27, 1.70, 2.24, 2.34, 2.88),
            ("16", 19, 29, 1.35, 1.85, 1.96, 4.52),
            ("18", 19, 30, 1.19, 1.68, 1.78, 5.79),
            ("20", 19, 32, 0.97, 1.42, 1.52, 9.19),
            ("22", 19, 34, 0.76, 1.19, 1.30, 15.1),
            ("24", 19, 36, 0.61, 1.04, 1.14, 24.3),
            ("26", 19, 38, 0.48, 0.91, 1.02, 38.4),
            ("28", 7, 36, 0.38, 0.79, 0.89, 63.8)
        ]
    },
    # M22759/12 - Nickel-plated, Extruded PTFE (medium weight), 260°C, 600V
    "12": {
        "conductor": "Nickel Plated Copper",
        "insulation": "Extruded PTFE (medium weight)",
        "temp": 260,
        "voltage": 600,
        "specs": [
            ("8", 133, 29, 4.14, 5.08, 5.28, 0.694),
            ("10", 37, 26, 2.77, 3.43, 3.63, 1.24),
            ("12", 19, 25, 2.18, 2.74, 2.90, 1.89),
            ("14", 19, 27, 1.70, 2.24, 2.34, 3.00),
            ("16", 19, 29, 1.35, 1.85, 1.96, 4.76),
            ("18", 19, 30, 1.19, 1.68, 1.78, 6.10),
            ("20", 19, 32, 0.97, 1.42, 1.52, 9.77),
            ("22", 19, 34, 0.76, 1.19, 1.30, 16.0),
            ("24", 19, 36, 0.61, 1.04, 1.14, 25.9),
            ("26", 19, 38, 0.48, 0.914, 1.02, 42.2),
            ("28", 7, 36, 0.38, 0.79, 0.89, 67.9)
        ]
    },
    # M22759/16 - Tin-plated, Extruded ETFE, 150°C, 600V
    "16": {
        "conductor": "Tin Plated Copper",
        "insulation": "Extruded ETFE",
        "temp": 150,
        "voltage": 600,
        "specs": [
            ("2/0", 1330, 30, 11.7, 13.7, 14.0, 0.091),
            ("1/0", 1045, 30, 10.5, 12.0, 12.3, 0.126),
            ("1", 817, 30, 9.40, 10.8, 11.1, 0.149),
            ("2", 665, 30, 8.38, 9.75, 9.96, 0.183),
            ("4", 133, 25, 6.60, 7.82, 8.03, 0.280),
            ("6", 133, 27, 5.13, 6.27, 6.43, 0.445),
            ("8", 133, 29, 4.11, 4.98, 5.13, 0.701),
            ("10", 37, 26, 2.79, 3.45, 3.61, 1.26),
            ("12", 37, 28, 2.18, 2.82, 2.97, 2.02),
            ("14", 19, 27, 1.70, 2.31, 2.41, 3.06),
            ("16", 19, 29, 1.35, 1.96, 2.06, 4.81),
            ("18", 19, 30, 1.22, 1.75, 1.85, 6.23),
            ("20", 19, 32, 0.97, 1.47, 1.57, 9.88),
            ("22", 19, 34, 0.76, 1.27, 1.37, 16.2),
            ("24", 19, 36, 0.61, 1.09, 1.19, 26.2)
        ]
    },
    # M22759/17 - Silver-plated high-strength, Extruded ETFE, 150°C, 600V
    "17": {
        "conductor": "Silver Plated High-Strength Copper Alloy",
        "insulation": "Extruded ETFE",
        "temp": 150,
        "voltage": 600,
        "specs": [
            ("20", 19, 32, 0.97, 1.47, 1.57, 10.7),
            ("22", 19, 34, 0.76, 1.27, 1.37, 17.5),
            ("24", 19, 36, 0.61, 1.09, 1.19, 28.4),
            ("26", 19, 38, 0.48, 0.97, 1.07, 44.8)
        ]
    },
    # M22759/18 - Tin-plated, Extruded ETFE (light weight), 150°C, 600V
    "18": {
        "conductor": "Tin Plated Copper",
        "insulation": "Extruded ETFE (light weight)",
        "temp": 150,
        "voltage": 600,
        "specs": [
            ("10", 37, 26, 2.79, 3.33, 3.48, 1.26),
            ("12", 37, 28, 2.18, 2.64, 2.80, 2.02),
            ("14", 19, 27, 1.70, 2.11, 2.21, 3.06),
            ("16", 19, 29, 1.35, 1.73, 1.83, 4.81),
            ("18", 19, 30, 1.19, 1.50, 1.60, 6.23),
            ("20", 19, 32, 0.97, 1.24, 1.35, 9.88),
            ("22", 19, 34, 0.76, 1.04, 1.14, 16.2),
            ("24", 19, 36, 0.61, 0.86, 0.97, 26.2),
            ("26", 19, 38, 0.48, 0.76, 0.86, 41.3)
        ]
    },
    # M22759/19 - Silver-plated high-strength, Extruded ETFE (light weight), 150°C, 600V
    "19": {
        "conductor": "Silver Plated High-Strength Copper Alloy",
        "insulation": "Extruded ETFE (light weight)",
        "temp": 150,
        "voltage": 600,
        "specs": [
            ("20", 19, 32, 0.97, 1.24, 1.35, 10.7),
            ("22", 19, 34, 0.76, 1.04, 1.14, 17.5),
            ("24", 19, 36, 0.61, 0.86, 0.97, 28.4),
            ("26", 19, 38, 0.48, 0.76, 0.86, 44.8)
        ]
    },
    # M22759/20 - Silver-plated high-strength, Extruded PTFE, 200°C, 1000V
    "20": {
        "conductor": "Silver Plated High-Strength Copper Alloy",
        "insulation": "Extruded PTFE",
        "temp": 200,
        "voltage": 1000,
        "specs": [
            ("20", 19, 32, 0.97, 1.68, 1.78, 10.7),
            ("22", 19, 34, 0.76, 1.47, 1.57, 17.5),
            ("24", 19, 36, 0.61, 1.30, 1.40, 28.4),
            ("26", 19, 38, 0.48, 1.17, 1.27, 44.8),
            ("28", 7, 36, 0.38, 1.04, 1.14, 74.4)
        ]
    },
    # M22759/21 - Nickel-plated high-strength, Extruded PTFE, 260°C, 1000V
    "21": {
        "conductor": "Nickel Plated High-Strength Copper Alloy",
        "insulation": "Extruded PTFE",
        "temp": 260,
        "voltage": 1000,
        "specs": [
            ("20", 19, 32, 0.97, 1.68, 1.78, 11.4),
            ("22", 19, 34, 0.76, 1.47, 1.57, 18.6),
            ("24", 19, 36, 0.61, 1.30, 1.40, 30.1),
            ("26", 19, 38, 0.48, 1.17, 1.27, 49.4),
            ("28", 7, 36, 0.38, 1.04, 1.14, 79.0)
        ]
    },
    # M22759/22 - Silver-plated high-strength, Extruded PTFE (light weight), 200°C, 600V
    "22": {
        "conductor": "Silver Plated High-Strength Copper Alloy",
        "insulation": "Extruded PTFE (light weight)",
        "temp": 200,
        "voltage": 600,
        "specs": [
            ("20", 19, 32, 0.97, 1.42, 1.52, 10.7),
            ("22", 19, 34, 0.76, 1.19, 1.30, 17.5),
            ("24", 19, 36, 0.61, 1.04, 1.14, 28.4),
            ("26", 19, 38, 0.48, 0.91, 1.02, 44.8),
            ("28", 7, 36, 0.38, 0.79, 0.89, 74.4)
        ]
    },
    # M22759/23 - Nickel-plated high-strength, Extruded PTFE (light weight), 260°C, 600V
    "23": {
        "conductor": "Nickel Plated High-Strength Copper Alloy",
        "insulation": "Extruded PTFE (light weight)",
        "temp": 260,
        "voltage": 600,
        "specs": [
            ("20", 19, 32, 0.97, 1.42, 1.52, 11.4),
            ("22", 19, 34, 0.76, 1.19, 1.30, 18.6),
            ("24", 19, 36, 0.61, 1.04, 1.14, 30.1),
            ("26", 19, 38, 0.48, 0.91, 1.02, 49.4),
            ("28", 7, 36, 0.38, 0.79, 0.89, 79.0)
        ]
    },
    # M22759/28 - Silver-plated, PTFE with polyimide hardcoat, 200°C, 600V
    "28": {
        "conductor": "Silver Plated Copper",
        "insulation": "PTFE with polyimide hardcoat",
        "temp": 200,
        "voltage": 600,
        "specs": [
            ("14", 19, 27, 1.70, 2.24, 2.39, 2.88),
            ("16", 19, 29, 1.35, 1.85, 2.01, 4.52),
            ("18", 19, 30, 1.19, 1.70, 1.80, 5.79),
            ("20", 19, 32, 0.97, 1.45, 1.55, 9.19),
            ("22", 19, 34, 0.76, 1.22, 1.32, 15.1),
            ("24", 19, 36, 0.61, 1.07, 1.17, 24.3),
            ("26", 19, 38, 0.48, 0.94, 1.04, 38.4),
            ("28", 7, 36, 0.38, 0.81, 0.91, 63.8)
        ]
    },
    # M22759/29 - Nickel-plated, PTFE with polyimide hardcoat, 260°C, 600V
    "29": {
        "conductor": "Nickel Plated Copper",
        "insulation": "PTFE with polyimide hardcoat",
        "temp": 260,
        "voltage": 600,
        "specs": [
            ("14", 19, 27, 1.70, 2.24, 2.39, 3.00),
            ("16", 19, 29, 1.35, 1.85, 2.01, 4.76),
            ("18", 19, 30, 1.19, 1.70, 1.80, 6.10),
            ("20", 19, 32, 0.97, 1.45, 1.55, 9.77),
            ("22", 19, 34, 0.76, 1.22, 1.32, 16.0),
            ("24", 19, 36, 0.61, 1.07, 1.17, 25.9),
            ("26", 19, 38, 0.48, 0.94, 1.04, 42.2),
            ("28", 7, 36, 0.38, 0.81, 0.91, 67.9)
        ]
    },
    # M22759/30 - Silver-plated high-strength, PTFE with polyimide hardcoat, 200°C, 600V
    "30": {
        "conductor": "Silver Plated High-Strength Copper Alloy",
        "insulation": "PTFE with polyimide hardcoat",
        "temp": 200,
        "voltage": 600,
        "specs": [
            ("20", 19, 32, 0.97, 1.45, 1.55, 10.7),
            ("22", 19, 34, 0.76, 1.22, 1.32, 17.5),
            ("24", 19, 36, 0.61, 1.07, 1.17, 28.4),
            ("26", 19, 38, 0.48, 0.94, 1.04, 44.8),
            ("28", 7, 36, 0.38, 0.81, 0.91, 74.4)
        ]
    },
    # M22759/31 - Nickel-plated high-strength, PTFE with polyimide hardcoat, 260°C, 600V
    "31": {
        "conductor": "Nickel Plated High-Strength Copper Alloy",
        "insulation": "PTFE with polyimide hardcoat",
        "temp": 260,
        "voltage": 600,
        "specs": [
            ("20", 19, 32, 0.97, 1.45, 1.55, 11.4),
            ("22", 19, 34, 0.76, 1.22, 1.32, 18.6),
            ("24", 19, 36, 0.61, 1.07, 1.17, 30.1),
            ("26", 19, 38, 0.48, 0.94, 1.04, 49.4),
            ("28", 7, 36, 0.38, 0.81, 0.91, 79.0)
        ]
    },
    # M22759/80 - Tin-plated, PTFE/polyimide/PTFE tape (light weight), 150°C, 600V
    "80": {
        "conductor": "Tin Plated Copper",
        "insulation": "PTFE/polyimide/PTFE tape (light weight)",
        "temp": 150,
        "voltage": 600,
        "specs": [
            ("10", 37, 26, 2.69, 3.02, 3.12, 1.26),
            ("12", 37, 28, 2.12, 2.44, 2.54, 2.02),
            ("14", 19, 27, 1.64, 1.93, 2.03, 3.06),
            ("16", 19, 29, 1.31, 1.60, 1.70, 4.81),
            ("18", 19, 30, 1.16, 1.42, 1.52, 6.23),
            ("20", 19, 32, 0.93, 1.22, 1.30, 9.88),
            ("22", 19, 34, 0.72, 1.02, 1.09, 16.2),
            ("24", 19, 36, 0.57, 0.86, 0.97, 26.2),
            ("26", 19, 38, 0.44, 0.76, 0.86, 41.3)
        ]
    },
    # M22759/81 - Silver-plated high-strength, PTFE/polyimide/PTFE tape (light weight), 200°C, 600V
    "81": {
        "conductor": "Silver Plated High-Strength Copper Alloy",
        "insulation": "PTFE/polyimide/PTFE tape (light weight)",
        "temp": 200,
        "voltage": 600,
        "specs": [
            ("20", 19, 32, 0.93, 1.22, 1.30, 10.7),
            ("22", 19, 34, 0.72, 1.02, 1.09, 17.5),
            ("24", 19, 36, 0.57, 0.86, 0.97, 28.4),
            ("26", 19, 38, 0.44, 0.76, 0.86, 56.4)
        ]
    },
    # M22759/82 - Nickel-plated high-strength, PTFE/polyimide/PTFE tape (light weight), 260°C, 600V
    "82": {
        "conductor": "Nickel Plated High-Strength Copper Alloy",
        "insulation": "PTFE/polyimide/PTFE tape (light weight)",
        "temp": 260,
        "voltage": 600,
        "specs": [
            ("20", 19, 32, 0.93, 1.22, 1.30, 11.4),
            ("22", 19, 34, 0.72, 1.02, 1.09, 18.6),
            ("24", 19, 36, 0.57, 0.86, 0.97, 30.1),
            ("26", 19, 38, 0.44, 0.72, 0.86, 58.4)
        ]
    },
    # M22759/83 - Silver-plated, PTFE/polyimide/PTFE tape with polyamide braid, 200°C, 600V
    "83": {
        "conductor": "Silver Plated Copper",
        "insulation": "PTFE/polyimide/PTFE tape with polyamide braid",
        "temp": 200,
        "voltage": 600,
        "specs": [
            ("4/0", 2109, 30, 14.35, 15.62, 16.64, 0.054),
            ("3/0", 1665, 30, 12.70, 14.07, 14.83, 0.068),
            ("2/0", 1330, 30, 11.18, 12.65, 13.41, 0.085),
            ("0", 1045, 30, 10.03, 11.23, 11.73, 0.108),
            ("1", 817, 30, 9.30, 10.16, 10.67, 0.139),
            ("2", 665, 30, 8.13, 9.14, 9.65, 0.170)
        ]
    },
    # M22759/84 - Nickel-plated, PTFE/polyimide/PTFE tape with polyamide braid, 260°C, 600V
    "84": {
        "conductor": "Nickel Plated Copper",
        "insulation": "PTFE/polyimide/PTFE tape with polyamide braid",
        "temp": 260,
        "voltage": 600,
        "specs": [
            ("4/0", 2109, 30, 14.35, 15.62, 16.64, 0.056),
            ("3/0", 1665, 30, 12.70, 14.07, 14.83, 0.071),
            ("2/0", 1330, 30, 11.18, 12.65, 13.41, 0.089),
            ("0", 1045, 30, 10.03, 11.23, 11.73, 0.113),
            ("1", 817, 30, 9.30, 10.16, 10.67, 0.144),
            ("2", 665, 30, 8.13, 9.14, 9.65, 0.177)
        ]
    },
    # M22759/85 - Tin-plated, PTFE/polyimide/PTFE tape with polyamide braid, 150°C, 600V
    "85": {
        "conductor": "Tin Plated Copper",
        "insulation": "PTFE/polyimide/PTFE tape with polyamide braid",
        "temp": 150,
        "voltage": 600,
        "specs": [
            ("4/0", 2109, 30, 14.35, 15.62, 16.64, 0.056),
            ("3/0", 1665, 30, 12.70, 14.07, 14.83, 0.071),
            ("2/0", 1330, 30, 11.18, 12.65, 13.41, 0.091),
            ("0", 1045, 30, 10.03, 11.23, 11.73, 0.116),
            ("1", 817, 30, 9.30, 10.16, 10.67, 0.149),
            ("2", 665, 30, 8.13, 9.14, 9.65, 0.183)
        ]
    },
    # M22759/86 - Silver-plated, PTFE/polyimide tape, 200°C, 600V
    "86": {
        "conductor": "Silver Plated Copper",
        "insulation": "PTFE/polyimide tape",
        "temp": 200,
        "voltage": 600,
        "specs": [
            ("4/0", 2109, 30, 14.35, 14.99, 16.00, 0.054),
            ("3/0", 1665, 30, 12.70, 13.46, 14.22, 0.068),
            ("2/0", 1330, 30, 11.18, 12.07, 12.83, 0.085),
            ("0", 1045, 30, 10.03, 10.67, 11.43, 0.108),
            ("1", 817, 30, 9.30, 9.86, 10.36, 0.139),
            ("2", 665, 30, 8.13, 8.74, 9.25, 0.170),
            ("4", 133, 25, 6.35, 7.01, 7.32, 0.264),
            ("6", 133, 27, 5.03, 5.56, 5.82, 0.418),
            ("8", 133, 29, 4.01, 4.57, 4.78, 0.658),
            ("10", 37, 26, 2.69, 3.10, 3.23, 1.19),
            ("12", 37, 28, 2.12, 2.54, 2.67, 1.90),
            ("14", 19, 27, 1.64, 2.06, 2.18, 2.88),
            ("16", 19, 29, 1.31, 1.73, 1.85, 4.52),
            ("18", 19, 30, 1.16, 1.55, 1.65, 5.79),
            ("20", 19, 32, 0.93, 1.30, 1.40, 9.19),
            ("22", 19, 34, 0.72, 1.09, 1.19, 15.1),
            ("24", 19, 36, 0.57, 0.97, 1.07, 24.3),
            ("26", 19, 38, 0.44, 0.84, 0.94, 38.4)
        ]
    },
    # M22759/87 - Nickel-plated, PTFE/polyimide tape, 260°C, 600V
    "87": {
        "conductor": "Nickel Plated Copper",
        "insulation": "PTFE/polyimide tape",
        "temp": 260,
        "voltage": 600,
        "specs": [
            ("4/0", 2109, 30, 14.35, 14.99, 16.00, 0.056),
            ("3/0", 1665, 30, 12.70, 13.46, 14.22, 0.071),
            ("2/0", 1330, 30, 11.18, 12.07, 12.83, 0.089),
            ("0", 1045, 30, 10.03, 10.67, 11.43, 0.113),
            ("1", 817, 30, 9.30, 9.86, 10.36, 0.144),
            ("2", 665, 30, 8.13, 8.74, 9.25, 0.177),
            ("4", 133, 25, 6.35, 7.01, 7.32, 0.275),
            ("6", 133, 27, 5.03, 5.56, 5.82, 0.436),
            ("8", 133, 29, 4.01, 4.57, 4.78, 0.694),
            ("10", 37, 26, 2.69, 3.10, 3.23, 1.24),
            ("12", 37, 28, 2.12, 2.54, 2.67, 1.98),
            ("14", 19, 27, 1.64, 2.06, 2.18, 3.00),
            ("16", 19, 29, 1.31, 1.73, 1.85, 4.76),
            ("18", 19, 30, 1.16, 1.55, 1.65, 6.10),
            ("20", 19, 32, 0.93, 1.30, 1.40, 9.77),
            ("22", 19, 34, 0.72, 1.09, 1.19, 16.0),
            ("24", 19, 36, 0.57, 0.97, 1.07, 25.9),
            ("26", 19, 38, 0.44, 0.84, 0.94, 42.2)
        ]
    },
    # M22759/88 - Tin-plated, PTFE/polyimide tape, 150°C, 600V
    "88": {
        "conductor": "Tin Plated Copper",
        "insulation": "PTFE/polyimide tape",
        "temp": 150,
        "voltage": 600,
        "specs": [
            ("4/0", 2109, 30, 14.35, 14.99, 16.00, 0.056),
            ("3/0", 1665, 30, 12.70, 13.46, 14.22, 0.071),
            ("2/0", 1330, 30, 11.18, 12.07, 12.83, 0.091),
            ("0", 1045, 30, 10.03, 10.67, 11.43, 0.116),
            ("1", 817, 30, 9.30, 9.86, 10.36, 0.149),
            ("2", 665, 30, 8.13, 8.74, 9.25, 0.183),
            ("4", 133, 25, 6.35, 7.01, 7.32, 0.280),
            ("6", 133, 27, 5.03, 5.56, 5.82, 0.445),
            ("8", 133, 29, 4.01, 4.57, 4.78, 0.701),
            ("10", 37, 26, 2.69, 3.10, 3.23, 1.26),
            ("12", 37, 28, 2.12, 2.54, 2.67, 2.02),
            ("14", 19, 27, 1.64, 2.06, 2.18, 3.06),
            ("16", 19, 29, 1.31, 1.73, 1.85, 4.81),
            ("18", 19, 30, 1.16, 1.55, 1.65, 6.23),
            ("20", 19, 32, 0.93, 1.30, 1.40, 9.88),
            ("22", 19, 34, 0.73, 1.09, 1.19, 16.2),
            ("24", 19, 36, 0.57, 0.97, 1.07, 26.2),
            ("26", 19, 38, 0.44, 0.84, 0.94, 41.3)
        ]
    },
    # M22759/89 - Silver-plated high-strength, PTFE/polyimide tape, 200°C, 600V
    "89": {
        "conductor": "Silver Plated High-Strength Copper Alloy",
        "insulation": "PTFE/polyimide tape",
        "temp": 200,
        "voltage": 600,
        "specs": [
            ("20", 19, 32, 0.93, 1.30, 1.40, 10.7),
            ("22", 19, 34, 0.72, 1.09, 1.19, 17.5),
            ("24", 19, 36, 0.57, 0.965, 1.07, 28.4),
            ("26", 19, 38, 0.44, 0.84, 0.94, 56.4)
        ]
    },
    # M22759/90 - Nickel-plated high-strength, PTFE/polyimide tape, 260°C, 600V
    "90": {
        "conductor": "Nickel Plated High-Strength Copper Alloy",
        "insulation": "PTFE/polyimide tape",
        "temp": 260,
        "voltage": 600,
        "specs": [
            ("20", 19, 32, 0.93, 1.30, 1.40, 11.4),
            ("22", 19, 34, 0.72, 1.09, 1.19, 18.6),
            ("24", 19, 36, 0.57, 0.965, 1.07, 30.1),
            ("26", 19, 38, 0.44, 0.84, 0.94, 58.4)
        ]
    },
    # M22759/91 - Silver-plated, PTFE/polyimide/PTFE tape (light weight), 200°C, 600V
    "91": {
        "conductor": "Silver Plated Copper",
        "insulation": "PTFE/polyimide/PTFE tape (light weight)",
        "temp": 200,
        "voltage": 600,
        "specs": [
            ("10", 37, 26, 2.69, 3.02, 3.12, 1.19),
            ("12", 37, 28, 2.12, 2.44, 2.54, 1.90),
            ("14", 19, 27, 1.64, 1.93, 2.03, 2.88),
            ("16", 19, 29, 1.31, 1.60, 1.70, 4.52),
            ("18", 19, 30, 1.16, 1.42, 1.52, 5.79),
            ("20", 19, 32, 0.93, 1.22, 1.30, 9.19),
            ("22", 19, 34, 0.72, 1.02, 1.09, 15.1),
            ("24", 19, 36, 0.57, 0.86, 0.97, 24.3),
            ("26", 19, 38, 0.44, 0.76, 0.86, 38.4)
        ]
    },
    # M22759/92 - Nickel-plated, PTFE/polyimide/PTFE tape (light weight), 260°C, 600V
    "92": {
        "conductor": "Nickel Plated Copper",
        "insulation": "PTFE/polyimide/PTFE tape (light weight)",
        "temp": 260,
        "voltage": 600,
        "specs": [
            ("10", 37, 26, 2.69, 3.02, 3.12, 1.24),
            ("12", 37, 28, 2.12, 2.44, 2.54, 1.98),
            ("14", 19, 27, 1.64, 1.93, 2.03, 3.00),
            ("16", 19, 29, 1.31, 1.60, 1.70, 4.76),
            ("18", 19, 30, 1.16, 1.42, 1.52, 6.10),
            ("20", 19, 32, 0.93, 1.22, 1.30, 9.77),
            ("22", 19, 34, 0.72, 1.02, 1.09, 16.0),
            ("24", 19, 36, 0.57, 0.86, 0.97, 25.9),
            ("26", 19, 38, 0.44, 0.76, 0.86, 42.2)
        ]
    }
}

def create_cable_entry(slash_num, gauge, conductor_dia, insulation_min, insulation_max, conductor_mat, insulation_type):
    """Create a single cable JSON entry"""
    # Calculate insulation thickness (average of min/max diameter minus conductor diameter, divided by 2)
    avg_insulation_dia = (insulation_min + insulation_max) / 2.0
    insulation_thickness = (avg_insulation_dia - conductor_dia) / 2.0

    cable_id = f"m22759-{slash_num}-{gauge}".replace("/", "-")
    part_number = f"M22759/{slash_num}-{gauge}"

    return {
        "CableId": cable_id,
        "PartNumber": part_number,
        "Manufacturer": "MIL-SPEC",
        "Name": f"M22759/{slash_num} {gauge} AWG {conductor_mat} - {insulation_type}",
        "Type": 0,  # SingleCore
        "Cores": [
            {
                "CoreId": "1",
                "ConductorDiameter": conductor_dia,
                "InsulationThickness": insulation_thickness,
                "InsulationColor": "Natural",
                "Gauge": gauge,
                "ConductorMaterial": conductor_mat,
                "SignalName": "",
                "SignalDescription": "",
                "PinA": "",
                "PinB": "",
                "WireLabel": "",
                "SignalType": 0
            }
        ],
        "JacketThickness": insulation_thickness,
        "JacketColor": "Natural",
        "HasShield": False,
        "ShieldType": 0,
        "ShieldThickness": 0,
        "ShieldCoverage": 0,
        "HasDrainWire": False,
        "DrainWireDiameter": 0,
        "IsFiller": False,
        "FillerMaterial": "Nylon",
        "SpecifiedOuterDiameter": avg_insulation_dia
    }

# Generate all cable entries
all_cables = []
total_count = 0

for slash_num, data in m22759_specs.items():
    conductor = data["conductor"]
    insulation = data["insulation"]

    for spec in data["specs"]:
        gauge, strands, strand_gauge, conductor_dia, insulation_min, insulation_max, resistance = spec

        cable = create_cable_entry(
            slash_num,
            gauge,
            conductor_dia,
            insulation_min,
            insulation_max,
            conductor,
            insulation
        )

        all_cables.append(cable)
        total_count += 1

print(f"Generated {total_count} M22759 cable entries")

# Read existing CableLibrary.json
library_path = r"c:\Users\charl\OneDrive\Archive\Documents Churchie\CableConcentricityCalculator\CableConcentricityCalculator\Libraries\CableLibrary.json"

with open(library_path, 'r') as f:
    library = json.load(f)

# Append M22759 cables to existing library
library["Cables"].extend(all_cables)

# Write back to file with proper formatting
with open(library_path, 'w') as f:
    json.dump(library, f, indent=4)

print(f"Successfully appended {total_count} M22759 cables to CableLibrary.json")
print(f"Total cables in library: {len(library['Cables'])}")
