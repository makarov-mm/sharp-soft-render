using System.Globalization;

namespace Renderer;

public sealed class DxfLoader: ILoader
{
    public IReadOnlyList<string> Extensions { get; } = new[] { ".dxf" };

    public VertexBuffer Load(string fileName)
    {
        var vb = new VertexBuffer();

        string[] lines = File.ReadAllLines(fileName);
        var currentSection = Sections.None;
        var entityType = Entities.None;

        int primitiveType = 0;
        int coordsCount = 0;
        var coords = new List<float>();

        var indexes = new List<int>();
        var vertices = new List<Vector>();
        int verticesCount = 0;

        int indexesCount = 0;

        float a10 = 0, a20 = 0, a30 = 0,
            a11 = 0, a21 = 0, a31 = 0,
            a12 = 0, a22 = 0, a32 = 0,
            a13 = 0, a23 = 0, a33 = 0;

        for (int i = 0; i < lines.Length - 1; i += 2)
        {
            int code = int.Parse(lines[i]);
            string value = lines[i + 1].ToLowerInvariant();

            if (currentSection == Sections.None)
            {
                if (code == 0 && value == "section")
                {
                    int j = i + 2;
                    code = int.Parse(lines[j]);
                    value = lines[j + 1].ToLowerInvariant();

                    if (code == 2)
                    {
                        switch (value)
                        {
                            case "header":
                                currentSection = Sections.Header;
                                break;

                            case "tables":
                                currentSection = Sections.Tables;
                                break;

                            case "blocks":
                                currentSection = Sections.Blocks;
                                break;

                            case "entities":
                                currentSection = Sections.Entities;
                                break;
                        }

                        i += 2;
                    }
                }
            }
            else
            {
                if (code == 0 && value == "endsec")
                    currentSection = Sections.None;
                else
                {
                    if (currentSection == Sections.Entities)
                    {
                        if (code == 0)
                        {
                            switch (value)
                            {
                                case "line":
                                    entityType = Entities.Line;
                                    break;

                                case "insert":
                                    entityType = Entities.Insert;
                                    break;

                                case "mesh":
                                    entityType = Entities.Mesh;
                                    break;

                                case "3dface":
                                    entityType = Entities.Mesh;
                                    break;
                            }

                            int j = i + 2;
                            code = int.Parse(lines[j]);

                            if (code == 5)
                            {
                                i += 2;
                            }
                        }
                        else
                        {
                            if (entityType == Entities.Mesh)
                            {
                                if (code == 10 && verticesCount > 0)
                                    coords.Add(float.Parse(value, CultureInfo.InvariantCulture));
                                else if (code == 20 && verticesCount > 0)
                                    coords.Add(float.Parse(value, CultureInfo.InvariantCulture));
                                else if (code == 30 && verticesCount > 0)
                                {
                                    coords.Add(float.Parse(value, CultureInfo.InvariantCulture));
                                    --verticesCount;
                                    vertices.Add(new Vector(coords[0], coords[2], coords[1]));
                                    coords.Clear();
                                }
                                else if (code == 92)
                                    verticesCount = int.Parse(value, CultureInfo.InvariantCulture);
                                else if (code == 93)
                                    indexesCount = int.Parse(value, CultureInfo.InvariantCulture);
                                else if (code == 90 && indexesCount > 0)
                                {
                                    if (coordsCount == 0)
                                    {
                                        coordsCount = int.Parse(value, CultureInfo.InvariantCulture);
                                        --indexesCount;
                                        primitiveType = coordsCount;
                                    }
                                    else
                                    {
                                        indexes.Add(int.Parse(value, CultureInfo.InvariantCulture));
                                        --indexesCount;
                                        --coordsCount;

                                        if (coordsCount == 0)
                                        {
                                            if (primitiveType == 3)
                                            {
                                                vb.Add(new Vertex(vertices[indexes[0]]));
                                                vb.Add(new Vertex(vertices[indexes[1]]));
                                                vb.Add(new Vertex(vertices[indexes[2]]));
                                            }
                                            else if (primitiveType == 4)
                                            {
                                                vb.Add(new Vertex(vertices[indexes[0]]));
                                                vb.Add(new Vertex(vertices[indexes[1]]));
                                                vb.Add(new Vertex(vertices[indexes[2]]));

                                                vb.Add(new Vertex(vertices[indexes[0]]));
                                                vb.Add(new Vertex(vertices[indexes[2]]));
                                                vb.Add(new Vertex(vertices[indexes[3]]));
                                            }
                                            else
                                            {
                                                //TODO: ?
                                            }

                                            indexes.Clear();
                                        }
                                    }
                                }
                            }
                            else if (entityType == Entities.Face3D)
                            {
                                if (code == 10)
                                    a10 = float.Parse(value, CultureInfo.InvariantCulture);
                                else if (code == 20)
                                    a20 = float.Parse(value, CultureInfo.InvariantCulture);
                                else if (code == 30)
                                {
                                    a30 = float.Parse(value, CultureInfo.InvariantCulture);
                                    vb.Add(new Vertex(new Vector(a10, a20, a30)));
                                }
                                else if (code == 11)
                                    a11 = float.Parse(value, CultureInfo.InvariantCulture);
                                else if (code == 21)
                                    a21 = float.Parse(value, CultureInfo.InvariantCulture);
                                else if (code == 31)
                                {
                                    a31 = float.Parse(value, CultureInfo.InvariantCulture);
                                    vb.Add(new Vertex(new Vector(a11, a21, a31)));
                                }
                                else if (code == 12)
                                    a12 = float.Parse(value, CultureInfo.InvariantCulture);
                                else if (code == 22)
                                    a22 = float.Parse(value, CultureInfo.InvariantCulture);
                                else if (code == 32)
                                {
                                    a32 = float.Parse(value, CultureInfo.InvariantCulture);
                                    vb.Add(new Vertex(new Vector(a12, a22, a32)));
                                }
                                else if (code == 13)
                                    a13 = float.Parse(value, CultureInfo.InvariantCulture);
                                else if (code == 23)
                                    a23 = float.Parse(value, CultureInfo.InvariantCulture);
                                else if (code == 33)
                                {
                                    a33 = float.Parse(value, CultureInfo.InvariantCulture);
                                    vb.Add(new Vertex(new Vector(a12, a22, a32)));
                                    vb.Add(new Vertex(new Vector(a13, a23, a33)));
                                    vb.Add(new Vertex(new Vector(a10, a20, a30)));
                                }
                            }
                        }
                    }
                }
            }
        }

        vb.CenterCoordsToZero();
        vb.SortByDistance();
        vb.CalcTrianglesNormals();
        return vb;
    }

    private enum Sections
    {
        None,
        Header,
        Tables,
        Blocks,
        Entities
    }

    private enum Entities
    {
        None,
        Line,
        Insert,
        Mesh,
        Face3D
    }
}
