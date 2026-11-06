namespace Renderer;

public interface IModelLoader
{
    /// <summary>Supported file extensions, lower-case with a leading dot.</summary>
    IReadOnlyList<string> Extensions { get; }

    /// <summary>Loads a mesh centred on the origin, with normals on every vertex.</summary>
    Mesh Load(string fileName);
}

/// <summary>The loaders the application knows, picked by file extension.</summary>
public sealed class ModelLoaders
{
    private readonly Dictionary<string, IModelLoader> _byExtension = new(StringComparer.OrdinalIgnoreCase);

    /// <param name="decodeTexture">Reads an image file into a <see cref="Texture"/>; null when textures are not wanted.</param>
    public ModelLoaders(Func<string, Texture?>? decodeTexture = null)
    {
        Register(new ObjLoader(decodeTexture));
        Register(new DxfLoader());
    }

    public IModelLoader? For(string fileName) => _byExtension.GetValueOrDefault(Path.GetExtension(fileName));

    public bool CanLoad(string fileName) => For(fileName) is not null;

    public Mesh Load(string fileName) =>
        (For(fileName) ?? throw new NotSupportedException($"No loader for '{Path.GetFileName(fileName)}'.")).Load(fileName);

    private void Register(IModelLoader loader)
    {
        foreach (string extension in loader.Extensions)
            _byExtension[extension] = loader;
    }
}
