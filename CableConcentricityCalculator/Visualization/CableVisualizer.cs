using CableConcentricityCalculator.Models;
using CableConcentricityCalculator.Services;
using CableConcentricityCalculator.Utilities;
using SkiaSharp;

namespace CableConcentricityCalculator.Visualization;

/// <summary>
/// Generates visual representations of cable assemblies
/// </summary>
public class CableVisualizer
{
    private const float DefaultScale = 50f; // pixels per mm
    private const float MinImageSize = 400f;
    private const float MaxImageSize = 2000f;
    private const float Padding = 50f;

    /// <summary>
    /// Generate a cross-section image of the cable assembly
    /// </summary>
    public static byte[] GenerateCrossSectionImage(CableAssembly assembly, int width = 800, int height = 800)
    {
        // Calculate scale based on assembly diameter
        float assemblyDiameter = (float)assembly.OverallDiameter;
        if (assemblyDiameter <= 0) assemblyDiameter = 10f;

        float availableSize = Math.Min(width, height) - 2 * Padding;
        float scale = availableSize / assemblyDiameter;

        // Cap the scale
        scale = Math.Min(scale, DefaultScale * 2);
        scale = Math.Max(scale, 5f);

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;

        // Clear background
        canvas.Clear(SKColors.White);

        // Center point
        float centerX = width / 2f;
        float centerY = height / 2f;

        // Draw from outside in so inner elements are on top
        DrawOuterElements(canvas, assembly, centerX, centerY, scale);
        DrawLayers(canvas, assembly, centerX, centerY, scale);
        DrawLegend(canvas, assembly, width, height);
        DrawDimensionInfo(canvas, assembly, width, height);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        return data.ToArray();
    }

    /// <summary>
    /// Draw outer elements (jacket, heat shrink, braid)
    /// </summary>
    private static void DrawOuterElements(SKCanvas canvas, CableAssembly assembly,
        float centerX, float centerY, float scale)
    {
        float currentRadius = (float)assembly.OverallDiameter / 2 * scale;

        // Draw outer jacket if present
        if (assembly.OuterJacket != null)
        {
            var jacketPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = GetColor(assembly.OuterJacket.Color).WithAlpha(200)
            };
            canvas.DrawCircle(centerX, centerY, currentRadius, jacketPaint);

            var jacketStroke = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black,
                StrokeWidth = 1.5f
            };
            canvas.DrawCircle(centerX, centerY, currentRadius, jacketStroke);

            currentRadius -= (float)assembly.OuterJacket.WallThickness * scale;
        }

        // Draw heat shrinks
        foreach (var hs in assembly.HeatShrinks.Where(h => h.AppliedOverLayer == -1))
        {
            var hsPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = GetColor(hs.Color).WithAlpha(180)
            };
            canvas.DrawCircle(centerX, centerY, currentRadius, hsPaint);

            var hsStroke = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.DarkGray,
                StrokeWidth = 1f,
                PathEffect = SKPathEffect.CreateDash(new[] { 4f, 2f }, 0)
            };
            canvas.DrawCircle(centerX, centerY, currentRadius, hsStroke);

            currentRadius -= (float)hs.TotalWallAddition / 2 * scale;
        }

        // Draw over-braids
        foreach (var braid in assembly.OverBraids.OrderByDescending(b => b.AppliedOverLayer))
        {
            DrawBraid(canvas, centerX, centerY, currentRadius, braid, scale);
            currentRadius -= (float)braid.WallThickness * scale;
        }
    }

    /// <summary>
    /// Draw braid pattern
    /// </summary>
    private static void DrawBraid(SKCanvas canvas, float centerX, float centerY,
        float radius, OverBraid braid, float scale)
    {
        var braidPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = GetColor(braid.Color).WithAlpha(160)
        };

        float innerRadius = radius - (float)braid.WallThickness * scale;
        float midRadius = (radius + innerRadius) / 2;

        // Draw base fill
        canvas.DrawCircle(centerX, centerY, radius, braidPaint);

        // Draw braid pattern (cross-hatch)
        var patternPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = GetColor(braid.Color).WithAlpha(200),
            StrokeWidth = 1f
        };

        // Draw crossing lines to simulate braid pattern
        int lineCount = 24;
        for (int i = 0; i < lineCount; i++)
        {
            float angle = i * 2 * MathF.PI / lineCount;

            // Diagonal lines
            float x1 = centerX + midRadius * MathF.Cos(angle);
            float y1 = centerY + midRadius * MathF.Sin(angle);
            float x2 = centerX + midRadius * MathF.Cos(angle + 0.3f);
            float y2 = centerY + midRadius * MathF.Sin(angle + 0.3f);

            canvas.DrawLine(x1, y1, x2, y2, patternPaint);
        }

        // Outline
        var strokePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.DarkGray,
            StrokeWidth = 1f
        };
        canvas.DrawCircle(centerX, centerY, radius, strokePaint);
        canvas.DrawCircle(centerX, centerY, innerRadius, strokePaint);
    }

    /// <summary>
    /// Draw all cable layers
    /// </summary>
    private static void DrawLayers(SKCanvas canvas, CableAssembly assembly,
        float centerX, float centerY, float scale)
    {
        // Calculate positions for all layers
        foreach (var layer in assembly.Layers.OrderBy(l => l.LayerNumber))
        {
            // Draw tape wrap if present
            if (layer.TapeWrap != null)
            {
                DrawTapeWrap(canvas, assembly, layer, centerX, centerY, scale);
            }

            // Calculate positions
            float pitchRadius = (float)ConcentricityCalculator.CalculateLayerPitchRadius(assembly, layer.LayerNumber) * scale;

            var elements = layer.GetElements();
            var angles = ConcentricityCalculator.CalculateAngularPositions(elements.Count);

            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                float cableRadius = (float)element.Diameter / 2 * scale;

                float x, y;
                if (layer.LayerNumber == 0 && elements.Count == 1)
                {
                    x = centerX;
                    y = centerY;
                }
                else if (layer.LayerNumber == 0)
                {
                    // Special center arrangements
                    var positions = GetCenterPositions(elements, scale);
                    x = centerX + positions[i].x;
                    y = centerY + positions[i].y;
                }
                else
                {
                    x = centerX + pitchRadius * MathF.Cos((float)angles[i]);
                    y = centerY + pitchRadius * MathF.Sin((float)angles[i]);
                }

                DrawCableElement(canvas, element, x, y, cableRadius, layer.LayerNumber);
            }
        }
    }

    /// <summary>
    /// Get positions for center layer elements
    /// </summary>
    private static List<(float x, float y)> GetCenterPositions(List<LayerElement> elements, float scale)
    {
        var positions = new List<(float x, float y)>();
        float d = elements.Max(e => (float)e.Diameter) * scale;

        switch (elements.Count)
        {
            case 1:
                positions.Add((0, 0));
                break;
            case 2:
                positions.Add((-d / 2, 0));
                positions.Add((d / 2, 0));
                break;
            case 3:
                float r3 = d / MathF.Sqrt(3);
                for (int i = 0; i < 3; i++)
                {
                    float angle = (i * 2 * MathF.PI / 3) - (MathF.PI / 2);
                    positions.Add((r3 * MathF.Cos(angle), r3 * MathF.Sin(angle)));
                }
                break;
            case 4:
                float r4 = d / MathF.Sqrt(2);
                for (int i = 0; i < 4; i++)
                {
                    float angle = (i * MathF.PI / 2) + (MathF.PI / 4);
                    positions.Add((r4 * MathF.Cos(angle), r4 * MathF.Sin(angle)));
                }
                break;
            case 5:
                float r5 = d / (2 * MathF.Sin(MathF.PI / 5));
                for (int i = 0; i < 5; i++)
                {
                    float angle = (i * 2 * MathF.PI / 5) - (MathF.PI / 2);
                    positions.Add((r5 * MathF.Cos(angle), r5 * MathF.Sin(angle)));
                }
                break;
            case 6:
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * MathF.PI / 3;
                    positions.Add((d * MathF.Cos(angle), d * MathF.Sin(angle)));
                }
                break;
            case 7:
                positions.Add((0, 0));
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * MathF.PI / 3;
                    positions.Add((d * MathF.Cos(angle), d * MathF.Sin(angle)));
                }
                break;
            default:
                // Approximate positioning for larger counts
                positions.Add((0, 0));
                int remaining = elements.Count - 1;
                float ringRadius = d;
                int placed = 1;
                while (placed < elements.Count)
                {
                    int inRing = Math.Min(6, remaining);
                    for (int i = 0; i < inRing; i++)
                    {
                        float angle = i * 2 * MathF.PI / inRing;
                        positions.Add((ringRadius * MathF.Cos(angle), ringRadius * MathF.Sin(angle)));
                        placed++;
                    }
                    ringRadius += d;
                    remaining -= inRing;
                }
                break;
        }

        return positions;
    }

    /// <summary>
    /// Draw a single cable element
    /// </summary>
    private static void DrawCableElement(SKCanvas canvas, LayerElement element,
        float x, float y, float radius, int layerNumber)
    {
        if (element.IsFiller && element.Cable == null)
        {
            // Draw filler
            var fillerPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = GetColor(element.Color).WithAlpha(200)
            };
            canvas.DrawCircle(x, y, radius, fillerPaint);

            var fillerStroke = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Gray,
                StrokeWidth = 0.5f,
                PathEffect = SKPathEffect.CreateDash(new[] { 2f, 2f }, 0)
            };
            canvas.DrawCircle(x, y, radius, fillerStroke);

            // Mark as filler
            var textPaint = new SKPaint
            {
                Color = SKColors.Gray,
                TextSize = Math.Max(8, radius / 2),
                TextAlign = SKTextAlign.Center
            };
            canvas.DrawText("F", x, y + textPaint.TextSize / 3, textPaint);
        }
        else if (element.Cable != null)
        {
            DrawCable(canvas, element.Cable, x, y, radius);
        }
    }

    /// <summary>
    /// Draw a cable with all its components
    /// </summary>
    private static void DrawCable(SKCanvas canvas, Cable cable, float x, float y, float totalRadius)
    {
        float currentRadius = totalRadius;
        float scale = totalRadius / ((float)cable.OuterDiameter / 2);

        // Draw outer jacket
        var jacketPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = GetColor(cable.JacketColor)
        };
        canvas.DrawCircle(x, y, currentRadius, jacketPaint);

        // Jacket outline
        var outlinePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Black,
            StrokeWidth = 1f
        };
        canvas.DrawCircle(x, y, currentRadius, outlinePaint);

        currentRadius -= (float)cable.JacketThickness * scale;

        // Draw shield if present
        if (cable.HasShield)
        {
            var shieldPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = new SKColor(180, 180, 180)
            };
            canvas.DrawCircle(x, y, currentRadius, shieldPaint);

            // Shield pattern
            var shieldPattern = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = new SKColor(160, 160, 160),
                StrokeWidth = 0.5f
            };
            int lines = 8;
            for (int i = 0; i < lines; i++)
            {
                float angle = i * MathF.PI / lines;
                float dx = currentRadius * 0.8f * MathF.Cos(angle);
                float dy = currentRadius * 0.8f * MathF.Sin(angle);
                canvas.DrawLine(x - dx, y - dy, x + dx, y + dy, shieldPattern);
            }

            currentRadius -= (float)cable.ShieldThickness * scale;
        }

        // Draw cores
        if (cable.Cores.Count == 1)
        {
            // Single core - centered
            DrawCore(canvas, cable.Cores[0], x, y, currentRadius, scale);
        }
        else if (cable.Cores.Count > 1)
        {
            // Multiple cores - arranged concentrically
            DrawMultipleCores(canvas, cable.Cores, x, y, currentRadius, scale);
        }
    }

    /// <summary>
    /// Draw a single core
    /// </summary>
    private static void DrawCore(SKCanvas canvas, CableCore core, float x, float y, float availableRadius, float scale)
    {
        float insulationRadius = (float)core.OverallDiameter / 2 * scale;
        float conductorRadius = (float)core.ConductorDiameter / 2 * scale;

        // Insulation
        var insulationPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = GetColor(core.InsulationColor)
        };
        canvas.DrawCircle(x, y, Math.Min(insulationRadius, availableRadius), insulationPaint);

        // Conductor (copper color)
        var conductorPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = new SKColor(184, 115, 51) // Copper color
        };
        canvas.DrawCircle(x, y, conductorRadius, conductorPaint);
    }

    /// <summary>
    /// Draw multiple cores
    /// </summary>
    private static void DrawMultipleCores(SKCanvas canvas, List<CableCore> cores,
        float centerX, float centerY, float bundleRadius, float scale)
    {
        var positions = GetCorePositions(cores.Count, bundleRadius);

        for (int i = 0; i < cores.Count && i < positions.Count; i++)
        {
            float coreRadius = (float)cores[i].OverallDiameter / 2 * scale * 0.8f;
            DrawCore(canvas, cores[i], centerX + positions[i].x, centerY + positions[i].y, coreRadius, scale);
        }
    }

    /// <summary>
    /// Get positions for multiple cores
    /// </summary>
    private static List<(float x, float y)> GetCorePositions(int count, float bundleRadius)
    {
        var positions = new List<(float x, float y)>();
        float spacing = bundleRadius * 0.6f;

        switch (count)
        {
            case 2:
                positions.Add((-spacing / 2, 0));
                positions.Add((spacing / 2, 0));
                break;
            case 3:
                for (int i = 0; i < 3; i++)
                {
                    float angle = (i * 2 * MathF.PI / 3) - (MathF.PI / 2);
                    positions.Add((spacing * 0.6f * MathF.Cos(angle), spacing * 0.6f * MathF.Sin(angle)));
                }
                break;
            case 4:
                for (int i = 0; i < 4; i++)
                {
                    float angle = (i * MathF.PI / 2) + (MathF.PI / 4);
                    positions.Add((spacing * 0.5f * MathF.Cos(angle), spacing * 0.5f * MathF.Sin(angle)));
                }
                break;
            default:
                // Arrange in a circle
                for (int i = 0; i < count; i++)
                {
                    float angle = i * 2 * MathF.PI / count;
                    float r = count <= 6 ? spacing * 0.5f : spacing * 0.6f;
                    positions.Add((r * MathF.Cos(angle), r * MathF.Sin(angle)));
                }
                break;
        }

        return positions;
    }

    /// <summary>
    /// Draw tape wrap
    /// </summary>
    private static void DrawTapeWrap(SKCanvas canvas, CableAssembly assembly,
        CableLayer layer, float centerX, float centerY, float scale)
    {
        if (layer.TapeWrap == null) return;

        // Calculate the radius at which tape wrap is applied
        float radius = 0;
        for (int i = 0; i <= layer.LayerNumber; i++)
        {
            var l = assembly.Layers[i];
            if (i == 0)
            {
                var elements = l.GetElements();
                if (elements.Count > 0)
                {
                    var positions = GetCenterPositions(elements, scale);
                    radius = positions.Max(p =>
                        MathF.Sqrt(p.x * p.x + p.y * p.y) + elements.Max(e => (float)e.Diameter) / 2 * scale);
                }
            }
            else
            {
                radius += (float)l.MaxCableDiameter * scale;
            }
        }

        // Draw tape indication
        var tapePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = GetColor(layer.TapeWrap.Color).WithAlpha(200),
            StrokeWidth = (float)layer.TapeWrap.EffectiveThickness * scale,
            PathEffect = SKPathEffect.CreateDash(new[] { 8f, 4f }, 0)
        };
        canvas.DrawCircle(centerX, centerY, radius + (float)layer.TapeWrap.EffectiveThickness * scale / 2, tapePaint);
    }

    /// <summary>
    /// Draw legend
    /// </summary>
    private static void DrawLegend(SKCanvas canvas, CableAssembly assembly, int width, int height)
    {
        float legendX = 10;
        float legendY = 10;
        float lineHeight = 16;

        var titlePaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 14,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };

        var textPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 11,
            IsAntialias = true
        };

        canvas.DrawText($"{assembly.PartNumber} Rev {assembly.Revision}", legendX, legendY + lineHeight, titlePaint);
        legendY += lineHeight;
        canvas.DrawText(assembly.Name, legendX, legendY + lineHeight, textPaint);
        legendY += lineHeight * 1.5f;

        // Draw layer legend
        canvas.DrawText("Layers:", legendX, legendY + lineHeight, titlePaint);
        legendY += lineHeight;

        foreach (var layer in assembly.Layers.OrderBy(l => l.LayerNumber))
        {
            string dirStr = layer.TwistDirection switch
            {
                TwistDirection.RightHand => "RH",
                TwistDirection.LeftHand => "LH",
                _ => "--"
            };

            int cableCount = layer.Cables.Count(c => !c.IsFiller);
            int fillerCount = layer.FillerCount + layer.Cables.Count(c => c.IsFiller);

            string text = $"L{layer.LayerNumber}: {cableCount} cables";
            if (fillerCount > 0) text += $" +{fillerCount} fill";
            if (layer.LayerNumber > 0) text += $" ({dirStr})";

            canvas.DrawText(text, legendX, legendY + lineHeight, textPaint);
            legendY += lineHeight;
        }
    }

    /// <summary>
    /// Draw dimension information
    /// </summary>
    private static void DrawDimensionInfo(SKCanvas canvas, CableAssembly assembly, int width, int height)
    {
        float infoX = width - 10;
        float infoY = 10;
        float lineHeight = 14;

        var textPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 11,
            TextAlign = SKTextAlign.Right,
            IsAntialias = true
        };

        canvas.DrawText($"OD: {assembly.OverallDiameter:F2} mm", infoX, infoY + lineHeight, textPaint);
        infoY += lineHeight;
        canvas.DrawText($"Core Bundle: {assembly.CoreBundleDiameter:F2} mm", infoX, infoY + lineHeight, textPaint);
        infoY += lineHeight;
        canvas.DrawText($"Conductors: {assembly.TotalConductorCount}", infoX, infoY + lineHeight, textPaint);
        infoY += lineHeight;
        canvas.DrawText($"Cables: {assembly.TotalCableCount}", infoX, infoY + lineHeight, textPaint);
        infoY += lineHeight;
        if (assembly.TotalFillerCount > 0)
        {
            canvas.DrawText($"Fillers: {assembly.TotalFillerCount}", infoX, infoY + lineHeight, textPaint);
        }
    }

    /// <summary>
    /// Get SKColor from color name
    /// </summary>
    private static SKColor GetColor(string colorName)
    {
        if (ColorUtilities.ColorMapSK.TryGetValue(colorName, out var color))
        {
            return color;
        }

        // Try to parse as hex
        if (colorName.StartsWith("#") && colorName.Length == 7)
        {
            try
            {
                byte r = Convert.ToByte(colorName.Substring(1, 2), 16);
                byte g = Convert.ToByte(colorName.Substring(3, 2), 16);
                byte b = Convert.ToByte(colorName.Substring(5, 2), 16);
                return new SKColor(r, g, b);
            }
            catch
            {
                // Fall through to default
            }
        }

        return SKColors.Gray;
    }

    /// <summary>
    /// Generate a simple text-based representation for console output
    /// </summary>
    public static string GenerateTextDiagram(CableAssembly assembly)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"╔══════════════════════════════════════════════════╗");
        sb.AppendLine($"║  Cable Assembly: {assembly.PartNumber,-30} ║");
        sb.AppendLine($"║  {assembly.Name,-47} ║");
        sb.AppendLine($"╠══════════════════════════════════════════════════╣");
        sb.AppendLine($"║  Overall Diameter: {assembly.OverallDiameter,8:F2} mm               ║");
        sb.AppendLine($"║  Core Bundle:      {assembly.CoreBundleDiameter,8:F2} mm               ║");
        sb.AppendLine($"║  Total Conductors: {assembly.TotalConductorCount,8}                   ║");
        sb.AppendLine($"║  Total Cables:     {assembly.TotalCableCount,8}                   ║");
        sb.AppendLine($"╠══════════════════════════════════════════════════╣");

        sb.AppendLine($"║  LAYER STRUCTURE (center → outside)              ║");
        sb.AppendLine($"║                                                  ║");

        foreach (var layer in assembly.Layers.OrderBy(l => l.LayerNumber))
        {
            string twist = layer.TwistDirection switch
            {
                TwistDirection.RightHand => "→ RH",
                TwistDirection.LeftHand => "← LH",
                _ => "  --"
            };

            int cables = layer.Cables.Count(c => !c.IsFiller);
            int fillers = layer.FillerCount + layer.Cables.Count(c => c.IsFiller);
            string fillStr = fillers > 0 ? $"+{fillers}F" : "   ";

            sb.AppendLine($"║  Layer {layer.LayerNumber}: {cables,3} cables {fillStr} {twist} {layer.LayLength,4}mm lay    ║");
        }

        sb.AppendLine($"║                                                  ║");

        if (assembly.OverBraids.Count > 0)
        {
            sb.AppendLine($"║  OVER-BRAIDS:                                    ║");
            foreach (var braid in assembly.OverBraids)
            {
                sb.AppendLine($"║    {braid.PartNumber,-20} {braid.CoveragePercent,3}% coverage    ║");
            }
        }

        if (assembly.HeatShrinks.Count > 0)
        {
            sb.AppendLine($"║  HEAT SHRINK:                                    ║");
            foreach (var hs in assembly.HeatShrinks)
            {
                sb.AppendLine($"║    {hs.PartNumber,-20} {hs.ShrinkRatio,-10}         ║");
            }
        }

        sb.AppendLine($"╚══════════════════════════════════════════════════╝");

        return sb.ToString();
    }
}
