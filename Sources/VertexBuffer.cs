namespace Renderer;

public sealed class VertexBuffer
{
    private List<Vertex> _vertices;

    public VertexBuffer()
    {
        _vertices = new List<Vertex>();
    }

    public VertexBuffer(int capacity)
    {
        _vertices = new List<Vertex>(capacity);
    }

    public void Add(Vertex vx)
    {
        _vertices.Add(vx);
    }

    public VertexBuffer Multiply(Matrix matrix)
    {
        var vb = new VertexBuffer(_vertices.Count);

        foreach (Vertex vx in _vertices)
        {
            vb.Add(vx * matrix);
        }

        return vb;
    }

    public VertexBuffer Clone()
    {
        var vb = new VertexBuffer(_vertices.Count);
        vb._vertices.AddRange(_vertices);
        return vb;
    }

    public static VertexBuffer operator* (VertexBuffer vb, Matrix matrix)
    {
        return vb.Multiply(matrix);
    }

    public void DrawTriangles(IRenderTarget target, Color color, bool lighting)
    {
        var tmp = new List<Vertex>(3);

        foreach (Vertex vertex in _vertices)
        {
            tmp.Add(vertex);

            if (tmp.Count == 3)
            {
                var resultColor = lighting
                    ? Color.FromArgb(
                        255,
                        Math.Min(Math.Max((int)(Math.Abs(tmp[0].Normal.Z) * color.R), 0), 255),
                        Math.Min(Math.Max((int)(Math.Abs(tmp[0].Normal.Z) * color.G), 0), 255),
                        Math.Min(Math.Max((int)(Math.Abs(tmp[0].Normal.Z) * color.B), 0), 255))
                    : color;

                DrawTriangle(
                    target,
                    (int)tmp[0].Coords.X, (int)tmp[0].Coords.Y, (int)tmp[0].Coords.Z,
                    (int)tmp[1].Coords.X, (int)tmp[1].Coords.Y, (int)tmp[1].Coords.Z,
                    (int)tmp[2].Coords.X, (int)tmp[2].Coords.Y, (int)tmp[2].Coords.Z,
                    resultColor);

                tmp.Clear();
            }
        }
    }

    public void CalcTrianglesNormals()
    {
        for (int i = 0; i < _vertices.Count - 2; i += 3)
        {
            Vertex vx0 = _vertices[i];
            Vertex vx1 = _vertices[i + 1];
            Vertex vx2 = _vertices[i + 2];

            Vector coords0 = vx0.Coords;
            Vector coords1 = vx1.Coords;
            Vector coords2 = vx2.Coords;

            var vec1 = new Vector(
                coords0.X - coords1.X,
                coords0.Y - coords1.Y,
                coords0.Z - coords1.Z
            );

            var vec0 = new Vector(
                coords1.X - coords2.X,
                coords1.Y - coords2.Y,
                coords1.Z - coords2.Z
            );

            double wrki = Math.Sqrt(
                Math.Pow(vec0.Y * vec1.Z - vec0.Z * vec1.Y, 2) +
                Math.Pow(vec0.Z * vec1.X - vec0.X * vec1.Z, 2) +
                Math.Pow(vec0.X * vec1.Y - vec0.Y * vec1.X, 2)
            );

            var normal = new Vector(
                (vec0.Y * vec1.Z - vec0.Z * vec1.Y) / wrki,
                (vec0.Z * vec1.X - vec0.X * vec1.Z) / wrki,
                (vec0.X * vec1.Y - vec0.Y * vec1.X) / wrki
            );

            if (double.IsNaN(wrki) || double.IsInfinity(wrki) || Math.Abs(wrki) <= double.Epsilon)
            {
                normal = new Vector(0, 0, 0);
            }

            vx0.Normal = normal;
            vx1.Normal = normal;
            vx2.Normal = normal;

            _vertices[i] = vx0;
            _vertices[i + 1] = vx1;
            _vertices[i + 2] = vx2;
        }
    }

    public void SortByDistance()
    {
        bool repeat = true;

        while (repeat)
        {
            repeat = false;

            for (int i = 0; i < _vertices.Count - 6; i += 3)
            {
                double dist0 = _vertices[i].Coords.Z + _vertices[i + 1].Coords.Z + _vertices[i + 2].Coords.Z;
                double dist1 = _vertices[i + 3].Coords.Z + _vertices[i + 4].Coords.Z + _vertices[i + 5].Coords.Z;

                if (dist0 > dist1)
                {
                    Vertex tmp = _vertices[i];
                    _vertices[i] = _vertices[i + 3];
                    _vertices[i + 3] = tmp;

                    tmp = _vertices[i + 1];
                    _vertices[i + 1] = _vertices[i + 4];
                    _vertices[i + 4] = tmp;

                    tmp = _vertices[i + 2];
                    _vertices[i + 2] = _vertices[i + 5];
                    _vertices[i + 5] = tmp;

                    repeat = true;
                }
            }
        }
    }

    public double Width
    {
        get
        {
            double minValue = double.MaxValue;
            double maxValue = double.MinValue;

            foreach (Vertex vertex in _vertices)
            {
                maxValue = Math.Max(maxValue, vertex.Coords.X);
                minValue = Math.Min(minValue, vertex.Coords.X);
            }

            return Math.Max(maxValue - minValue, 0);
        }
    }

    public double Length
    {
        get
        {
            double minValue = double.MaxValue;
            double maxValue = double.MinValue;

            foreach (Vertex vertex in _vertices)
            {
                maxValue = Math.Max(maxValue, vertex.Coords.Z);
                minValue = Math.Min(minValue, vertex.Coords.Z);
            }

            return Math.Max(maxValue - minValue, 0);
        }
    }

    public double Height
    {
        get
        {
            double minValue = double.MaxValue;
            double maxValue = double.MinValue;

            foreach (Vertex vertex in _vertices)
            {
                maxValue = Math.Max(maxValue, vertex.Coords.Y);
                minValue = Math.Min(minValue, vertex.Coords.Y);
            }

            return Math.Max(maxValue - minValue, 0);
        }
    }

    public Vector Volume => new Vector(Width, Height, Length);

    public void CenterCoordsToZero()
    {
        var transformedVertices = new List<Vertex>();

        if (_vertices.Count > 0)
        {
            double minX = _vertices[0].Coords.X;
            double minY = _vertices[0].Coords.Y;
            double minZ = _vertices[0].Coords.Z;

            double maxX = _vertices[0].Coords.X;
            double maxY = _vertices[0].Coords.Y;
            double maxZ = _vertices[0].Coords.Z;

            foreach (Vertex vertex in _vertices)
            {
                minX = Math.Min(vertex.Coords.X, minX);
                maxX = Math.Max(vertex.Coords.X, maxX);

                minY = Math.Min(vertex.Coords.Y, minY);
                maxY = Math.Max(vertex.Coords.Y, maxY);

                minZ = Math.Min(vertex.Coords.Z, minZ);
                maxZ = Math.Max(vertex.Coords.Z, maxZ);
            }

            double xOffset = (maxX + minX) / 2.0;
            double yOffset = (maxY + minY) / 2.0;
            double zOffset = (maxZ + minZ) / 2.0;

            foreach (Vertex vertex in _vertices)
            {
                transformedVertices.Add(new Vertex(
                    new Vector(
                        vertex.Coords.X - xOffset,
                        vertex.Coords.Y - yOffset,
                        vertex.Coords.Z - zOffset),
                    vertex.Normal));
            }
        }

        _vertices = transformedVertices;
    }

    private Func<float, float, float> CreatePlan(
        float x1, float y1, float z1,
        float x2, float y2, float z2,
        float x3, float y3, float z3)
    {
        float y2y1 = y2 - y1;
        float y3y1 = y3 - y1;
		
        float z2z1 = z2 - z1;
        float z3z1 = z3 - z1;
		
        float x2x1 = x2 - x1;
        float x3x1 = x3 - x1;
		
        float e = y2y1 * x3x1 - x2x1 * y3y1;
        float ac = y2y1 * z3z1 - z2z1 * y3y1;
        float bd = z2z1 * x3x1 - z3z1 * x2x1;

        return (x, y) => z1 + ((x - x1) * ac + (y - y1) * bd) / e;
    }
	
    private void DrawTriangle(
        IRenderTarget renderTarget,
        int ax, int ay, int az,
        int bx, int by, int bz,
        int cx, int cy, int cz,
        Color color)
    {
        if (ay > cy)
        {
            (ay, cy) = (cy, ay);
            (ax, cx) = (cx, ax);
            (az, cz) = (cz, az);
        }
		
        if (ay > by)
        {
            (ay, by) = (by, ay);
            (ax, bx) = (bx, ax);
            (az, bz) = (bz, az);
        }
		
        if (cy < by)
        {
            (cy, by) = (by, cy);
            (cx, bx) = (bx, cx);
            (cz, bz) = (bz, cz);
        }
		
        Func<float, float, float> plan = CreatePlan(
            ax, ay, az,
            bx, by, bz,
            cx, cy, cz);
		
        float cxaxcyay = (cx - ax) / (float)(cy - ay);
        float bxaxbyay = (bx - ax) / (float)(by - ay);
        float cxbxcyby = (cx - bx) / (float)(cy - by);
		
        for (int y = ay; y < cy; ++y)
        {
            var x1 = (int)(ax + (y - ay) * cxaxcyay);
            int x2;

            if (y < by)
            {
                x2 = (int)(ax + (y - ay) * bxaxbyay);
            }
            else
            {
                if (cy == by)
                {
                    x2 = bx;
                }
                else
                {
                    x2 = (int)(bx + (y - by) * cxbxcyby);
                }
            }

            if (x1 > x2)
            {
                (x1, x2) = (x2, x1);
            }

            DrawHorizontalLine(renderTarget, y, x1, x2, color, plan);
        }
    }

    private void DrawHorizontalLine(IRenderTarget renderTarget, int y, int x1, int x2, Color color, Func<float, float, float> plan)
    {
        if (y >= 0 && y < renderTarget.Height)
        {
            if (x1 > x2)
            {
                (x1, x2) = (x2, x1);
            }

            if (x2 >= 0 && x1 < renderTarget.Width)
            {
                if (x1 < 0)
                {
                    x1 = 0;
                }

                if (x2 >= renderTarget.Width)
                {
                    x2 = renderTarget.Width - 1;
                }
				
                if (x1 <= x2)
                {
                    for (int x = x1; x < x2; ++x)
                    {
                        float z = plan(x, y);

                        if (z > renderTarget.GetDepth(x, y))
                        {
                            renderTarget[x, y] = color;
                            renderTarget.SetDepth(x, y, z);
                        }
                    }
                }
            }
        }
    }
}
