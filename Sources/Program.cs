using System.Windows.Forms;

namespace Renderer;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();   // visual styles, per-monitor DPI (see the csproj)
        Application.Run(new MainForm());
    }
}
