namespace Renderer;

public interface ILoader
{
    /// <summary>Supported file extensions, lower-case with a leading dot.</summary>
    IReadOnlyList<string> Extensions { get; }

    VertexBuffer Load(string fileName);
}
