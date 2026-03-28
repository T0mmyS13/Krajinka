using System.Collections.Generic;
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
    private const float EyeHeight = 1.8f;
    private const float MouseDeltaEpsilon = 0.0001f;

    private Terrain terrain;
    private Camera camera;
    private double fps = 0;

    private readonly Queue<double> frameTimes = new();
    private double frameTimeSum = 0;

    private Vector2 lastMousePos;
    private bool firstMove = true;

    private Vector3 moveForward;
    private Vector3 moveRight;

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

        float startX = 25.0f;
        float startZ = 25.0f;
        float startY = terrain.GetHeightAt(startX, startZ) + EyeHeight;
        camera = new Camera(new Vector3(startX, startY, startZ));

        RecalculateMovementDirections();

        // Skryje a uzamkne kurzor pro FPS ovládání
        CursorState = CursorState.Grabbed;
    }

    /// <summary>
    /// Přepočítá pomocné směry pohybu v rovině XZ podle aktuální orientace kamery.
    /// </summary>
    private void RecalculateMovementDirections()
    {
        moveForward = new Vector3(camera.Front.X, 0f, camera.Front.Z);
        if (moveForward.LengthSquared > 0)
        {
            moveForward = Vector3.Normalize(moveForward);
        }

        moveRight = new Vector3(camera.Right.X, 0f, camera.Right.Z);
        if (moveRight.LengthSquared > 0)
        {
            moveRight = Vector3.Normalize(moveRight);
        }
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

        // Udržujeme časy snímků jen za poslední 1 sekundu
        frameTimes.Enqueue(e.Time);
        frameTimeSum += e.Time;

        while (frameTimeSum > 1.0 && frameTimes.Count > 0)
        {
            double oldestFrameTime = frameTimes.Dequeue();
            frameTimeSum -= oldestFrameTime;
        }

        if (frameTimeSum > 0)
        {
            fps = frameTimes.Count / frameTimeSum;
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

        KeyboardState keyboard = KeyboardState;
        if (keyboard.IsKeyDown(Keys.Escape))
        {
            Close();
        }

        float speed = 6.0f * (float)e.Time;
        Vector3 moveDirection = Vector3.Zero;

        if (keyboard.IsKeyDown(Keys.W))
        {
            moveDirection += moveForward;
        }

        if (keyboard.IsKeyDown(Keys.S))
        {
            moveDirection -= moveForward;
        }

        if (keyboard.IsKeyDown(Keys.A))
        {
            moveDirection -= moveRight;
        }

        if (keyboard.IsKeyDown(Keys.D))
        {
            moveDirection += moveRight;
        }

        // Normalizace zajistí stejnou rychlost i při diagonále
        if (moveDirection.LengthSquared > 0)
        {
            moveDirection = Vector3.Normalize(moveDirection);
            camera.Position += moveDirection * speed;
        }

        float clampedX = MathHelper.Clamp(camera.Position.X, terrain.MinX, terrain.MaxX);
        float clampedZ = MathHelper.Clamp(camera.Position.Z, terrain.MinZ, terrain.MaxZ);
        float terrainHeight = terrain.GetHeightAt(clampedX, clampedZ);

        camera.Position = new Vector3(clampedX, terrainHeight + EyeHeight, clampedZ);

        MouseState mouse = MouseState;

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

            bool hasMouseMovement = MathF.Abs(deltaX) > MouseDeltaEpsilon || MathF.Abs(deltaY) > MouseDeltaEpsilon;
            if (hasMouseMovement)
            {
                camera.Yaw += deltaX * 0.2f;
                camera.Pitch -= deltaY * 0.2f;
                camera.UpdateVectors();
                RecalculateMovementDirections();
            }
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