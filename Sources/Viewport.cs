using System.Drawing;
using System.Numerics;

namespace Renderer;

/// <summary>
/// A model, its orientation and zoom, and how they are drawn into a
/// <see cref="RenderTarget"/>: the model fitted to the target, lit, with a
/// flat drop shadow behind it. Used for the main view and the thumbnails.
/// </summary>
public sealed class Viewport
{
    public const int MinZoom = 2, MaxZoom = 20, DefaultZoom = 10;   // tenths

    /// <summary>
    /// Directional light in screen space: from the upper left towards the
    /// viewer (+Z is closer to the camera in this pipeline).
    /// </summary>
    public static readonly Vector3 LightDirection = Vector3.Normalize(new Vector3(-0.35f, -0.5f, 0.85f));

    private static readonly Color Background = Color.Azure;
    private static readonly Color ModelColor = Color.GhostWhite;
    private static readonly Color ShadowColor = Color.Gray;

    private Vertex[] _scratch = [];

    public Mesh? Model { get; set; }

    /// <summary>Model orientation; starts tilted so the three axes are all visible.</summary>
    public Matrix4x4 Rotation { get; set; } = InitialRotation;

    /// <summary>Scale in tenths: 10 fits the model to the target.</summary>
    public int Zoom { get; set; } = DefaultZoom;

    public bool DrawShadow { get; set; } = true;

    public static Matrix4x4 InitialRotation => RotationX(30f) * RotationY(30f);

    // Screen space is Y-down, so to a viewer the right-handed rotations of
    // System.Numerics run the other way round; these helpers take the angle
    // in degrees as seen on screen.
    public static Matrix4x4 RotationX(float degrees) => Matrix4x4.CreateRotationX(-float.DegreesToRadians(degrees));
    public static Matrix4x4 RotationY(float degrees) => Matrix4x4.CreateRotationY(-float.DegreesToRadians(degrees));

    public void Render(RenderTarget target)
    {
        target.Clear(Background);
        if (Model is not { VertexCount: > 0 } model) return;

        Vector3 size = model.Size;
        float extent = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
        if (extent <= 0f) return;
        float scale = Math.Min(target.Width, target.Height) / 2f / extent * (Zoom / 10f);
        var centre = new Vector3(target.Width / 2f, target.Height / 2f, 0f);

        if (_scratch.Length < model.VertexCount) _scratch = new Vertex[model.VertexCount];
        Span<Vertex> vertices = _scratch.AsSpan(0, model.VertexCount);

        if (DrawShadow)
        {
            // the model flattened onto the screen plane, pushed back and
            // offset down-left, as if lit from the upper right
            float offset = 100f * (Zoom / 10f);
            Matrix4x4 shadow = Matrix4x4.CreateScale(scale) * Rotation * Matrix4x4.CreateScale(1f, 1f, 0f) *
                               Matrix4x4.CreateTranslation(centre + new Vector3(-offset, offset, -1000f));
            model.Transform(shadow, vertices);
            Rasterizer.Draw(target, vertices, Shading.Unlit(ShadowColor));
        }

        Matrix4x4 world = Matrix4x4.CreateScale(scale) * Rotation * Matrix4x4.CreateTranslation(centre);
        model.Transform(world, vertices);
        Rasterizer.Draw(target, vertices, Shading.Lit(ModelColor, model.Texture, LightDirection));
    }
}
