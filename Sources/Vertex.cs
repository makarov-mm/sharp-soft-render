namespace Renderer;

public sealed class Vertex(Vector coords, Vector normal)
{
    public Vector Coords = coords;
    public Vector Normal = normal;

    public Vertex(Vector coords) : this(coords, new Vector())
    {
    }

    public Vertex Multiply(Matrix matrix)
    {
        return new Vertex(
            Coords * matrix,
            (Normal * matrix).Normalize());
    }

    public static Vertex operator* (Vertex vx, Matrix matrix)
    {
        return vx.Multiply(matrix);
    }
}
