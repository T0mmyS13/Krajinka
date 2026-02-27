using OpenTK.Graphics.OpenGL; // OpenTK používá namespace OpenGL4 i pro verze 3.3
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Krajinka;

public class Window : GameWindow
{
    public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
        : base(gameWindowSettings, nativeWindowSettings)
    {
    }

    // Volá se pouze jednou při startu aplikace. Ideální pro načítání dat a nastavování paměti.
    protected override void OnLoad()
    {
        base.OnLoad();

        // Nastavíme barvu pozadí, kterou se okno vyčistí (třeba nebesky modrá)
        GL.ClearColor(0.2f, 0.4f, 0.8f, 1.0f);
    }

    // Volá se každý snímek. Slouží k samotnému kreslení grafiky.
    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        // Vymaže obrazovku nastavenou barvou z OnLoad
        GL.Clear(ClearBufferMask.ColorBufferBit);

        // ZDE BUDEME KRESLIT TERÉN

        // Prohodí buffery a zobrazí to, co jsme právě nakreslili
        SwapBuffers();
    }

    // Volá se každý snímek. Slouží pro logiku, fyziku a vstupy (klávesnice/myš).
    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        // Pokud uživatel zmáčkne Escape, zavřeme okno
        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }
    }

    // Volá se pokaždé, když uživatel změní velikost okna.
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        // Řekneme OpenGL, ať přizpůsobí vykreslovací plochu (Viewport) nové velikosti okna
        GL.Viewport(0, 0, Size.X, Size.Y);
    }
}