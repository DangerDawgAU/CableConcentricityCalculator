using CableConcentricityCalculator.Models;
using CableConcentricityCalculator.Services;
using CableConcentricityCalculator.Utilities;
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
        if (ColorUtilities.ColorMapSK.TryGetValue(colorName, out var color))
            return color;

        return SKColors.Gray;
    }
}
