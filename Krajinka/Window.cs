using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Krajinka;

/// <summary>
/// Herní okno aplikace zajišťující inicializaci, vykreslování a zpracování vstupu.
/// </summary>
public class Window : GameWindow
{
    private Terrain terrain;
    private double fps = 0;

    /// <summary>
    /// Inicializuje nové okno aplikace s daným nastavením herní smyčky a nativního okna.
    /// </summary>
    /// <param name="gameWindowSettings">Nastavení herní smyčky (např. frekvence aktualizace).</param>
    /// <param name="nativeWindowSettings">Nastavení nativního okna (např. velikost, název, režim zobrazení).</param>
    public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
        : base(gameWindowSettings, nativeWindowSettings)
    {
    }

    /// <summary>
    /// Provede inicializaci OpenGL stavu a vytvoří instanci terénu při načtení okna.
    /// </summary>
    protected override void OnLoad()
    {
        base.OnLoad();

        GL.ClearColor(0.1f, 0.2f, 0.3f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        GL.PointSize(5);

        terrain = new Terrain(50, 50);
    }

    /// <summary>
    /// Vykreslí jeden snímek scény, nastaví kamerové matice a aktualizuje zobrazené FPS v titulku okna.
    /// </summary>
    /// <param name="e">Argumenty snímku obsahující čas od posledního vykreslení.</param>
    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Matice kamery
        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45f), (float)Size.X / Size.Y, 0.1f, 100f);
        Matrix4 view = Matrix4.LookAt(new Vector3(12, 10, 30), new Vector3(12, 0, 12), Vector3.UnitY);
        Matrix4 model = Matrix4.Identity;

        terrain.Render(model, view, projection);
        SwapBuffers();

        if (e.Time > 0)
        {
            fps = 0.95 * fps + 0.05 * (1 / e.Time);
            Title = $"Semestrální práce - Krajinka | FPS: {fps:0}";
        }
    }

    /// <summary>
    /// Aktualizuje stav aplikace pro aktuální snímek a zpracuje vstup z klávesnice.
    /// </summary>
    /// <param name="e">Argumenty snímku obsahující čas od poslední aktualizace.</param>
    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }
    }

    /// <summary>
    /// Reaguje na změnu velikosti okna a upraví OpenGL viewport na nové rozměry.
    /// </summary>
    /// <param name="e">Argumenty změny velikosti okna.</param>
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, Size.X, Size.Y);
    }
}