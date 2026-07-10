namespace Renderer;

public struct Vector(double x, double y, double z)
{
    public double X = x, Y = y, Z = z;

    public Vector Multiply(Matrix matrix)
    {
        double x =
            X * matrix[0, 0] +
            Y * matrix[0, 1] +
            Z * matrix[0, 2] +
            matrix[0, 3];

        double y =
            X * matrix[1, 0] +
            Y * matrix[1, 1] +
            Z * matrix[1, 2] +
            matrix[1, 3];

        double z =
            X * matrix[2, 0] +
            Y * matrix[2, 1] +
            Z * matrix[2, 2] +
            matrix[2, 3];

        double w =
            X * matrix[3, 0] +
            Y * matrix[3, 1] +
            Z * matrix[3, 2] +
            matrix[3, 3];

        return new Vector(x / w, y / w, z / w);
    }

    /// <summary>
    /// Transforms the vector as a direction: translation and perspective
    /// rows of the matrix are ignored. Use for normals and light directions.
    /// </summary>
    public Vector MultiplyDirection(Matrix matrix)
    {
        return new Vector(
            X * matrix[0, 0] + Y * matrix[0, 1] + Z * matrix[0, 2],
            X * matrix[1, 0] + Y * matrix[1, 1] + Z * matrix[1, 2],
            X * matrix[2, 0] + Y * matrix[2, 1] + Z * matrix[2, 2]);
    }

    public static Vector operator *(Vector vec, Matrix matrix)
    {
        return vec.Multiply(matrix);
    }

    public static Vector operator +(Vector a, Vector b)
    {
        return new Vector(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }

    public static Vector operator -(Vector a, Vector b)
    {
        return new Vector(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }

    public static Vector operator -(Vector a)
    {
        return new Vector(-a.X, -a.Y, -a.Z);
    }

    public static Vector operator *(Vector a, double k)
    {
        return new Vector(a.X * k, a.Y * k, a.Z * k);
    }

    public double Dot(Vector other)
    {
        return X * other.X + Y * other.Y + Z * other.Z;
    }

    public Vector Cross(Vector other)
    {
        return new Vector(
            Y * other.Z - Z * other.Y,
            Z * other.X - X * other.Z,
            X * other.Y - Y * other.X);
    }

    public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

    public Vector Normalize()
    {
        double len = Length;

        return len <= double.Epsilon
            ? new Vector(0, 0, 0)
            : new Vector(X / len, Y / len, Z / len);
    }
}
