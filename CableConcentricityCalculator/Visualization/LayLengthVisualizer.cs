using SkiaSharp;
using CableConcentricityCalculator.Models;

namespace CableConcentricityCalculator.Visualization;

/// <summary>
/// Generates side-view diagrams showing cable lay length (twist pitch)
/// </summary>
public static class LayLengthVisualizer
{
    /// <summary>
    /// Generate a side-view diagram showing lay length for a cable layer
    /// </summary>
    public static byte[] GenerateLayLengthDiagram(CableLayer layer, int width = 600, int height = 200)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        // Calculate parameters
        float layLength = (float)layer.LayLength;
        float margin = 40;
        float diagramWidth = width - 2 * margin;
        float centerY = height / 2f;

        // Number of complete rotations to show
        int rotations = 3;
        float totalLength = layLength * rotations;
        float scale = diagramWidth / totalLength;

        // Draw the twisted cables
        DrawTwistedCables(canvas, layer, margin, centerY, scale, layLength, rotations);

        // Draw lay length annotation
        DrawLayLengthAnnotation(canvas, margin, centerY, scale, layLength);

        // Draw text label
        DrawTextLabel(canvas, width / 2f, 30, "Lay Length", layer.TwistDirection);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static void DrawTwistedCables(SKCanvas canvas, CableLayer layer, float startX, float centerY,
        float scale, float layLength, int rotations)
    {
        int cableCount = Math.Max(layer.Cables.Count, 3); // Show at least 3 cables
        float cableRadius = 6f;
        float bundleRadius = cableRadius * 2.5f; // Radius of the bundle circle

        // Create colors for different cables
        var colors = new[] {
            SKColors.Gray,
            SKColors.DarkGray,
            SKColors.LightGray,
            SKColors.Silver,
            SKColors.DimGray
        };

        // Draw each cable as a sinusoidal path
        for (int i = 0; i < Math.Min(cableCount, 5); i++)
        {
            float phaseOffset = (2f * MathF.PI / cableCount) * i;
            var paint = new SKPaint
            {
                Color = colors[i % colors.Length],
                StrokeWidth = cableRadius * 2,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };

            var path = new SKPath();
            bool isFirst = true;

            for (float x = 0; x <= layLength * scale * rotations; x += 2f)
            {
                float realX = x / scale; // Convert back to real distance
                float angle = (2f * MathF.PI * realX / layLength) + phaseOffset;

                // Adjust angle direction based on twist direction
                if (layer.TwistDirection == TwistDirection.LeftHand)
                    angle = -angle;

                float y = centerY + MathF.Sin(angle) * bundleRadius;
                float drawX = startX + x;

                if (isFirst)
                {
                    path.MoveTo(drawX, y);
                    isFirst = false;
                }
                else
                {
                    path.LineTo(drawX, y);
                }
            }

            canvas.DrawPath(path, paint);
        }

        // Add highlights to make it look more 3D
        for (int i = 0; i < Math.Min(cableCount, 5); i++)
        {
            float phaseOffset = (2f * MathF.PI / cableCount) * i;
            var highlightPaint = new SKPaint
            {
                Color = SKColors.White.WithAlpha(100),
                StrokeWidth = cableRadius,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };

            var path = new SKPath();
            bool isFirst = true;

            for (float x = 0; x <= layLength * scale * rotations; x += 2f)
            {
                float realX = x / scale;
                float angle = (2f * MathF.PI * realX / layLength) + phaseOffset;

                if (layer.TwistDirection == TwistDirection.LeftHand)
                    angle = -angle;

                float y = centerY + MathF.Sin(angle) * bundleRadius - cableRadius * 0.3f;
                float drawX = startX + x;

                if (isFirst)
                {
                    path.MoveTo(drawX, y);
                    isFirst = false;
                }
                else
                {
                    path.LineTo(drawX, y);
                }
            }

            canvas.DrawPath(path, highlightPaint);
        }
    }

    private static void DrawLayLengthAnnotation(SKCanvas canvas, float startX, float centerY,
        float scale, float layLength)
    {
        float annotationY = centerY + 50;
        float arrowStart = startX;
        float arrowEnd = startX + (layLength * scale);

        var arrowPaint = new SKPaint
        {
            Color = new SKColor(220, 53, 69), // Red color
            StrokeWidth = 2,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };

        var fillPaint = new SKPaint
        {
            Color = new SKColor(220, 53, 69),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        // Draw horizontal line
        canvas.DrawLine(arrowStart, annotationY, arrowEnd, annotationY, arrowPaint);

        // Draw left arrow
        DrawArrowHead(canvas, arrowStart, annotationY, true, fillPaint);

        // Draw right arrow
        DrawArrowHead(canvas, arrowEnd, annotationY, false, fillPaint);

        // Draw vertical ticks
        canvas.DrawLine(arrowStart, annotationY - 5, arrowStart, annotationY + 5, arrowPaint);
        canvas.DrawLine(arrowEnd, annotationY - 5, arrowEnd, annotationY + 5, arrowPaint);

        // Draw measurement text
        var textPaint = new SKPaint
        {
            Color = new SKColor(220, 53, 69),
            TextSize = 14,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };

        string text = $"{layLength:F1} mm";
        float textWidth = textPaint.MeasureText(text);
        float textX = (arrowStart + arrowEnd) / 2f - textWidth / 2f;
        canvas.DrawText(text, textX, annotationY + 20, textPaint);
    }

    private static void DrawArrowHead(SKCanvas canvas, float x, float y, bool pointsLeft, SKPaint paint)
    {
        float arrowSize = 8;
        var path = new SKPath();

        if (pointsLeft)
        {
            path.MoveTo(x, y);
            path.LineTo(x + arrowSize, y - arrowSize / 2);
            path.LineTo(x + arrowSize, y + arrowSize / 2);
        }
        else
        {
            path.MoveTo(x, y);
            path.LineTo(x - arrowSize, y - arrowSize / 2);
            path.LineTo(x - arrowSize, y + arrowSize / 2);
        }

        path.Close();
        canvas.DrawPath(path, paint);
    }

    private static void DrawTextLabel(SKCanvas canvas, float x, float y, string label, TwistDirection direction)
    {
        var textPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 16,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };

        float textWidth = textPaint.MeasureText(label);
        canvas.DrawText(label, x - textWidth / 2f, y, textPaint);

        // Add twist direction indicator
        string directionText = direction switch
        {
            TwistDirection.RightHand => "(Right-Hand / S-Twist)",
            TwistDirection.LeftHand => "(Left-Hand / Z-Twist)",
            _ => "(No Twist)"
        };

        var subtextPaint = new SKPaint
        {
            Color = SKColors.Gray,
            TextSize = 12,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial")
        };

        float subtextWidth = subtextPaint.MeasureText(directionText);
        canvas.DrawText(directionText, x - subtextWidth / 2f, y + 18, subtextPaint);
    }
}
