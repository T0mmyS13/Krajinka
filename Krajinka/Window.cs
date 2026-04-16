using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Krajinka;

/// <summary>
/// Hlavní okno aplikace s vykreslovací a aktualizační smyčkou.
/// </summary>
public class Window : GameWindow
{
    /// <summary>
    /// Výška očí kamery nad terénem.
    /// </summary>
    private const float EyeHeight = 1.8f;

    /// <summary>
    /// Minimální pohyb myši, od kterého se zpracuje změna pohledu.
    /// </summary>
    private const float MouseDeltaEpsilon = 0.0001f;

    /// <summary>
    /// Rozestup vzorků terénu v mapě objektů.
    /// </summary>
    private const float TerrainSampleSpacing = 0.5f;

    /// <summary>
    /// Počet snímků po přepnutí fullscreen, kdy se resetuje myš.
    /// </summary>
    private const int MouseFSResetFrames = 3;

    /// <summary>
    /// Seznam objektů aktuálně přidaných do scény.
    /// </summary>
    private readonly List<SceneObject> Objects = new List<SceneObject>();

    /// <summary>
    /// Generátor náhodných čísel pro rotaci a měřítko objektů.
    /// </summary>
    private readonly Random random = new Random();

    /// <summary>
    /// Shader použitý pro vykreslení scény.
    /// </summary>
    private Shader shader = null!;

    /// <summary>
    /// Informace o aktivním viewportu.
    /// </summary>
    private Viewport viewport = null!;

    /// <summary>
    /// Kamera hráče.
    /// </summary>
    private Camera camera = null!;

    /// <summary>
    /// Terén načtený z mapy.
    /// </summary>
    private Terrain terrain = null!;

    /// <summary>
    /// Hlavní světlo scény.
    /// </summary>
    private Light lightSun = null!;

    /// <summary>
    /// Historie časů snímků pro výpočet FPS.
    /// </summary>
    private readonly Queue<double> frameTimes = new Queue<double>();

    /// <summary>
    /// Součet časů snímků v aktuálním FPS okně.
    /// </summary>
    private double frameTimeSum;

    /// <summary>
    /// Poslední známá pozice myši.
    /// </summary>
    private Vector2 lastMousePos;

    /// <summary>
    /// Indikuje první snímek pohybu myši po startu/režimu fullscreen.
    /// </summary>
    private bool firstMove = true;

    /// <summary>
    /// Počet snímků, po které se ignoruje delta myši po změně režimu okna.
    /// </summary>
    private int mouseResetFrames;

    /// <summary>
    /// Relativní cesta k vybrané mapě terénu.
    /// </summary>
    private readonly string selectedMapPath;

    /// <summary>
    /// Vytvoří hlavní okno aplikace.
    /// </summary>
    /// <param name="gameWindowSettings">Nastavení herní smyčky.</param>
    /// <param name="nativeWindowSettings">Nativní nastavení okna.</param>
    /// <param name="terrainMapPath">Relativní cesta k mapě terénu.</param>
    public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, string terrainMapPath)
        : base(gameWindowSettings, nativeWindowSettings)
    {
        selectedMapPath = terrainMapPath;
    }

    /// <summary>
    /// Inicializuje grafiku, scénu a vstupy po vytvoření okna.
    /// </summary>
    protected override void OnLoad()
    {
        base.OnLoad();


        viewport = new Viewport();
        viewport.ClientSize = Size;

        shader = new Shader(Path.Combine("Shaders", "basic.vert"), Path.Combine("Shaders", "basic.frag"));

        terrain = new Terrain(selectedMapPath);
        Objects.Add(terrain);

        CreateObjects();

        float startX = 25.0f;
        float startZ = 25.0f;
        float startY = terrain.GetHeightAt(startX, startZ) + EyeHeight;

        camera = new Camera(new Vector3(startX, startY, startZ));
        camera.EyeHeight = EyeHeight;
        camera.MinX = terrain.MinX;
        camera.MaxX = terrain.MaxX;
        camera.MinZ = terrain.MinZ;
        camera.MaxZ = terrain.MaxZ;
        camera.Terrain = terrain;

        lightSun = Light.CreatePoint(new Vector3(200.0f, 350.0f, 150.0f), Vector3.One, 1.0f);

        CursorState = CursorState.Grabbed;
    }

    /// <summary>
    /// Vytvoří objekty podle zeleného kanálu mapy objektů.
    /// </summary>
    private void CreateObjects()
    {
        int mapWidth = terrain.ObjectCodes.GetLength(0);
        int mapDepth = terrain.ObjectCodes.GetLength(1);

        for (int z = 0; z < mapDepth; z++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                byte objectCode = terrain.ObjectCodes[x, z];
                if (objectCode != 1 && objectCode != 2)
                {
                    continue;
                }

                float worldX = x * TerrainSampleSpacing;
                float worldY = terrain.Heights[x, z];
                float worldZ = z * TerrainSampleSpacing;

                if (objectCode == 1)
                {
                    Model tree = new Model(
                        Path.Combine("Data","models", "tree.obj"),
                        new Vector3(0.12f, 0.62f, 0.14f),
                        new Vector3(worldX, worldY, worldZ));

                    float treeScale = GetRandomScale(0.28f, 0.42f);
                    tree.SetScale(new Vector3(treeScale, treeScale, treeScale));
                    tree.SetRotation(new Vector3(0.0f, GetRandomRotationY(), 0.0f));
                    Objects.Add(tree);
                }
                else
                {
                    Model rock = new Model(
                        Path.Combine("Data","models", "rock.obj"),
                        new Vector3(0.50f, 0.50f, 0.50f),
                        new Vector3(worldX, worldY, worldZ));

                    float rockScale = GetRandomScale(0.50f, 0.80f);
                    rock.SetScale(new Vector3(rockScale, rockScale, rockScale));
                    rock.SetRotation(new Vector3(0.0f, GetRandomRotationY(), 0.0f));
                    Objects.Add(rock);
                }
            }
        }
    }

    /// <summary>
    /// Vrátí náhodné měřítko v daném rozsahu.
    /// </summary>
    /// <param name="minScale">Minimální měřítko.</param>
    /// <param name="maxScale">Maximální měřítko.</param>
    /// <returns>Náhodná hodnota měřítka.</returns>
    private float GetRandomScale(float minScale, float maxScale)
    {
        float value = (float)random.NextDouble();
        return minScale + (value * (maxScale - minScale));
    }

    /// <summary>
    /// Vrátí náhodnou rotaci kolem osy Y v radiánech.
    /// </summary>
    /// <returns>Náhodná rotace v radiánech.</returns>
    private float GetRandomRotationY()
    {
        float degrees = (float)(random.NextDouble() * 360.0);
        return MathHelper.DegreesToRadians(degrees);
    }

    /// <summary>
    /// Vykreslí scénu do zadaného viewportu.
    /// </summary>
    /// <param name="viewportState">Aktuální viewport.</param>
    /// <param name="cameraState">Aktuální stav kamery.</param>
    private void DrawScene(Viewport viewportState, Camera cameraState)
    {
        (Vector2i position, Vector2i size) = viewportState.GetPixelViewport();

        GL.Enable(EnableCap.ScissorTest);
        GL.Scissor(position.X, position.Y, size.X, size.Y);

        GL.Viewport(position.X, position.Y, size.X, size.Y);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);
        GL.FrontFace(FrontFaceDirection.Ccw);

        shader.Use();
        shader.SetUniform("projection", cameraState.GetProjectionMatrix(viewportState.GetAspectRatio()));
        shader.SetUniform("view", cameraState.GetViewMatrix());
        shader.SetUniform("lightPosWorld", lightSun.GetPositionWorld());
        shader.SetUniform("lightColor", lightSun.Color);
        shader.SetUniform("lightIntensity", lightSun.Intensity);
        shader.SetUniform("texWater", 0);
        shader.SetUniform("texGrass", 1);
        shader.SetUniform("texRock", 2);
        shader.SetUniform("texMud", 3);
        shader.SetUniform("surfaceTypeMap", 4);


        foreach (SceneObject sceneObject in Objects)
        {
            if (sceneObject is Terrain terrainObject)
            {
                terrainObject.BindSurfaceTextures();

                shader.SetUniform("useTexture", 1);

                shader.SetUniform("isTerrain", 1);
                shader.SetUniform("terrainMaxXZ", new Vector2(terrainObject.MaxX, terrainObject.MaxZ));
            }
            else
            {
                shader.SetUniform("useTexture", 1);
                shader.SetUniform("isTerrain", 0);
            }

            shader.SetUniform("model", sceneObject.GetModelMatrix());
            sceneObject.Draw();
        }
    }

    /// <summary>
    /// Vykreslí aktuální snímek.
    /// </summary>
    /// <param name="e">Časové informace snímku.</param>
    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        GL.ClearColor(0.529f, 0.808f, 0.922f, 1.0f);
        DrawScene(viewport, camera);
        SwapBuffers();

        frameTimes.Enqueue(e.Time);
        frameTimeSum += e.Time;

        while (frameTimeSum > 1.0 && frameTimes.Count > 0)
        {
            double oldestFrameTime = frameTimes.Dequeue();
            frameTimeSum -= oldestFrameTime;
        }

        if (frameTimeSum > 0)
        {
            double fps = frameTimes.Count / frameTimeSum;
            Title = $"Semestrální práce - Krajinka | FPS: {fps:0}";
        }
    }

    /// <summary>
    /// Aktualizuje logiku scény a zpracování vstupu.
    /// </summary>
    /// <param name="e">Časové informace snímku.</param>
    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        if (!IsFocused)
        {
            return;
        }

        KeyboardState keyboard = KeyboardState;

        if (keyboard.IsKeyPressed(Keys.F11))
        {
            ToggleFullscreen();
        }

        if (keyboard.IsKeyDown(Keys.Escape))
        {
            Close();
        }

        if (keyboard.IsKeyPressed(Keys.Space))
        {
            camera.RequestJump();
        }

        Vector3 moveForward = new Vector3(camera.Front.X, 0f, camera.Front.Z);
        if (moveForward.LengthSquared > 0)
        {
            moveForward = Vector3.Normalize(moveForward);
        }

        Vector3 moveRight = new Vector3(camera.Right.X, 0f, camera.Right.Z);
        if (moveRight.LengthSquared > 0)
        {
            moveRight = Vector3.Normalize(moveRight);
        }

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

        if (moveDirection.LengthSquared > 0)
        {
            moveDirection = Vector3.Normalize(moveDirection);
        }

        camera.MoveDirection = moveDirection;

        MouseState mouse = MouseState;
        if (firstMove || mouseResetFrames > 0)
        {
            lastMousePos = new Vector2(mouse.X, mouse.Y);
            firstMove = false;

            if (mouseResetFrames > 0)
            {
                mouseResetFrames--;
            }
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
            }
        }

        float dt = (float)e.Time;
        camera.Update(dt);

        for (int i = 0; i < Objects.Count; i++)
        {
            Objects[i].Update(dt);
        }
    }

    /// <summary>
    /// Reaguje na změnu velikosti okna.
    /// </summary>
    /// <param name="e">Informace o nové velikosti.</param>
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        viewport.ClientSize = Size;

        (Vector2i position, Vector2i size) = viewport.GetPixelViewport();
        GL.Viewport(position.X, position.Y, size.X, size.Y);
    }

    /// <summary>
    /// Uvolní prostředky při zavírání okna.
    /// </summary>
    protected override void OnUnload()
    {
        base.OnUnload();

        shader.Dispose();

        foreach (SceneObject obj in Objects)
        {
            obj.Dispose();
        }
    }

    /// <summary>
    /// Přepne okno mezi běžným a borderless fullscreen režimem.
    /// </summary>
    private void ToggleFullscreen()
    {
        if (WindowState == WindowState.Normal)
        {
            WindowBorder = WindowBorder.Hidden;
            WindowState = WindowState.Maximized;
        }
        else
        {
            WindowState = WindowState.Normal;
            WindowBorder = WindowBorder.Resizable;
        }

        firstMove = true;
        mouseResetFrames = MouseFSResetFrames;
    }
}