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

    public static Vector operator *(Vector vec, Matrix matrix)
    {
        return vec.Multiply(matrix);
    }

    public Vector Normalize()
    {
        double len = Math.Sqrt(
            Math.Pow(X, 2) +
            Math.Pow(Y, 2) +
            Math.Pow(Z, 2)
        );

        return new Vector(X / len, Y / len, Z / len);
    }
}
