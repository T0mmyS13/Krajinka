using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Krajinka;

class Program
{
    static void Main(string[] args)
    {
        // Nastavení vlastností samotného okna
        var nativeWindowSettings = new NativeWindowSettings()
        {
            ClientSize = new Vector2i(800, 600),
            Title = "Semestrální práce - Krajinka",
            APIVersion = new Version(3, 3),
            Profile = ContextProfile.Core
        };

        // Vytvoření a spuštění okna
        using (var window = new Window(GameWindowSettings.Default, nativeWindowSettings))
        {
            window.Run();
        }
    }
}