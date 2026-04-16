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
    /// Seznam instancí objektů načtených z mapy (pozice, typ, rotace, měřítko).
    /// </summary>
    private readonly List<(Vector3 Position, byte Type, float RotationY, float Scale)> objectInstances = new List<(Vector3, byte, float, float)>();

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
    /// Kód objektu stromu v mapě.
    /// </summary>
    private const byte TreeObjectCode = 1;

    /// <summary>
    /// Kód objektu kamene v mapě.
    /// </summary>
    private const byte RockObjectCode = 2;

    /// <summary>
    /// Kolizní poloměr hráče v rovině XZ.
    /// </summary>
    private const float CameraCollisionRadius = 0.35f;

    /// <summary>
    /// Základní kolizní poloměr stromu před aplikací měřítka instance.
    /// </summary>
    private const float TreeCollisionRadius = 0.55f;

    /// <summary>
    /// Základní kolizní poloměr kamene před aplikací měřítka instance.
    /// </summary>
    private const float RockCollisionRadius = 0.85f;

    /// <summary>
    /// Sdílený model stromu načtený jednou.
    /// </summary>
    private Model treeModel = null!;

    /// <summary>
    /// Sdílený model kamene načtený jednou.
    /// </summary>
    private Model rockModel = null!;

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
        treeModel = new Model(
            Path.Combine("Data", "models", "tree", "Tree.obj"),
            Vector3.Zero);

        rockModel = new Model(
            Path.Combine("Data", "models","rock", "Rock1.obj"),
            Vector3.Zero);

        CreateObjects();

        float startX = 125.0f;
        float startZ = 125.0f;
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
        objectInstances.Clear();

        int mapWidth = terrain.ObjectCodes.GetLength(0);
        int mapDepth = terrain.ObjectCodes.GetLength(1);

        for (int z = 0; z < mapDepth; z++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                byte objectCode = terrain.ObjectCodes[x, z];
                if (objectCode != TreeObjectCode && objectCode != RockObjectCode)
                {
                    continue;
                }

                float worldX = x * TerrainSampleSpacing;
                float worldY = terrain.Heights[x, z];
                float worldZ = z * TerrainSampleSpacing;

                float rotationY = GetRandomRotationY();
                float scale;

                if (objectCode == TreeObjectCode)
                {
                    scale = GetRandomScale(0.8f, 1.2f);
                }
                else
                {
                    scale = GetRandomScale(0.50f, 0.80f);
                }

                objectInstances.Add((
                    new Vector3(worldX, worldY, worldZ),
                    objectCode,
                    rotationY,
                    scale));
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
    /// Vrátí true, pokud plánovaný horizontální pohyb kamery narazí do stromu nebo kamene.
    /// </summary>
    /// <param name="currentPosition">Aktuální pozice kamery.</param>
    /// <param name="moveDirection">Požadovaný směr pohybu.</param>
    /// <param name="dt">Doba od posledního snímku v sekundách.</param>
    /// <returns>True pokud je pohyb blokován kolizí objektu, jinak false.</returns>
    private bool IsHorizontalMoveBlockedByObjects(Vector3 currentPosition, Vector3 moveDirection, float dt)
    {
        if (moveDirection.LengthSquared <= 0.0f)
        {
            return false;
        }

        Vector3 normalizedMoveDirection = Vector3.Normalize(moveDirection);
        float targetX = currentPosition.X + (normalizedMoveDirection.X * camera.MovementSpeed * dt);
        float targetZ = currentPosition.Z + (normalizedMoveDirection.Z * camera.MovementSpeed * dt);
        Vector2 targetPosition = new Vector2(targetX, targetZ);

        for (int i = 0; i < objectInstances.Count; i++)
        {
            (Vector3 position, byte type, float rotationY, float scale) = objectInstances[i];

            float baseObjectRadius;
            if (type == TreeObjectCode)
            {
                baseObjectRadius = TreeCollisionRadius;
            }
            else if (type == RockObjectCode)
            {
                baseObjectRadius = RockCollisionRadius;
            }
            else
            {
                continue;
            }

            float objectRadius = baseObjectRadius * scale;
            float combinedRadius = objectRadius + CameraCollisionRadius;

            Vector2 objectPosition = new Vector2(position.X, position.Z);
            float distanceSquared = Vector2.DistanceSquared(targetPosition, objectPosition);
            float combinedRadiusSquared = combinedRadius * combinedRadius;

            if (distanceSquared < combinedRadiusSquared)
            {
                return true;
            }
        }

        return false;
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

        shader.SetUniform("useTexture", 1);
        shader.SetUniform("isTerrain", 0);

        foreach ((Vector3 Position, byte Type, float RotationY, float Scale) model in objectInstances)
        {
            Model sharedModel;

            if (model.Type == TreeObjectCode)
            {
                sharedModel = treeModel;
            }
            else
            {
                sharedModel = rockModel;
            }

            sharedModel.SetPosition(model.Position);
            sharedModel.SetRotation(new Vector3(0.0f, model.RotationY, 0.0f));
            sharedModel.SetScale(new Vector3(model.Scale, model.Scale, model.Scale));

            shader.SetUniform("model", sharedModel.GetModelMatrix());
            sharedModel.Draw();
        }

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

        float dt = (float)e.Time;
        Vector3 currentCameraPosition = camera.GetPosition();

        if (IsHorizontalMoveBlockedByObjects(currentCameraPosition, moveDirection, dt))
        {
            moveDirection = Vector3.Zero;
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
        treeModel.Dispose();
        rockModel.Dispose();

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