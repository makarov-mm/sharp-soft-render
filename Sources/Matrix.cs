namespace Renderer;

public sealed class Matrix
{
    private readonly double[,] _arr = new double[Size, Size];
    private const int Size = 4;

    public static Matrix CreateIdentity()
    {
        var matrix = new Matrix();

        for (int i = 0; i < Size; ++i)
        {
            matrix._arr[i, i] = 1.0;
        }

        return matrix;
    }

    public static Matrix CreateTranslate(double x, double y, double z)
    {
        var matrix = CreateIdentity();

        matrix[0, Size - 1] = x;
        matrix[1, Size - 1] = y;
        matrix[2, Size - 1] = z;

        return matrix;
    }

    public static Matrix CreateRotateX(double angle)
    {
        var matrix = CreateIdentity();

        angle = angle.DegToRad();
        double angleSin = Math.Sin(angle);
        double angleCos = Math.Cos(angle);

        matrix[1, 1] = angleCos;
        matrix[1, 2] = angleSin;
        matrix[2, 1] = -angleSin;
        matrix[2, 2] = angleCos;

        return matrix;
    }

    public static Matrix CreateRotateY(double angle)
    {
        var matrix = CreateIdentity();

        angle = angle.DegToRad();
        double angleSin = Math.Sin(angle);
        double angleCos = Math.Cos(angle);

        matrix[0, 0] = angleCos;
        matrix[0, 2] = -angleSin;
        matrix[2, 0] = angleSin;
        matrix[2, 2] = angleCos;

        return matrix;
    }

    public static Matrix CreateRotateZ(double angle)
    {
        var matrix = CreateIdentity();

        angle = angle.DegToRad();
        double angleSin = Math.Sin(angle);
        double angleCos = Math.Cos(angle);

        matrix[0, 0] = angleCos;
        matrix[0, 1] = angleSin;
        matrix[1, 0] = -angleSin;
        matrix[1, 1] = angleCos;

        return matrix;
    }

    public static Matrix CreateScale(double x, double y, double z)
    {
        var matrix = CreateIdentity();

        matrix[0, 0] = x;
        matrix[1, 1] = y;
        matrix[2, 2] = z;

        return matrix;
    }

    private Matrix Multiply(Matrix matrix)
    {
        var result = new Matrix();

        for (int j = 0; j < Size; ++j)
        {
            for (int i = 0; i < Size; ++i)
            {
                for (int k = 0; k < Size; ++k)
                {
                    result[i, j] += this[k, j] * matrix[i, k];
                }
            }
        }

        return result;
    }

    public static Matrix operator *(Matrix mx0, Matrix mx1)
    {
        return mx0.Multiply(mx1);
    }

    public double this[int i, int j]
    {
        get => _arr[i, j];
        set => _arr[i, j] = value;
    }
}
