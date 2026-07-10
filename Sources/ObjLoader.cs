using System.Globalization;

namespace Renderer;

/// <summary>
/// Wavefront OBJ loader: v / vt / vn / f (with fan triangulation and negative
/// indices) plus a minimal MTL parser that picks up the diffuse map (map_Kd).
/// If the file has no normals, smooth vertex normals are computed.
/// </summary>
public sealed class ObjLoader : ILoader
{
    public IReadOnlyList<string> Extensions { get; } = new[] { ".obj" };

    public VertexBuffer Load(string fileName)
    {
        var vb = new VertexBuffer();

        var positions = new List<Vector>();
        var texCoords = new List<(double U, double V)>();
        var normals = new List<Vector>();

        bool fileHasNormals = false;
        string texturePath = null;
        var materialTextures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string baseDir = Path.GetDirectoryName(Path.GetFullPath(fileName)) ?? string.Empty;

        foreach (string rawLine in File.ReadLines(fileName))
        {
            string line = StripComment(rawLine).Trim();

            if (line.Length == 0)
                continue;

            string[] parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

            switch (parts[0].ToLowerInvariant())
            {
                case "v":
                    // Flip Y: OBJ is Y-up, screen space is Y-down.
                    positions.Add(new Vector(
                        ParseDouble(parts[1]),
                        -ParseDouble(parts[2]),
                        ParseDouble(parts[3])));
                    break;

                case "vt":
                    texCoords.Add((
                        ParseDouble(parts[1]),
                        parts.Length > 2 ? ParseDouble(parts[2]) : 0));
                    break;

                case "vn":
                    normals.Add(new Vector(
                        ParseDouble(parts[1]),
                        -ParseDouble(parts[2]),
                        ParseDouble(parts[3])));
                    break;

                case "f":
                {
                    // Fan triangulation: (0, i, i + 1) for any polygon size.
                    for (int i = 2; i < parts.Length - 1; ++i)
                    {
                        AddFaceVertex(vb, parts[1], positions, texCoords, normals, ref fileHasNormals);
                        AddFaceVertex(vb, parts[i], positions, texCoords, normals, ref fileHasNormals);
                        AddFaceVertex(vb, parts[i + 1], positions, texCoords, normals, ref fileHasNormals);
                    }

                    break;
                }

                case "mtllib":
                    LoadMaterials(Path.Combine(baseDir, line.Substring(parts[0].Length).Trim()), materialTextures);
                    break;

                case "usemtl":
                    if (texturePath == null &&
                        materialTextures.TryGetValue(parts.Length > 1 ? parts[1] : string.Empty, out string mapKd))
                    {
                        texturePath = Path.IsPathRooted(mapKd) ? mapKd : Path.Combine(baseDir, mapKd);
                    }

                    break;
            }
        }

        vb.CenterCoordsToZero();

        if (fileHasNormals)
        {
            vb.HasNormals = true;
        }
        else
        {
            vb.CalcSmoothNormals();
        }

        if (texturePath != null && File.Exists(texturePath))
        {
            vb.Texture = TextureRegistry.LoadFromFile(texturePath);
        }

        return vb;
    }

    private static void AddFaceVertex(
        VertexBuffer vb,
        string token,
        List<Vector> positions,
        List<(double U, double V)> texCoords,
        List<Vector> normals,
        ref bool fileHasNormals)
    {
        // Token forms: "v", "v/vt", "v//vn", "v/vt/vn". Indices are 1-based;
        // negative values count back from the end of the respective list.
        string[] refs = token.Split('/');

        Vector position = positions[ResolveIndex(refs[0], positions.Count)];

        double u = 0, v = 0;

        if (refs.Length > 1 && refs[1].Length > 0)
        {
            (u, v) = texCoords[ResolveIndex(refs[1], texCoords.Count)];
        }

        var normal = new Vector();

        if (refs.Length > 2 && refs[2].Length > 0)
        {
            normal = normals[ResolveIndex(refs[2], normals.Count)];
            fileHasNormals = true;
        }

        vb.Add(new Vertex(position, normal, u, v));
    }

    private static int ResolveIndex(string token, int count)
    {
        int index = int.Parse(token, CultureInfo.InvariantCulture);
        return index > 0 ? index - 1 : count + index;
    }

    private static void LoadMaterials(string mtlPath, Dictionary<string, string> materialTextures)
    {
        if (!File.Exists(mtlPath))
            return;

        string currentMaterial = null;

        foreach (string rawLine in File.ReadLines(mtlPath))
        {
            string line = StripComment(rawLine).Trim();

            if (line.Length == 0)
                continue;

            string[] parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

            switch (parts[0].ToLowerInvariant())
            {
                case "newmtl":
                    currentMaterial = parts.Length > 1 ? parts[1] : null;
                    break;

                case "map_kd":
                    if (currentMaterial != null)
                    {
                        // The texture file name may contain spaces; options (-o, -s, ...)
                        // are not supported, take everything after the keyword.
                        materialTextures[currentMaterial] = line.Substring(parts[0].Length).Trim();
                    }

                    break;
            }
        }
    }

    private static string StripComment(string line)
    {
        int hash = line.IndexOf('#');
        return hash < 0 ? line : line.Substring(0, hash);
    }

    private static double ParseDouble(string s)
    {
        return double.Parse(s, CultureInfo.InvariantCulture);
    }
}
