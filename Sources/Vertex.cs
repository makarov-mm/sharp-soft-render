namespace Renderer;

public sealed class Vertex(Vector coords, Vector normal, double u, double v)
{
    public Vector Coords = coords;
    public Vector Normal = normal;
    public double U = u, V = v;

    public Vertex(Vector coords) : this(coords, new Vector(), 0, 0)
    {
    }

    public Vertex(Vector coords, Vector normal) : this(coords, normal, 0, 0)
    {
    }

    public Vertex Multiply(Matrix matrix)
    {
        return new Vertex(
            Coords * matrix,
            Normal.MultiplyDirection(matrix).Normalize(),
            U, V);
    }

    public static Vertex operator* (Vertex vx, Matrix matrix)
    {
        return vx.Multiply(matrix);
    }
}
