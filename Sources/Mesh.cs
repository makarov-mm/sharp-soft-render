using System.Numerics;
using System.Runtime.InteropServices;

namespace Renderer;

/// <summary>
/// A triangle list (three consecutive vertices per triangle) with an optional
/// diffuse texture. Loaders centre the mesh on the origin and give every vertex
/// a normal, so the rasterizer never has to guess.
/// </summary>
public sealed class Mesh
{
    private readonly List<Vertex> _vertices;

    public Mesh(int capacity = 0)
    {
        _vertices = new List<Vertex>(capacity);
    }

    /// <summary>Diffuse texture of the model, if any.</summary>
    public Texture? Texture { get; set; }

    public int VertexCount => _vertices.Count;
    public int TriangleCount => _vertices.Count / 3;

    public ReadOnlySpan<Vertex> Vertices => CollectionsMarshal.AsSpan(_vertices);

    public void Add(Vertex vertex) => _vertices.Add(vertex);

    public void AddTriangle(Vertex a, Vertex b, Vertex c)
    {
        _vertices.Add(a);
        _vertices.Add(b);
        _vertices.Add(c);
    }

    /// <summary>Writes every vertex transformed by <paramref name="matrix"/> into <paramref name="destination"/>.</summary>
    public void Transform(in Matrix4x4 matrix, Span<Vertex> destination)
    {
        ReadOnlySpan<Vertex> source = Vertices;
        for (int i = 0; i < source.Length; i++)
            destination[i] = source[i].Transform(matrix);
    }

    /// <summary>Axis-aligned extents of the mesh (zero for an empty mesh).</summary>
    public Vector3 Size
    {
        get
        {
            (Vector3 min, Vector3 max) = Bounds();
            return max - min;
        }
    }

    /// <summary>Moves the mesh so that its bounding box is centred on the origin.</summary>
    public void CenterOnOrigin()
    {
        if (_vertices.Count == 0) return;
        (Vector3 min, Vector3 max) = Bounds();
        Vector3 offset = (min + max) * 0.5f;
        foreach (ref Vertex vertex in CollectionsMarshal.AsSpan(_vertices))
            vertex.Position -= offset;
    }

    /// <summary>Flat shading: every vertex gets the normal of its triangle.</summary>
    public void ComputeFlatNormals()
    {
        Span<Vertex> vertices = CollectionsMarshal.AsSpan(_vertices);
        for (int i = 0; i + 2 < vertices.Length; i += 3)
        {
            Vector3 normal = VectorMath.NormalizeOrZero(FaceNormal(vertices[i].Position, vertices[i + 1].Position, vertices[i + 2].Position));
            vertices[i].Normal = normal;
            vertices[i + 1].Normal = normal;
            vertices[i + 2].Normal = normal;
        }
    }

    /// <summary>
    /// Smooth shading: every vertex gets the average of the face normals of all
    /// triangles sharing its position, weighted by triangle area (the length of
    /// the unnormalized cross product), so large faces dominate.
    /// </summary>
    public void ComputeSmoothNormals()
    {
        Span<Vertex> vertices = CollectionsMarshal.AsSpan(_vertices);
        var sums = new Dictionary<(int, int, int), Vector3>();

        for (int i = 0; i + 2 < vertices.Length; i += 3)
        {
            Vector3 faceNormal = FaceNormal(vertices[i].Position, vertices[i + 1].Position, vertices[i + 2].Position);
            for (int j = 0; j < 3; j++)
            {
                ref Vector3 sum = ref CollectionsMarshal.GetValueRefOrAddDefault(sums, PositionKey(vertices[i + j].Position), out _);
                sum += faceNormal;
            }
        }

        foreach (ref Vertex vertex in vertices)
            vertex.Normal = VectorMath.NormalizeOrZero(sums[PositionKey(vertex.Position)]);
    }

    private static Vector3 FaceNormal(Vector3 a, Vector3 b, Vector3 c) => Vector3.Cross(b - a, c - a);

    // Positions within 1e-4 of each other count as the same vertex.
    private static (int, int, int) PositionKey(Vector3 p) =>
        ((int)MathF.Round(p.X * 10000f), (int)MathF.Round(p.Y * 10000f), (int)MathF.Round(p.Z * 10000f));

    private (Vector3 Min, Vector3 Max) Bounds()
    {
        if (_vertices.Count == 0) return (Vector3.Zero, Vector3.Zero);
        Vector3 min = new(float.MaxValue), max = new(float.MinValue);
        foreach (ref readonly Vertex vertex in Vertices)
        {
            min = Vector3.Min(min, vertex.Position);
            max = Vector3.Max(max, vertex.Position);
        }
        return (min, max);
    }
}
