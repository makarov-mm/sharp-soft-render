using System.Reflection;

namespace Renderer;

public sealed class LoadersFactory
{
    private readonly Dictionary<string, ILoader> _loaders;

    public LoadersFactory()
    {
        _loaders = new Dictionary<string, ILoader>(StringComparer.OrdinalIgnoreCase);

        Type loaderInterface = typeof(ILoader);

        IEnumerable<Type> loaderClasses = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && loaderInterface.IsAssignableFrom(t));

        foreach (Type loaderClass in loaderClasses)
        {
            var loader = (ILoader)Activator.CreateInstance(loaderClass);

            foreach (string extension in loader!.Extensions)
            {
                _loaders[extension] = loader;
            }
        }
    }

    public ILoader GetLoader(string fileName)
    {
        string ext = Path.GetExtension(fileName) ?? string.Empty;
        return _loaders.GetValueOrDefault(ext);
    }

    public bool CanLoad(string fileName)
    {
        return GetLoader(fileName) != null;
    }
}
