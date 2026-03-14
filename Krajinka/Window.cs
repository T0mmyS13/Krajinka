using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Krajinka;

/// <summary>
/// Hlavní okno aplikace. Zajišťuje inicializaci OpenGL, vykreslování scény a ovládání kamery.
/// </summary>
public class Window : GameWindow
{
    private Terrain terrain;
    private Camera camera;
    private double fps = 0;

    private Vector2 lastMousePos;
    private bool firstMove = true;

    /// <summary>
    /// Vytvoří nové herní okno s daným nastavením.
    /// </summary>
    /// <param name="gameWindowSettings">Nastavení herní smyčky.</param>
    /// <param name="nativeWindowSettings">Nastavení nativního okna.</param>
    public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
        : base(gameWindowSettings, nativeWindowSettings)
    {
    }

    /// <summary>
    /// Inicializuje OpenGL stav, terén a kameru po načtení okna.
    /// </summary>
    protected override void OnLoad()
    {
        base.OnLoad();

        GL.ClearColor(0.1f, 0.2f, 0.3f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        GL.PointSize(5);

        terrain = new Terrain(100, 100);
        camera = new Camera(new Vector3(25.0f, 10.0f, 25.0f));

        // Skryje a uzamkne kurzor pro FPS ovládání
        CursorState = CursorState.Grabbed;
    }

    /// <summary>
    /// Vykreslí jeden snímek scény a aktualizuje FPS v titulku okna.
    /// </summary>
    /// <param name="e">Časové informace o snímku.</param>
    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(45f),
            (float)Size.X / Size.Y,
            0.1f,
            100f);

        Matrix4 view = camera.GetViewMatrix();
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
    /// Zpracuje vstup z klávesnice a myši a aktualizuje pohyb kamery.
    /// </summary>
    /// <param name="e">Časové informace o aktualizačním kroku.</param>
    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        if (!IsFocused)
        {
            return;
        }

        var keyboard = KeyboardState;
        if (keyboard.IsKeyDown(Keys.Escape))
        {
            Close();
        }

        // Pohyb kamery klávesami WASD
        float speed = 6.0f * (float)e.Time;

        if (keyboard.IsKeyDown(Keys.W)) camera.Position += camera.Front * speed;
        if (keyboard.IsKeyDown(Keys.S)) camera.Position -= camera.Front * speed;
        if (keyboard.IsKeyDown(Keys.A)) camera.Position -= camera.Right * speed;
        if (keyboard.IsKeyDown(Keys.D)) camera.Position += camera.Right * speed;

        var mouse = MouseState;

        if (firstMove)
        {
            lastMousePos = new Vector2(mouse.X, mouse.Y);
            firstMove = false;
        }
        else
        {
            float deltaX = mouse.X - lastMousePos.X;
            float deltaY = mouse.Y - lastMousePos.Y;

            lastMousePos = new Vector2(mouse.X, mouse.Y);

            camera.Yaw += deltaX * 0.2f;
            camera.Pitch -= deltaY * 0.2f;
            camera.UpdateVectors();
        }
    }

    /// <summary>
    /// Upraví OpenGL viewport při změně velikosti okna.
    /// </summary>
    /// <param name="e">Informace o nové velikosti okna.</param>
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, Size.X, Size.Y);
    }
}