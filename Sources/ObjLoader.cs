using System.Globalization;
using System.Numerics;

namespace Renderer;

/// <summary>
/// Wavefront OBJ loader: v / vt / vn / f (fan triangulation of n-gons, negative
/// indices) plus the minimal MTL parsing needed to find the diffuse map
/// (map_Kd). Files without normals get smooth vertex normals.
/// </summary>
public sealed class ObjLoader(Func<string, Texture?>? decodeTexture = null) : IModelLoader
{
    public IReadOnlyList<string> Extensions { get; } = [".obj"];

    public Mesh Load(string fileName)
    {
        var mesh = new Mesh();
        var positions = new List<Vector3>();
        var texCoords = new List<Vector2>();
        var normals = new List<Vector3>();
        var materialTextures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? texturePath = null;
        bool fileHasNormals = false;
        string baseDir = Path.GetDirectoryName(Path.GetFullPath(fileName)) ?? string.Empty;

        foreach (string rawLine in File.ReadLines(fileName))
        {
            string[] parts = Tokenize(rawLine);
            if (parts.Length == 0) continue;

            switch (parts[0].ToLowerInvariant())
            {
            case "v":   // OBJ is Y-up, screen space is Y-down
                positions.Add(new Vector3(Parse(parts[1]), -Parse(parts[2]), Parse(parts[3])));
                break;

            case "vt":
                texCoords.Add(new Vector2(Parse(parts[1]), parts.Length > 2 ? Parse(parts[2]) : 0f));
                break;

            case "vn":
                normals.Add(new Vector3(Parse(parts[1]), -Parse(parts[2]), Parse(parts[3])));
                break;

            case "f":   // fan triangulation: (0, i, i + 1) for any polygon size
                Vertex first = FaceVertex(parts[1], positions, texCoords, normals, ref fileHasNormals);
                for (int i = 2; i + 1 < parts.Length; i++)
                {
                    mesh.AddTriangle(
                        first,
                        FaceVertex(parts[i], positions, texCoords, normals, ref fileHasNormals),
                        FaceVertex(parts[i + 1], positions, texCoords, normals, ref fileHasNormals));
                }
                break;

            case "mtllib":
                LoadMaterials(Path.Combine(baseDir, RestOfLine(rawLine, parts[0])), materialTextures);
                break;

            case "usemtl":  // the first material with a map wins; one texture per mesh
                if (texturePath is null && parts.Length > 1 && materialTextures.TryGetValue(parts[1], out string? mapKd))
                    texturePath = Path.IsPathRooted(mapKd) ? mapKd : Path.Combine(baseDir, mapKd);
                break;
            }
        }

        mesh.CenterOnOrigin();
        if (!fileHasNormals) mesh.ComputeSmoothNormals();
        if (texturePath is not null && decodeTexture is not null && File.Exists(texturePath))
            mesh.Texture = decodeTexture(texturePath);
        return mesh;
    }

    // Token forms: "v", "v/vt", "v//vn", "v/vt/vn". Indices are 1-based;
    // negative values count back from the end of the respective list.
    private static Vertex FaceVertex(string token, List<Vector3> positions, List<Vector2> texCoords, List<Vector3> normals,
                                     ref bool fileHasNormals)
    {
        string[] refs = token.Split('/');
        Vector3 position = positions[ResolveIndex(refs[0], positions.Count)];
        Vector2 uv = refs.Length > 1 && refs[1].Length > 0 ? texCoords[ResolveIndex(refs[1], texCoords.Count)] : Vector2.Zero;
        Vector3 normal = Vector3.Zero;
        if (refs.Length > 2 && refs[2].Length > 0)
        {
            normal = normals[ResolveIndex(refs[2], normals.Count)];
            fileHasNormals = true;
        }
        return new Vertex(position, normal, uv);
    }

    private static int ResolveIndex(string token, int count)
    {
        int index = int.Parse(token, CultureInfo.InvariantCulture);
        return index > 0 ? index - 1 : count + index;
    }

    private static void LoadMaterials(string mtlPath, Dictionary<string, string> materialTextures)
    {
        if (!File.Exists(mtlPath)) return;

        string? material = null;
        foreach (string rawLine in File.ReadLines(mtlPath))
        {
            string[] parts = Tokenize(rawLine);
            if (parts.Length == 0) continue;

            switch (parts[0].ToLowerInvariant())
            {
            case "newmtl":
                material = parts.Length > 1 ? parts[1] : null;
                break;

            case "map_kd":  // the file name may contain spaces; map options (-o, -s, ...) are not supported
                if (material is not null) materialTextures[material] = RestOfLine(rawLine, parts[0]);
                break;
            }
        }
    }

    // The line without its comment, split on whitespace.
    private static string[] Tokenize(string line)
    {
        int hash = line.IndexOf('#');
        if (hash >= 0) line = line[..hash];
        return line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
    }

    private static string RestOfLine(string line, string keyword)
    {
        int hash = line.IndexOf('#');
        if (hash >= 0) line = line[..hash];
        return line.TrimStart()[keyword.Length..].Trim();
    }

    private static float Parse(string s) => float.Parse(s, CultureInfo.InvariantCulture);
}
