using System.Globalization;
using System.Numerics;

namespace Renderer;

/// <summary>
/// DXF loader for the two mesh-carrying entities of the ENTITIES section:
/// MESH (AcDbSubDMesh: a vertex table plus an n-gon face list) and 3DFACE.
/// DXF is Z-up; the mesh is brought into the renderer's Y-down screen space
/// by swapping Y and Z. Faces are flat shaded.
/// </summary>
public sealed class DxfLoader : IModelLoader
{
    public IReadOnlyList<string> Extensions { get; } = [".dxf"];

    // A DXF file is a flat list of (group code, value) pairs, one per two lines.
    private readonly record struct Group(int Code, string Value);

    public Mesh Load(string fileName)
    {
        List<Group> groups = ReadGroups(fileName);
        var mesh = new Mesh();
        bool inEntities = false;

        for (int i = 0; i < groups.Count; i++)
        {
            if (groups[i].Code != 0) continue;   // only entity / section markers matter here

            switch (groups[i].Value.ToUpperInvariant())
            {
            case "SECTION":
                inEntities = i + 1 < groups.Count && groups[i + 1].Code == 2 &&
                             groups[i + 1].Value.Equals("ENTITIES", StringComparison.OrdinalIgnoreCase);
                break;
            case "ENDSEC":
                inEntities = false;
                break;
            case "MESH" when inEntities:
                ReadMesh(groups, i + 1, mesh);
                break;
            case "3DFACE" when inEntities:
                ReadFace(groups, i + 1, mesh);
                break;
            }
        }

        mesh.CenterOnOrigin();
        mesh.ComputeFlatNormals();
        return mesh;
    }

    private static List<Group> ReadGroups(string fileName)
    {
        var groups = new List<Group>();
        using var reader = new StreamReader(fileName);
        while (reader.ReadLine() is { } codeLine && reader.ReadLine() is { } value)
        {
            if (!int.TryParse(codeLine, NumberStyles.Integer, CultureInfo.InvariantCulture, out int code))
                throw new InvalidDataException($"'{Path.GetFileName(fileName)}' is not a DXF file: bad group code '{codeLine}'.");
            groups.Add(new Group(code, value.Trim()));
        }
        return groups;
    }

    // MESH: 92 = vertex count, then 10/20/30 per vertex; 93 = length of the
    // face list, then that many 90 values: [corner count, indices...] per face.
    // The 90 values after the list (edges, creases) are not faces.
    private static void ReadMesh(List<Group> groups, int start, Mesh mesh)
    {
        var positions = new List<Vector3>();
        var faceList = new List<int>();
        int verticesLeft = 0, faceValuesLeft = 0;
        float x = 0f, y = 0f;

        for (int i = start; i < groups.Count && groups[i].Code != 0; i++)
        {
            (int code, string value) = groups[i];
            switch (code)
            {
            case 92: verticesLeft = ParseInt(value); break;
            case 93: faceValuesLeft = ParseInt(value); break;
            case 10 when verticesLeft > 0: x = ParseFloat(value); break;
            case 20 when verticesLeft > 0: y = ParseFloat(value); break;
            case 30 when verticesLeft > 0:
                positions.Add(ToScreen(x, y, ParseFloat(value)));
                verticesLeft--;
                break;
            case 90 when faceValuesLeft > 0:
                faceList.Add(ParseInt(value));
                faceValuesLeft--;
                break;
            }
        }

        for (int k = 0; k < faceList.Count;)
        {
            int corners = faceList[k++];
            if (corners < 3 || k + corners > faceList.Count) break;   // malformed list
            if (faceList.Skip(k).Take(corners).Any(index => index < 0 || index >= positions.Count))
            {
                k += corners;
                continue;
            }
            for (int t = 1; t + 1 < corners; t++)   // fan triangulation of the n-gon
            {
                mesh.AddTriangle(
                    new Vertex(positions[faceList[k]]),
                    new Vertex(positions[faceList[k + t]]),
                    new Vertex(positions[faceList[k + t + 1]]));
            }
            k += corners;
        }
    }

    // 3DFACE: corners 10/20/30, 11/21/31, 12/22/32, 13/23/33; a triangle
    // repeats the third corner as the fourth.
    private static void ReadFace(List<Group> groups, int start, Mesh mesh)
    {
        Span<Vector3> corners = stackalloc Vector3[4];
        for (int i = start; i < groups.Count && groups[i].Code != 0; i++)
        {
            (int code, string value) = groups[i];
            int corner = code % 10;
            if (corner > 3) continue;
            switch (code - corner)
            {
            case 10: corners[corner].X = ParseFloat(value); break;
            case 20: corners[corner].Y = ParseFloat(value); break;
            case 30: corners[corner].Z = ParseFloat(value); break;
            }
        }

        Vector3 p0 = ToScreen(corners[0]), p1 = ToScreen(corners[1]), p2 = ToScreen(corners[2]), p3 = ToScreen(corners[3]);
        mesh.AddTriangle(new Vertex(p0), new Vertex(p1), new Vertex(p2));
        if (p3 != p2) mesh.AddTriangle(new Vertex(p0), new Vertex(p2), new Vertex(p3));
    }

    private static Vector3 ToScreen(Vector3 dxf) => ToScreen(dxf.X, dxf.Y, dxf.Z);
    private static Vector3 ToScreen(float x, float y, float z) => new(x, z, y);

    private static int ParseInt(string s) => int.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture);
    private static float ParseFloat(string s) => float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
}
