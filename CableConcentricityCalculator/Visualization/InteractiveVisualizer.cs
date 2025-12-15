using CableConcentricityCalculator.Models;
using CableConcentricityCalculator.Services;
using SkiaSharp;

namespace CableConcentricityCalculator.Visualization;

/// <summary>
/// Information about a visual element for hit testing
/// </summary>
public class VisualElement
{
    public string Id { get; set; } = string.Empty;
    public VisualElementType Type { get; set; }
    public float CenterX { get; set; }
    public float CenterY { get; set; }
    public float Radius { get; set; }
    public Cable? Cable { get; set; }
    public CableCore? Core { get; set; }
    public int LayerNumber { get; set; }
    public int ElementIndex { get; set; }
    public Annotation? Annotation { get; set; }

    public bool ContainsPoint(float x, float y)
    {
        float dx = x - CenterX;
        float dy = y - CenterY;
        return (dx * dx + dy * dy) <= (Radius * Radius);
    }
}

public enum VisualElementType
{
    Cable,
    Core,
    Filler,
    HeatShrink,
    OverBraid,
    TapeWrap,
    Annotation
}

/// <summary>
/// Result of generating an interactive cross-section image
/// </summary>
public class InteractiveImageResult
{
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public List<VisualElement> Elements { get; set; } = new();
    public int Width { get; set; }
    public int Height { get; set; }
    public float Scale { get; set; }
    public float CenterX { get; set; }
    public float CenterY { get; set; }
}

/// <summary>
/// Generates interactive visual representations with hit testing support
/// </summary>
public class InteractiveVisualizer
{
    private const float Padding = 50f;

    private static readonly Dictionary<string, SKColor> ColorMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "White", SKColors.White },
        { "Black", SKColors.Black },
        { "Red", new SKColor(220, 20, 20) },
        { "Green", new SKColor(0, 150, 0) },
        { "Blue", new SKColor(0, 80, 180) },
        { "Yellow", new SKColor(240, 220, 0) },
        { "Orange", new SKColor(255, 140, 0) },
        { "Brown", new SKColor(139, 90, 43) },
        { "Violet", new SKColor(148, 0, 211) },
        { "Purple", new SKColor(148, 0, 211) },
        { "Gray", new SKColor(128, 128, 128) },
        { "Grey", new SKColor(128, 128, 128) },
        { "Pink", new SKColor(255, 182, 193) },
        { "Natural", new SKColor(245, 240, 220) },
        { "Clear", new SKColor(220, 220, 220) },
        { "Silver", new SKColor(192, 192, 192) },
        { "Tan", new SKColor(210, 180, 140) },
        { "Nylon", new SKColor(245, 245, 220) },
        { "Green/Yellow", new SKColor(154, 205, 50) }
    };

    /// <summary>
    /// Generate an interactive cross-section image with element tracking (synchronous wrapper)
    /// </summary>
    public static InteractiveImageResult GenerateInteractiveImage(CableAssembly assembly, int width = 800, int height = 800)
    {
        return GenerateInteractiveImageAsync(assembly, width, height).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Generate an interactive cross-section image with element tracking (async version)
    /// Runs image generation on background thread to avoid UI freezing
    /// </summary>
    public static async Task<InteractiveImageResult> GenerateInteractiveImageAsync(CableAssembly assembly, int width = 800, int height = 800)
    {
        // Offload rendering to thread pool
        return await Task.Run(() =>
        {
            var result = new InteractiveImageResult
            {
                Width = width,
                Height = height,
                CenterX = width / 2f,
                CenterY = height / 2f
            };

            float assemblyDiameter = (float)assembly.OverallDiameter;
            if (assemblyDiameter <= 0) assemblyDiameter = 10f;

            // Calculate scale to ensure assembly always fits within viewport
            float availableSize = Math.Min(width, height) - 2 * Padding;
            float scale = availableSize / assemblyDiameter;

            // Only set a minimum scale to prevent extremely small assemblies from being invisible
            // No maximum scale - let it zoom as needed to fit
            scale = Math.Max(scale, 0.1f);
            result.Scale = scale;

            DebugLogger.Log($"[VISUALIZER] Assembly diameter={assemblyDiameter:F2}mm, viewport={width}x{height}px, availableSize={availableSize:F2}px, scale={scale:F2}px/mm");

            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            float centerX = width / 2f;
            float centerY = height / 2f;

            // Draw and track elements
            DrawOuterElements(canvas, assembly, centerX, centerY, scale, result.Elements);
            DrawLayers(canvas, assembly, centerX, centerY, scale, result.Elements);
            DrawAnnotations(canvas, assembly, centerX, centerY, scale, result.Elements);
            DrawLegend(canvas, assembly, width, height);
            DrawDimensionInfo(canvas, assembly, width, height);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            result.ImageData = data.ToArray();

            return result;
        });
    }

    /// <summary>
    /// Find element at given coordinates
    /// </summary>
    public static VisualElement? HitTest(InteractiveImageResult image, float x, float y)
    {
        // Search from front to back (last drawn = on top)
        for (int i = image.Elements.Count - 1; i >= 0; i--)
        {
            if (image.Elements[i].ContainsPoint(x, y))
            {
                return image.Elements[i];
            }
        }
        return null;
    }

    /// <summary>
    /// Convert screen coordinates to assembly coordinates (mm from center)
    /// </summary>
    public static (double X, double Y) ScreenToAssemblyCoords(InteractiveImageResult image, float screenX, float screenY)
    {
        float relX = screenX - image.CenterX;
        float relY = screenY - image.CenterY;
        return (relX / image.Scale, relY / image.Scale);
    }

    private static void DrawOuterElements(SKCanvas canvas, CableAssembly assembly,
        float centerX, float centerY, float scale, List<VisualElement> elements)
    {
        float currentRadius = (float)assembly.OverallDiameter / 2 * scale;

        // Draw heat shrinks
        foreach (var hs in assembly.HeatShrinks.Where(h => h.AppliedOverLayer == -1))
        {
            elements.Add(new VisualElement
            {
                Id = hs.Id,
                Type = VisualElementType.HeatShrink,
                CenterX = centerX,
                CenterY = centerY,
                Radius = currentRadius
            });

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
            elements.Add(new VisualElement
            {
                Id = braid.Id ?? Guid.NewGuid().ToString("N")[..8],
                Type = VisualElementType.OverBraid,
                CenterX = centerX,
                CenterY = centerY,
                Radius = currentRadius
            });

            DrawBraid(canvas, centerX, centerY, currentRadius, braid, scale);
            currentRadius -= (float)braid.WallThickness * scale;
        }
    }

    private static void DrawBraid(SKCanvas canvas, float centerX, float centerY,
        float radius, OverBraid braid, float scale)
    {
        var braidPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = GetColor(braid.Color).WithAlpha(160)
        };

        float innerRadius = radius - (float)braid.WallThickness * scale;

        canvas.DrawCircle(centerX, centerY, radius, braidPaint);

        var patternPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = GetColor(braid.Color).WithAlpha(200),
            StrokeWidth = 1f
        };

        float midRadius = (radius + innerRadius) / 2;
        for (int i = 0; i < 24; i++)
        {
            float angle = i * 2 * MathF.PI / 24;
            float x1 = centerX + midRadius * MathF.Cos(angle);
            float y1 = centerY + midRadius * MathF.Sin(angle);
            float x2 = centerX + midRadius * MathF.Cos(angle + 0.3f);
            float y2 = centerY + midRadius * MathF.Sin(angle + 0.3f);
            canvas.DrawLine(x1, y1, x2, y2, patternPaint);
        }

        var strokePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.DarkGray,
            StrokeWidth = 1f
        };
        canvas.DrawCircle(centerX, centerY, radius, strokePaint);
        canvas.DrawCircle(centerX, centerY, innerRadius, strokePaint);
    }

    private static void DrawLayers(SKCanvas canvas, CableAssembly assembly,
        float centerX, float centerY, float scale, List<VisualElement> elements)
    {
        DebugLogger.Log($"[REWRITTEN VISUALIZER] Starting render for {assembly.Layers.Count} layers");

        foreach (var layer in assembly.Layers.OrderBy(l => l.LayerNumber))
        {
            DebugLogger.Log($"[REWRITTEN VISUALIZER] ========== LAYER {layer.LayerNumber} ==========");
            DebugLogger.Log($"[REWRITTEN VISUALIZER] Layer {layer.LayerNumber}: UsePartialLayerOptimization = {layer.UsePartialLayerOptimization}");

            var layerElements = layer.GetElements();
            DebugLogger.Log($"[REWRITTEN VISUALIZER] Layer {layer.LayerNumber}: Has {layerElements.Count} elements total");

            // ALWAYS get positions from CalculateCablePositions - it handles optimization internally
            var positions = ConcentricityCalculator.CalculateCablePositions(assembly, layer.LayerNumber);

            DebugLogger.Log($"[REWRITTEN VISUALIZER] Layer {layer.LayerNumber}: CalculateCablePositions returned {positions.Count} positions");

            // CRITICAL CHECK: positions count MUST match elements count
            if (positions.Count != layerElements.Count)
            {
                DebugLogger.Log($"[REWRITTEN VISUALIZER] ERROR: Position count mismatch! {positions.Count} positions for {layerElements.Count} elements");
            }

            // Draw each element at its calculated position
            for (int i = 0; i < layerElements.Count && i < positions.Count; i++)
            {
                var element = layerElements[i];
                var position = positions[i];

                // Calculate screen coordinates from mm coordinates
                float x = centerX + (float)position.X * scale;
                float y = centerY + (float)position.Y * scale;
                float cableRadius = (float)position.Diameter / 2 * scale;

                DebugLogger.Log($"[REWRITTEN VISUALIZER] Layer {layer.LayerNumber} Element {i}: Position=({position.X:F3}, {position.Y:F3})mm, Diameter={position.Diameter:F3}mm, Screen=({x:F1}, {y:F1})px, Radius={cableRadius:F1}px");

                // Track this element
                var visualElement = new VisualElement
                {
                    Id = element.Cable?.CableId ?? $"filler-{layer.LayerNumber}-{i}",
                    Type = element.IsFiller ? VisualElementType.Filler : VisualElementType.Cable,
                    CenterX = x,
                    CenterY = y,
                    Radius = cableRadius,
                    Cable = element.Cable,
                    LayerNumber = layer.LayerNumber,
                    ElementIndex = i
                };
                elements.Add(visualElement);

                DrawCableElement(canvas, element, x, y, cableRadius, layer.LayerNumber, elements);
            }
        }
    }

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
            default:
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

    private static void DrawCableElement(SKCanvas canvas, LayerElement element,
        float x, float y, float radius, int layerNumber, List<VisualElement> elements)
    {
        if (element.IsFiller && element.Cable == null)
        {
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
            DrawCable(canvas, element.Cable, x, y, radius, elements);
        }
    }

    private static void DrawCable(SKCanvas canvas, Cable cable, float x, float y, float totalRadius, List<VisualElement> elements)
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

            var shieldPattern = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = new SKColor(160, 160, 160),
                StrokeWidth = 0.5f
            };
            for (int i = 0; i < 8; i++)
            {
                float angle = i * MathF.PI / 8;
                float dx = currentRadius * 0.8f * MathF.Cos(angle);
                float dy = currentRadius * 0.8f * MathF.Sin(angle);
                canvas.DrawLine(x - dx, y - dy, x + dx, y + dy, shieldPattern);
            }

            currentRadius -= (float)cable.ShieldThickness * scale;
        }

        // Draw cores
        if (cable.Cores.Count == 1)
        {
            DrawCore(canvas, cable.Cores[0], x, y, currentRadius, scale, elements, cable.CableId);
        }
        else if (cable.Cores.Count > 1)
        {
            DrawMultipleCores(canvas, cable.Cores, x, y, currentRadius, scale, elements, cable.CableId);
        }
    }

    private static void DrawCore(SKCanvas canvas, CableCore core, float x, float y, float availableRadius, float scale, List<VisualElement> elements, string cableId)
    {
        float insulationRadius = (float)core.OverallDiameter / 2 * scale;
        float conductorRadius = (float)core.ConductorDiameter / 2 * scale;

        // When availableRadius is less than insulationRadius, we're in "scaled down" mode
        // Scale both insulation and conductor proportionally
        float scaleFactor = availableRadius < insulationRadius ? availableRadius / insulationRadius : 1.0f;
        float displayInsulationRadius = insulationRadius * scaleFactor;
        float displayConductorRadius = conductorRadius * scaleFactor;

        // Ensure minimum visible insulation ring (at least 2 pixels)
        // Reduce conductor slightly more to make insulation visible
        float minInsulationThickness = 2.0f;
        if (displayInsulationRadius - displayConductorRadius < minInsulationThickness)
        {
            displayConductorRadius = Math.Max(1.0f, displayInsulationRadius - minInsulationThickness);
        }

        // Track this core
        elements.Add(new VisualElement
        {
            Id = $"{cableId}-{core.CoreId}",
            Type = VisualElementType.Core,
            CenterX = x,
            CenterY = y,
            Radius = displayInsulationRadius,
            Core = core
        });

        var insulationPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = GetColor(core.InsulationColor)
        };
        canvas.DrawCircle(x, y, displayInsulationRadius, insulationPaint);

        // Add black outline around insulation
        var outlinePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Black,
            StrokeWidth = 1.0f,
            IsAntialias = true
        };
        canvas.DrawCircle(x, y, displayInsulationRadius, outlinePaint);

        var conductorPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = new SKColor(184, 115, 51)
        };
        canvas.DrawCircle(x, y, displayConductorRadius, conductorPaint);
    }

    private static void DrawMultipleCores(SKCanvas canvas, List<CableCore> cores,
        float centerX, float centerY, float bundleRadius, float scale, List<VisualElement> elements, string cableId)
    {
        // Get positions for cores with their actual diameters
        var positions = GetCorePositionsForDifferentSizes(cores, scale);

        for (int i = 0; i < cores.Count && i < positions.Count; i++)
        {
            // Render cores at full size (100%)
            float actualCoreRadius = (float)cores[i].OverallDiameter / 2 * scale;
            DrawCore(canvas, cores[i], centerX + positions[i].x, centerY + positions[i].y, actualCoreRadius, scale, elements, cableId);
        }
    }

    private static List<(float x, float y)> GetCorePositions(int count, float coreRadius)
    {
        var positions = new List<(float x, float y)>();

        // Calculate positions using proper geometry for circular packing
        // coreRadius is the radius of individual cores

        switch (count)
        {
            case 1:
                positions.Add((0, 0));
                break;
            case 2:
                // Two cores side by side, touching
                positions.Add((-coreRadius, 0));
                positions.Add((coreRadius, 0));
                break;
            case 3:
                // Triangle: distance from center to core center is coreRadius / sin(60°) = coreRadius / 0.866
                float r3 = coreRadius / MathF.Sin(MathF.PI / 3);
                for (int i = 0; i < 3; i++)
                {
                    float angle = (i * 2 * MathF.PI / 3) - (MathF.PI / 2);
                    positions.Add((r3 * MathF.Cos(angle), r3 * MathF.Sin(angle)));
                }
                break;
            case 4:
                // Square: distance from center is coreRadius / sin(45°) = coreRadius * √2
                float r4 = coreRadius * MathF.Sqrt(2);
                for (int i = 0; i < 4; i++)
                {
                    float angle = (i * MathF.PI / 2) + (MathF.PI / 4);
                    positions.Add((r4 * MathF.Cos(angle), r4 * MathF.Sin(angle)));
                }
                break;
            case 5:
                // Pentagon
                float r5 = coreRadius / MathF.Sin(MathF.PI / 5);
                for (int i = 0; i < 5; i++)
                {
                    float angle = (i * 2 * MathF.PI / 5) - (MathF.PI / 2);
                    positions.Add((r5 * MathF.Cos(angle), r5 * MathF.Sin(angle)));
                }
                break;
            case 6:
                // Hexagon: 6 cores around perimeter
                float r6 = coreRadius * 2; // Distance from center for hexagon
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * MathF.PI / 3;
                    positions.Add((r6 * MathF.Cos(angle), r6 * MathF.Sin(angle)));
                }
                break;
            case 7:
                // 1 center + 6 surrounding in hexagon
                positions.Add((0, 0));
                float r7 = coreRadius * 2;
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * MathF.PI / 3;
                    positions.Add((r7 * MathF.Cos(angle), r7 * MathF.Sin(angle)));
                }
                break;
            default:
                // For larger counts, arrange in ring
                // Use approximation: circumference = n * 2 * coreRadius, so radius = n * coreRadius / π
                float rDefault = count * coreRadius / MathF.PI;
                for (int i = 0; i < count; i++)
                {
                    float angle = i * 2 * MathF.PI / count;
                    positions.Add((rDefault * MathF.Cos(angle), rDefault * MathF.Sin(angle)));
                }
                break;
        }

        return positions;
    }

    private static List<(float x, float y)> GetCorePositionsForDifferentSizes(List<CableCore> cores, float scale)
    {
        var positions = new List<(float x, float y)>();

        if (cores.Count == 0)
            return positions;

        if (cores.Count == 1)
        {
            positions.Add((0, 0));
            return positions;
        }

        // Convert cores to radii in screen coordinates
        var radii = cores.Select(c => (float)c.OverallDiameter / 2 * scale).ToList();

        switch (cores.Count)
        {
            case 2:
                // Two cores touching - position based on their actual radii
                positions.Add((-radii[0], 0));
                positions.Add((radii[1], 0));
                break;

            case 3:
                // Triangular arrangement - three circles touching each other
                float r0 = radii[0];
                float r1 = radii[1];
                float r2 = radii[2];

                // Core 0 at origin, Core 1 to the right touching Core 0
                float x0 = 0;
                float y0 = 0;
                float x1 = r0 + r1;
                float y1 = 0;

                // Core 2: positioned to touch both Core 0 and Core 1
                float d01 = x1; // Distance between centers of core 0 and 1
                float d02 = r0 + r2; // Required distance from 0 to 2
                float d12 = r1 + r2; // Required distance from 1 to 2

                // Using triangle law of cosines
                float x2 = (d01 * d01 + d02 * d02 - d12 * d12) / (2 * d01);
                float y2 = MathF.Sqrt(d02 * d02 - x2 * x2);

                // Center the triangle at origin
                float centerX = (x0 + x1 + x2) / 3;
                float centerY = (y0 + y1 + y2) / 3;

                positions.Add((x0 - centerX, y0 - centerY));
                positions.Add((x1 - centerX, y1 - centerY));
                positions.Add((x2 - centerX, y2 - centerY));
                break;

            case 4:
                // Four cores arranged in square/diamond pattern
                float sqrt2 = MathF.Sqrt(2);
                float dist0 = radii[0] * sqrt2;
                float dist1 = radii[1] * sqrt2;
                float dist2 = radii[2] * sqrt2;
                float dist3 = radii[3] * sqrt2;

                // 45°, 135°, 225°, 315° - each touches 2 neighbors
                positions.Add((dist0 / sqrt2, dist0 / sqrt2));
                positions.Add((-dist1 / sqrt2, dist1 / sqrt2));
                positions.Add((-dist2 / sqrt2, -dist2 / sqrt2));
                positions.Add((dist3 / sqrt2, -dist3 / sqrt2));
                break;

            default:
                // For 5+ cores, use the simple approach with max diameter
                // This maintains backward compatibility for complex cases
                float maxRadius = radii.Max();
                var simplePositions = GetCorePositions(cores.Count, maxRadius);
                positions.AddRange(simplePositions);
                break;
        }

        return positions;
    }

    private static void DrawAnnotations(SKCanvas canvas, CableAssembly assembly,
        float centerX, float centerY, float scale, List<VisualElement> elements)
    {
        foreach (var annotation in assembly.Annotations)
        {
            float pointX = centerX + (float)annotation.X * scale;
            float pointY = centerY + (float)annotation.Y * scale;
            float balloonX = pointX + (float)annotation.BalloonOffsetX * scale;
            float balloonY = pointY + (float)annotation.BalloonOffsetY * scale;

            // Draw leader line
            if (annotation.ShowLeaderLine)
            {
                var linePaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.Black,
                    StrokeWidth = 1f
                };
                canvas.DrawLine(pointX, pointY, balloonX, balloonY, linePaint);
            }

            // Draw balloon
            float balloonRadius = 12f;
            var balloonFill = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = GetColor(annotation.BalloonColor)
            };
            canvas.DrawCircle(balloonX, balloonY, balloonRadius, balloonFill);

            var balloonStroke = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black,
                StrokeWidth = 1.5f
            };
            canvas.DrawCircle(balloonX, balloonY, balloonRadius, balloonStroke);

            // Draw number
            var textPaint = new SKPaint
            {
                Color = GetColor(annotation.TextColor),
                TextSize = 10f,
                TextAlign = SKTextAlign.Center,
                IsAntialias = true
            };
            canvas.DrawText(annotation.BalloonNumber.ToString(), balloonX, balloonY + 4, textPaint);

            // Track for hit testing
            elements.Add(new VisualElement
            {
                Id = annotation.Id,
                Type = VisualElementType.Annotation,
                CenterX = balloonX,
                CenterY = balloonY,
                Radius = balloonRadius,
                Annotation = annotation
            });
        }
    }

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
    }

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
    }

    private static SKColor GetColor(string colorName)
    {
        if (string.IsNullOrWhiteSpace(colorName))
            return SKColors.White; // White is visible against gray background

        if (ColorMap.TryGetValue(colorName, out var color))
            return color;

        if (colorName.StartsWith("#") && colorName.Length == 7)
        {
            try
            {
                byte r = Convert.ToByte(colorName.Substring(1, 2), 16);
                byte g = Convert.ToByte(colorName.Substring(3, 2), 16);
                byte b = Convert.ToByte(colorName.Substring(5, 2), 16);
                return new SKColor(r, g, b);
            }
            catch { }
        }

        return SKColors.White; // White for unrecognized colors
    }
}
