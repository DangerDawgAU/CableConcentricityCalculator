using CableConcentricityCalculator.Models;
using CableConcentricityCalculator.Services;
using SkiaSharp;

namespace CableConcentricityCalculator.Visualization;

/// <summary>
/// Generates 3D isometric visualizations of cable assemblies
/// </summary>
public static class Cable3DVisualizer
{
    private const float IsoAngle = 30f * MathF.PI / 180f; // 30 degrees for isometric
    private const float CosIso = 0.866f; // cos(30°)
    private const float SinIso = 0.5f;   // sin(30°)

    private static readonly Dictionary<string, SKColor> ColorMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "White", SKColors.White },
        { "Black", new SKColor(40, 40, 40) },
        { "Red", new SKColor(220, 20, 20) },
        { "Green", new SKColor(0, 150, 0) },
        { "Blue", new SKColor(0, 80, 180) },
        { "Yellow", new SKColor(240, 220, 0) },
        { "Orange", new SKColor(255, 140, 0) },
        { "Brown", new SKColor(139, 90, 43) },
        { "Violet", new SKColor(148, 0, 211) },
        { "Gray", new SKColor(128, 128, 128) },
        { "Silver", new SKColor(192, 192, 192) },
        { "Clear", new SKColor(220, 220, 220, 180) },
        { "Natural", new SKColor(245, 240, 220) }
    };

    /// <summary>
    /// Generate a 3D isometric view showing cables as solid spiraling cylinders
    /// </summary>

    private class CableQuadSegment
    {
        public string ColorName { get; set; } = "Black";
        public required CablePoint P0 { get; set; }
        public required CablePoint P1 { get; set; }
        public required CablePoint P2 { get; set; }
        public required CablePoint P3 { get; set; }
        public float AvgZ { get; set; }
    }

    private class CablePoint
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }
    public static byte[] GenerateIsometricCrossSection(CableAssembly assembly, int width = 800, int height = 600)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(new SKColor(245, 245, 245));

        float assemblyDiameter = (float)assembly.OverallDiameter;
        if (assemblyDiameter <= 0) assemblyDiameter = 10f;

        float cableLength = width * 0.65f;
        float centerX = width * 0.5f;
        float centerY = height * 0.45f;

        // Draw the solid spiral cables
        DrawSolidSpiralCables(canvas, assembly, centerX, centerY, cableLength, assemblyDiameter);

        // Draw title and info
        DrawInfo(canvas, assembly, width, height);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>
    /// Draw cables as solid ribbon/tube shapes spiraling together
    /// </summary>
    private static void DrawSolidSpiralCables(SKCanvas canvas, CableAssembly assembly,
        float centerX, float centerY, float cableLength, float assemblyDiameter)
    {
        if (assembly.Layers.Count == 0) return;

        float scale = cableLength / 80f;
        float startX = centerX - cableLength / 2;
        float endX = centerX + cableLength / 2;

        // Draw outer cylinder reference
        DrawCylinderBody(canvas, startX, endX, centerY, assemblyDiameter * scale * 0.5f);

        // Collect small quad segments for each cable element so we can depth-sort per-segment
        var cableQuads = new List<CableQuadSegment>();

        // Generate per-segment quads for each element
        foreach (var layer in assembly.Layers.OrderBy(l => l.LayerNumber))
        {
            if (layer.Cables.Count == 0) continue;

            var elements = layer.GetElements();
            var baseAngles = ConcentricityCalculator.CalculateAngularPositions(elements.Count);
            float pitchRadius = (float)ConcentricityCalculator.CalculateLayerPitchRadius(assembly, layer.LayerNumber) * scale;
            float twistDir = layer.TwistDirection == TwistDirection.RightHand ? 1f : -1f;
            float layLength = (float)layer.LayLength;
            if (layLength <= 0) layLength = 50f;

            float rotPerUnit = (2 * MathF.PI) / layLength;
            float cableRadius = (float)layer.Cables[0].OuterDiameter / 2 * scale;

            for (int e = 0; e < elements.Count; e++)
            {
                var element = elements[e];

                int numSegments = 120; // increased sampling for smooth silhouette and correct occlusion

                // build top and bottom edge lists of points
                var top = new List<CablePoint>(numSegments + 1);
                var bottom = new List<CablePoint>(numSegments + 1);

                for (int seg = 0; seg <= numSegments; seg++)
                {
                    float t = (float)seg / numSegments;
                    float x = startX + (endX - startX) * t;
                    float baseAngle = (float)baseAngles[e];
                    float spiralAngle = twistDir * rotPerUnit * (cableLength / scale) * t;
                    float angle = baseAngle + spiralAngle;

                    float yOffset = pitchRadius * MathF.Sin(angle);
                    float zOffset = pitchRadius * MathF.Cos(angle);
                    float y = centerY + yOffset;

                    float ribbonHeight = cableRadius * 1.25f;

                    top.Add(new CablePoint { X = x, Y = y - ribbonHeight, Z = zOffset });
                    bottom.Add(new CablePoint { X = x, Y = y + ribbonHeight, Z = zOffset });
                }

                // create quad segments between successive samples
                // small horizontal overlap to avoid hairline seams between adjacent quads
                const float overlap = 1.2f;
                for (int i = 0; i < numSegments; i++)
                {
                    // quad points in order: top[i], top[i+1], bottom[i+1], bottom[i]
                    var p0 = top[i];
                    var p1 = top[i + 1];
                    var p2 = bottom[i + 1];
                    var p3 = bottom[i];

                    // expand slightly in X to ensure overlap and hide seam artifacts
                    p0.X -= overlap;
                    p1.X += overlap;
                    p2.X += overlap;
                    p3.X -= overlap;

                    var quad = new CableQuadSegment
                    {
                        ColorName = element.Color,
                        P0 = p0,
                        P1 = p1,
                        P2 = p2,
                        P3 = p3,
                        AvgZ = (p0.Z + p1.Z + p2.Z + p3.Z) * 0.25f
                    };
                    cableQuads.Add(quad);
                }
            }
        }

        // Depth-sort quad segments back-to-front
        var sortedQuads = cableQuads.OrderBy(q => q.AvgZ).ToList();

        // Draw each quad in depth order
        foreach (var q in sortedQuads)
        {
            var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = GetColor(q.ColorName),
                // Disable antialias on fills to avoid semi-transparent hairline seams between adjacent quads
                IsAntialias = false
            };

            var path2d = new SKPath();
            path2d.MoveTo(q.P0.X, q.P0.Y);
            path2d.LineTo(q.P1.X, q.P1.Y);
            path2d.LineTo(q.P2.X, q.P2.Y);
            path2d.LineTo(q.P3.X, q.P3.Y);
            path2d.Close();

            canvas.DrawPath(path2d, paint);
        }
    }

    /// <summary>
    /// Draw cable outlines to define edges clearly
    /// </summary>
    private static void DrawCableOutlines(SKCanvas canvas, CableAssembly assembly,
        float startX, float endX, float centerY, float scale)
    {
        foreach (var layer in assembly.Layers.OrderBy(l => l.LayerNumber))
        {
            if (layer.Cables.Count == 0) continue;

            var elements = layer.GetElements();
            var baseAngles = ConcentricityCalculator.CalculateAngularPositions(elements.Count);
            float pitchRadius = (float)ConcentricityCalculator.CalculateLayerPitchRadius(assembly, layer.LayerNumber) * scale;
            float twistDir = layer.TwistDirection == TwistDirection.RightHand ? 1f : -1f;
            float layLength = (float)layer.LayLength;
            if (layLength <= 0) layLength = 50f;

            float rotPerUnit = (2 * MathF.PI) / layLength;
            float cableRadius = (float)layer.Cables[0].OuterDiameter / 2 * scale;

            for (int e = 0; e < elements.Count; e++)
            {
                var element = elements[e];
                var topEdge = new List<SKPoint>();
                var bottomEdge = new List<SKPoint>();

                // Create path
                int numSegments = 60;
                for (int seg = 0; seg <= numSegments; seg++)
                {
                    float t = (float)seg / numSegments;
                    float x = startX + (endX - startX) * t;
                    float baseAngle = (float)baseAngles[e];
                    float spiralAngle = twistDir * rotPerUnit * (endX - startX) * t;
                    float angle = baseAngle + spiralAngle;

                    float yOffset = pitchRadius * MathF.Sin(angle);
                    float ribbonHeight = cableRadius * 1.2f;

                    topEdge.Add(new SKPoint(x, centerY + yOffset - ribbonHeight));
                    bottomEdge.Add(new SKPoint(x, centerY + yOffset + ribbonHeight));
                }

                // Draw top outline
                var topOutlinePaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.DarkGray,
                    StrokeWidth = 1.2f,
                    StrokeCap = SKStrokeCap.Round,
                    StrokeJoin = SKStrokeJoin.Round,
                    IsAntialias = true
                };

                var topPath = new SKPath();
                topPath.MoveTo(topEdge[0]);
                for (int i = 1; i < topEdge.Count; i++)
                {
                    topPath.LineTo(topEdge[i]);
                }
                // draw a lighter, thinner outline to reduce heavy gray overlay
                var thinOutline = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.DarkGray.WithAlpha(180),
                    StrokeWidth = 0.9f,
                    StrokeCap = SKStrokeCap.Round,
                    StrokeJoin = SKStrokeJoin.Round,
                    IsAntialias = true
                };
                canvas.DrawPath(topPath, thinOutline);

                // Draw bottom outline
                var bottomPath = new SKPath();
                bottomPath.MoveTo(bottomEdge[0]);
                for (int i = 1; i < bottomEdge.Count; i++)
                {
                    bottomPath.LineTo(bottomEdge[i]);
                }
                canvas.DrawPath(bottomPath, thinOutline);

                // (Removed per-segment side lines to avoid visual artifacts)
            }
        }
    }

    /// <summary>
    /// Draw the outer cylinder body for reference
    /// </summary>
    private static void DrawCylinderBody(SKCanvas canvas, float startX, float endX, float centerY, float radius)
    {
        var outlinePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = new SKColor(180, 180, 180),
            StrokeWidth = 2f,
            PathEffect = SKPathEffect.CreateDash(new[] { 5f, 3f }, 0),
            IsAntialias = true
        };

        // Top and bottom lines
        canvas.DrawLine(startX, centerY - radius, endX, centerY - radius, outlinePaint);
        canvas.DrawLine(startX, centerY + radius, endX, centerY + radius, outlinePaint);

        // End caps
        var capPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = new SKColor(180, 180, 180),
            StrokeWidth = 1.5f,
            IsAntialias = true
        };

        var startCapRect = new SKRect(startX - radius * 0.15f, centerY - radius, startX + radius * 0.15f, centerY + radius);
        canvas.DrawOval(startCapRect, capPaint);

        var endCapRect = new SKRect(endX - radius * 0.15f, centerY - radius, endX + radius * 0.15f, centerY + radius);
        canvas.DrawOval(endCapRect, capPaint);
    }

    /// <summary>
    /// Generate a twisted cable view showing the lay pattern
    /// </summary>
    public static byte[] GenerateTwistedView(CableAssembly assembly, int width = 800, int height = 600, int twistCycles = 2)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(new SKColor(250, 250, 250));

        float assemblyDiameter = (float)assembly.OverallDiameter;
        if (assemblyDiameter <= 0) assemblyDiameter = 10f;

        float scale = Math.Min(width * 0.3f, height * 0.7f) / assemblyDiameter;
        float centerX = width * 0.5f;
        float centerY = height * 0.5f;

        // Draw the twisted cable view
        DrawTwistedCable(canvas, assembly, centerX, centerY, scale, width, height, twistCycles);

        // Draw title and info
        var titlePaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 14,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };
        canvas.DrawText($"{assembly.PartNumber} - Twist Pattern", 10, 20, titlePaint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static void DrawIsometricSection(SKCanvas canvas, CableAssembly assembly,
        float centerX, float centerY, float scale, float depth)
    {
        float radius = (float)assembly.OverallDiameter / 2 * scale;

        // Draw back ellipse (cable section back face)
        var backEllipsePaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = new SKColor(200, 200, 200)
        };
        DrawIsometricEllipse(canvas, centerX, centerY - depth * SinIso, radius, backEllipsePaint, true);

        // Draw cylinder sides
        DrawCylinderSides(canvas, centerX, centerY, radius, depth);

        // Draw outer jacket surface
        var surfacePaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = GetColor(assembly.OuterJacket?.Color ?? "Black").WithAlpha(220)
        };
        DrawIsometricEllipse(canvas, centerX, centerY, radius, surfacePaint, false);

        // Draw cross-section plane (cut surface)
        DrawCrossSection(canvas, assembly, centerX, centerY, scale);
    }

    private static void DrawIsometricEllipse(SKCanvas canvas, float cx, float cy, float radius,
        SKPaint paint, bool isBack)
    {
        var path = new SKPath();
        float ellipseHeight = radius * SinIso;

        var rect = new SKRect(cx - radius, cy - ellipseHeight, cx + radius, cy + ellipseHeight);
        path.AddOval(rect);

        canvas.DrawPath(path, paint);

        // Draw outline
        var strokePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.DarkGray,
            StrokeWidth = 1f,
            IsAntialias = true
        };
        canvas.DrawPath(path, strokePaint);
    }

    private static void DrawCylinderSides(SKCanvas canvas, float cx, float cy, float radius, float depth)
    {
        float topY = cy - depth * SinIso;
        float ellipseHeight = radius * SinIso;

        // Draw left and right sides
        var sidePaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        // Left side gradient
        using var leftGradient = SKShader.CreateLinearGradient(
            new SKPoint(cx - radius, cy),
            new SKPoint(cx - radius * 0.5f, cy),
            new[] { new SKColor(80, 80, 80), new SKColor(120, 120, 120) },
            SKShaderTileMode.Clamp);
        sidePaint.Shader = leftGradient;

        var leftPath = new SKPath();
        leftPath.MoveTo(cx - radius, cy - ellipseHeight);
        leftPath.LineTo(cx - radius, topY - ellipseHeight);
        leftPath.ArcTo(new SKRect(cx - radius, topY - ellipseHeight, cx + radius, topY + ellipseHeight),
            180, 180, false);
        leftPath.LineTo(cx + radius, cy + ellipseHeight);
        leftPath.ArcTo(new SKRect(cx - radius, cy - ellipseHeight, cx + radius, cy + ellipseHeight),
            0, -180, false);
        leftPath.Close();

        canvas.DrawPath(leftPath, sidePaint);
    }

    private static void DrawCrossSection(SKCanvas canvas, CableAssembly assembly,
        float centerX, float centerY, float scale)
    {
        // Draw each layer's cables in the cross-section view
        foreach (var layer in assembly.Layers.OrderBy(l => l.LayerNumber))
        {
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
                    x = centerX;
                    y = centerY;
                }
                else
                {
                    x = centerX + pitchRadius * MathF.Cos((float)angles[i]);
                    y = centerY + pitchRadius * MathF.Sin((float)angles[i]) * SinIso;
                }

                DrawIsometricCable(canvas, element, x, y, cableRadius);
            }
        }
    }

    private static void DrawIsometricCable(SKCanvas canvas, LayerElement element, float x, float y, float radius)
    {
        float ellipseHeight = radius * SinIso;

        var fillPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = GetColor(element.Color),
            IsAntialias = true
        };

        var rect = new SKRect(x - radius, y - ellipseHeight, x + radius, y + ellipseHeight);
        canvas.DrawOval(rect, fillPaint);

        var strokePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.DarkGray,
            StrokeWidth = 0.5f,
            IsAntialias = true
        };
        canvas.DrawOval(rect, strokePaint);

        // Draw conductor in center
        if (element.Cable != null && element.Cable.Cores.Count > 0)
        {
            var conductorPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = new SKColor(184, 115, 51),
                IsAntialias = true
            };
            float condRadius = radius * 0.4f;
            float condHeight = condRadius * SinIso;
            var condRect = new SKRect(x - condRadius, y - condHeight, x + condRadius, y + condHeight);
            canvas.DrawOval(condRect, conductorPaint);
        }
    }

    private static void DrawTwistedCable(SKCanvas canvas, CableAssembly assembly,
        float centerX, float centerY, float scale, int width, int height, int twistCycles)
    {
        if (assembly.Layers.Count == 0) return;

        float cableLength = width * 0.8f;
        float startX = (width - cableLength) / 2;
        int steps = 100 * twistCycles;
        float stepSize = cableLength / steps;

        // Calculate average lay length for animation
        float avgLayLength = assembly.Layers.Count > 1
            ? (float)assembly.Layers.Skip(1).Average(l => l.LayLength)
            : 50f;

        float twistAnglePerStep = (2 * MathF.PI * twistCycles) / steps;

        // Draw each layer
        foreach (var layer in assembly.Layers.OrderBy(l => l.LayerNumber))
        {
            if (layer.LayerNumber == 0) continue; // Skip center layer for twist visualization

            float pitchRadius = (float)ConcentricityCalculator.CalculateLayerPitchRadius(assembly, layer.LayerNumber) * scale;
            var elements = layer.GetElements();
            var baseAngles = ConcentricityCalculator.CalculateAngularPositions(elements.Count);

            float twistDirection = layer.TwistDirection == TwistDirection.RightHand ? 1f : -1f;

            for (int e = 0; e < elements.Count; e++)
            {
                var element = elements[e];
                float cableRadius = (float)element.Diameter / 2 * scale * 0.8f;

                var path = new SKPath();
                bool pathStarted = false;

                for (int i = 0; i <= steps; i++)
                {
                    float x = startX + i * stepSize;
                    float angle = (float)baseAngles[e] + twistDirection * twistAnglePerStep * i;

                    // Project 3D position to 2D isometric
                    float offsetY = pitchRadius * MathF.Sin(angle);
                    float offsetZ = pitchRadius * MathF.Cos(angle);

                    // Isometric projection
                    float screenY = centerY - offsetY * 0.7f - offsetZ * 0.3f;

                    if (!pathStarted)
                    {
                        path.MoveTo(x, screenY);
                        pathStarted = true;
                    }
                    else
                    {
                        path.LineTo(x, screenY);
                    }
                }

                // Draw the helix path
                var pathPaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = GetColor(element.Color),
                    StrokeWidth = cableRadius * 2,
                    StrokeCap = SKStrokeCap.Round,
                    IsAntialias = true
                };

                // Only draw visible portions (front half of helix)
                canvas.DrawPath(path, pathPaint);

                // Draw outline
                var outlinePaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.DarkGray.WithAlpha(100),
                    StrokeWidth = cableRadius * 2 + 1,
                    StrokeCap = SKStrokeCap.Round,
                    IsAntialias = true
                };
                canvas.DrawPath(path, outlinePaint);
            }
        }

        // Draw layer 0 center cable as straight line
        if (assembly.Layers.Any(l => l.LayerNumber == 0))
        {
            var centerLayer = assembly.Layers.First(l => l.LayerNumber == 0);
            foreach (var cable in centerLayer.Cables.Take(1))
            {
                float cableRadius = (float)cable.OuterDiameter / 2 * scale * 0.8f;
                var centerPaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = GetColor(cable.JacketColor),
                    StrokeWidth = cableRadius * 2,
                    StrokeCap = SKStrokeCap.Round,
                    IsAntialias = true
                };
                canvas.DrawLine(startX, centerY, startX + cableLength, centerY, centerPaint);
            }
        }

        // Draw dimension arrows for lay length
        DrawLayLengthDimension(canvas, assembly, startX, centerY, cableLength, height, twistCycles);
    }

    private static void DrawLayLengthDimension(SKCanvas canvas, CableAssembly assembly,
        float startX, float centerY, float cableLength, int height, int twistCycles)
    {
        if (assembly.Layers.Count <= 1) return;

        var outerLayer = assembly.Layers.OrderByDescending(l => l.LayerNumber).First();
        float layLength = (float)outerLayer.LayLength;

        float y = height - 40;
        float oneTwistLength = cableLength / twistCycles;

        var dimPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Blue,
            StrokeWidth = 1f,
            IsAntialias = true
        };

        var textPaint = new SKPaint
        {
            Color = SKColors.Blue,
            TextSize = 11,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center
        };

        // Draw dimension line for one complete twist
        canvas.DrawLine(startX, y, startX + oneTwistLength, y, dimPaint);
        canvas.DrawLine(startX, y - 5, startX, y + 5, dimPaint);
        canvas.DrawLine(startX + oneTwistLength, y - 5, startX + oneTwistLength, y + 5, dimPaint);

        canvas.DrawText($"Lay Length: {layLength}mm", startX + oneTwistLength / 2, y - 8, textPaint);
    }

    private static void DrawInfo(SKCanvas canvas, CableAssembly assembly, int width, int height)
    {
        var titlePaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 14,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };

        var textPaint = new SKPaint
        {
            Color = SKColors.DarkGray,
            TextSize = 11,
            IsAntialias = true
        };

        canvas.DrawText($"{assembly.PartNumber} Rev {assembly.Revision}", 10, 20, titlePaint);
        canvas.DrawText($"OD: {assembly.OverallDiameter:F2}mm | {assembly.TotalConductorCount} conductors", 10, 36, textPaint);
        canvas.DrawText("Isometric Cross-Section View", 10, height - 10, textPaint);
    }

    private static SKColor GetColor(string colorName)
    {
        if (ColorMap.TryGetValue(colorName, out var color))
            return color;

        return SKColors.Gray;
    }
}
