using CableConcentricityCalculator.Models;
using CableConcentricityCalculator.Services;
using System.Text;

namespace CableConcentricityCalculator.Visualization;

/// <summary>
/// Generates STL (Stereolithography) 3D models of cable assemblies for proper 3D visualization
/// </summary>
public static class Cable3DSTLVisualizer
{
    /// <summary>
    /// Generate an STL file representing the cable assembly
    /// </summary>
    public static byte[] GenerateSTL(CableAssembly assembly)
    {
        var triangles = new List<Triangle>();

        // Generate cable geometry
        GenerateCableGeometry(assembly, triangles);

        // Convert triangles to STL format
        return TrianglesToSTL(triangles, assembly.PartNumber ?? "Cable");
    }

    private static void GenerateCableGeometry(CableAssembly assembly, List<Triangle> triangles)
    {
        const float cableLength = 100f; // 100mm length
        const int lengthSegments = 20;
        const int circumferenceSegments = 16;

        // Draw each layer as a tube with spiral cables
        foreach (var layer in assembly.Layers.OrderBy(l => l.LayerNumber))
        {
            if (layer.Cables.Count == 0) continue;

            var elements = layer.GetElements();
            var baseAngles = ConcentricityCalculator.CalculateAngularPositions(elements.Count);
            float pitchRadius = (float)ConcentricityCalculator.CalculateLayerPitchRadius(assembly, layer.LayerNumber);
            float twistDirection = layer.TwistDirection == TwistDirection.RightHand ? 1f : -1f;
            float layLength = (float)layer.LayLength;
            if (layLength <= 0) layLength = 50f;

            // Draw each cable in this layer as a helix
            for (int e = 0; e < elements.Count; e++)
            {
                var element = elements[e];
                float cableRadius = (float)element.Diameter / 2;

                // Create cylinder for this cable spiraling around the bundle
                CreateSpiralCable(triangles, pitchRadius, cableRadius, cableLength, 
                    (float)baseAngles[e], twistDirection, layLength, lengthSegments, circumferenceSegments);
            }
        }
    }

    private static void CreateSpiralCable(List<Triangle> triangles, 
        float spiralRadius, float cableRadius, float length, 
        float startAngle, float twistDir, float twistPitch,
        int lengthSegments, int circumSegments)
    {
        // Calculate twist rate
        float rotationPerLength = (2 * MathF.PI) / twistPitch;

        // Create vertices along the spiral helix
        var vertices = new List<Vector3>[lengthSegments + 1];

        for (int i = 0; i <= lengthSegments; i++)
        {
            float z = (float)i / lengthSegments * length;
            float angle = startAngle + twistDir * rotationPerLength * z;

            // Center of cable at this position
            float cx = spiralRadius * MathF.Cos(angle);
            float cy = spiralRadius * MathF.Sin(angle);

            // Create circle of vertices around this center
            vertices[i] = new List<Vector3>();
            for (int j = 0; j < circumSegments; j++)
            {
                float circumAngle = (float)j / circumSegments * 2 * MathF.PI;
                float x = cx + cableRadius * MathF.Cos(circumAngle) * MathF.Cos(angle);
                float y = cy + cableRadius * MathF.Cos(circumAngle) * MathF.Sin(angle);
                float xyRadius = cableRadius * MathF.Sin(circumAngle);
                
                vertices[i].Add(new Vector3(x, y, z + xyRadius * 0.1f)); // slight z offset for twist visualization
            }
        }

        // Create triangles from vertex rings
        for (int i = 0; i < lengthSegments; i++)
        {
            var ring1 = vertices[i];
            var ring2 = vertices[i + 1];

            for (int j = 0; j < circumSegments; j++)
            {
                int j1 = j;
                int j2 = (j + 1) % circumSegments;

                // First triangle
                var v1 = ring1[j1];
                var v2 = ring1[j2];
                var v3 = ring2[j1];

                triangles.Add(new Triangle(v1, v2, v3));

                // Second triangle
                var v4 = ring2[j1];
                var v5 = ring1[j2];
                var v6 = ring2[j2];

                triangles.Add(new Triangle(v4, v5, v6));
            }
        }
    }

    private static byte[] TrianglesToSTL(List<Triangle> triangles, string name)
    {
        var sb = new StringBuilder();
        
        // ASCII STL format
        sb.AppendLine($"solid {name}");

        foreach (var triangle in triangles)
        {
            var normal = triangle.CalculateNormal();
            sb.AppendLine($"  facet normal {normal.X:E6} {normal.Y:E6} {normal.Z:E6}");
            sb.AppendLine($"    outer loop");
            sb.AppendLine($"      vertex {triangle.V1.X:E6} {triangle.V1.Y:E6} {triangle.V1.Z:E6}");
            sb.AppendLine($"      vertex {triangle.V2.X:E6} {triangle.V2.Y:E6} {triangle.V2.Z:E6}");
            sb.AppendLine($"      vertex {triangle.V3.X:E6} {triangle.V3.Y:E6} {triangle.V3.Z:E6}");
            sb.AppendLine($"    endloop");
            sb.AppendLine($"  endfacet");
        }

        sb.AppendLine($"endsolid {name}");

        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private class Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3 operator -(Vector3 a, Vector3 b) =>
            new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static Vector3 Cross(Vector3 a, Vector3 b) =>
            new Vector3(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X);

        public float Length => MathF.Sqrt(X * X + Y * Y + Z * Z);

        public Vector3 Normalize()
        {
            float len = Length;
            if (len == 0) return new Vector3(0, 0, 1);
            return new Vector3(X / len, Y / len, Z / len);
        }
    }

    private class Triangle
    {
        public Vector3 V1 { get; set; }
        public Vector3 V2 { get; set; }
        public Vector3 V3 { get; set; }

        public Triangle(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            V1 = v1;
            V2 = v2;
            V3 = v3;
        }

        public Vector3 CalculateNormal()
        {
            var edge1 = V2 - V1;
            var edge2 = V3 - V1;
            return Vector3.Cross(edge1, edge2).Normalize();
        }
    }
}
