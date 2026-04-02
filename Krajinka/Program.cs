using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Krajinka
{
       
    /// <summary>
    /// Vstupní bod aplikace, který vytvoří a spustí herní okno.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Inicializuje nastavení okna a spustí hlavní smyčku aplikace.
        /// </summary>
        /// <param name="args">Argumenty příkazové řádky.</param>
        static void Main(string[] args)
        {
            var nativeWindowSettings = new NativeWindowSettings()
            {
                ClientSize = new Vector2i(800, 600),
                Title = "Semestrální práce - Krajinka",
                APIVersion = new Version(3, 3),
                Profile = ContextProfile.Core
            };

            using (var window = new Window(GameWindowSettings.Default, nativeWindowSettings))
            {
                window.Run();
            }
        }
    }
}