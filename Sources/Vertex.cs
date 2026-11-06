using System.Numerics;

namespace Renderer;

/// <summary>A mesh vertex: position, unit normal and texture coordinates.</summary>
public record struct Vertex(Vector3 Position, Vector3 Normal, Vector2 Uv)
{
    public Vertex(Vector3 position) : this(position, Vector3.Zero, Vector2.Zero)
    {
    }

    /// <summary>
    /// Transforms the position as a point and the normal as a direction
    /// (translation ignored, renormalized afterwards).
    /// </summary>
    public readonly Vertex Transform(in Matrix4x4 matrix) => new(
        Vector3.Transform(Position, matrix),
        VectorMath.NormalizeOrZero(Vector3.TransformNormal(Normal, matrix)),
        Uv);
}

public static class VectorMath
{
    /// <summary>Unit vector in the same direction, or zero for a zero vector (no NaNs).</summary>
    public static Vector3 NormalizeOrZero(Vector3 v)
    {
        float lengthSquared = v.LengthSquared();
        return lengthSquared > 0f ? v / MathF.Sqrt(lengthSquared) : Vector3.Zero;
    }
}
