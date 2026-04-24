using System.Reflection;

namespace Renderer;

public sealed class LoadersFactory
{
    private readonly Dictionary<string, ILoader> _loaders = new Dictionary<string, ILoader>();

    public ILoader GetLoader(string fileName)
    {
        string ext = (Path.GetExtension(fileName) ?? string.Empty).ToLowerInvariant();

        if (_loaders.TryGetValue(ext, out ILoader loader))
            return loader;

        Type loaderInterface = typeof (ILoader);

        Type loaderClass = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass)
            .FirstOrDefault(t => t.GetInterfaces().Any(i => i == loaderInterface));

        return loaderClass == null ? null : _loaders[ext] = (ILoader)Activator.CreateInstance(loaderClass);
    }
}
