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
    private readonly List<ObjectInstance> objectInstances = new List<ObjectInstance>();

    /// <summary>
    /// Seznam náhodně umístěných květin.
    /// </summary>
    private readonly List<FlowerInstance> flowerInstances = new List<FlowerInstance>();

    /// <summary>
    /// Generátor náhodných čísel pro rotaci a měřítko objektů.
    /// </summary>
    private readonly Random random = new Random();

    /// <summary>
    /// Kolizní systém pro hitboxy a detekci kolizí.
    /// </summary>
    private CollisionSystem collisionSystem = null!;

    /// <summary>
    /// Shader použitý pro vykreslení scény.
    /// </summary>
    private Shader shader = null!;

    /// <summary>
    /// Shader použitý pro vykreslení poloprůhledné hladiny vody.
    /// </summary>
    private Shader waterShader = null!;

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
    /// Uplynulý čas pro animaci vody.
    /// </summary>
    private double waterAnimationTime;

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
    /// Kód objektu keře použitý při náhradě stromu na skále.
    /// </summary>
    private const byte BushObjectCode = 3;

    /// <summary>
    /// Kód objektu květin použitý při náhradě stromu na skále.
    /// </summary>
    private const byte FlowerObjectCode = 4;

    /// <summary>
    /// Počet květin, které se do scény umístí náhodně.
    /// </summary>
    private const int FlowerCount = 60;

    /// <summary>
    /// Kolizní poloměr hráče v rovině XZ.
    /// </summary>
    private const float CameraCollisionRadius = 0.35f;

    /// <summary>
    /// Základní radius hitboxu stromu.
    /// </summary>
    private const float TreeHitboxRadius = 0.45f;

    /// <summary>
    /// Základní radius hitboxu kamene.
    /// </summary>
    private const float RockHitboxRadius = 0.95f;

    /// <summary>
    /// Základní radius hitboxu keře.
    /// </summary>
    private const float BushHitboxRadius = 0.6f;

    /// <summary>
    /// Základní radius hitboxu květiny.
    /// </summary>
    private const float FlowerHitboxRadius = 0.25f;

    /// <summary>
    /// Výška hitboxu stromu.
    /// </summary>
    private const float TreeHitboxHeight = 2.4f;

    /// <summary>
    /// Výška hitboxu kamene.
    /// </summary>
    private const float RockHitboxHeight = 0.6f;

    /// <summary>
    /// Výška hitboxu keře.
    /// </summary>
    private const float BushHitboxHeight = 0.3f;

    /// <summary>
    /// Výška hitboxu květiny.
    /// </summary>
    private const float FlowerHitboxHeight = 0.2f;

    /// <summary>
    /// Sdílený model stromu načtený jednou.
    /// </summary>
    private Model treeModel = null!;

    /// <summary>
    /// Sdílený model kamene načtený jednou.
    /// </summary>
    private Model rockModel = null!;

    /// <summary>
    /// Sdílený model keře načtený jednou.
    /// </summary>
    private Model bushModel = null!;

    /// <summary>
    /// Sdílený model růže načtený jednou.
    /// </summary>
    private Model roseModel = null!;

    /// <summary>
    /// Sdílený model tulipánu načtený jednou.
    /// </summary>
    private Model tulipModel = null!;

    /// <summary>
    /// Pole sdílených modelů květin pro náhodný výběr při generování květin.
    /// </summary>
    private Model[] flowers = null!;

    /// <summary>
    /// Sdílený model slunce načtený jednou.
    /// </summary>
    private Model sunModel = null!;

    /// <summary>
    /// Měřítko modelu slunce.
    /// </summary>
    private const float SunScale = 0.2f;

    /// <summary>
    /// Pozice slunce na ose mezi kamerou a světlem (0 = kamera, 1 = světlo).
    /// </summary>
    private const float SunAxisFactor = 0.5f;

    /// <summary>
    /// Barva modelu slunce při vykreslení bez textury.
    /// </summary>
    private static readonly Vector3 SunColor = new Vector3(1.0f, 0.92f, 0.25f);

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
        waterShader = new Shader(Path.Combine("Shaders", "basic.vert"), Path.Combine("Shaders", "water.frag"));

        collisionSystem = new CollisionSystem(
            TreeObjectCode,
            RockObjectCode,
            BushObjectCode,
            FlowerObjectCode,
            CameraCollisionRadius,
            TreeHitboxRadius,
            RockHitboxRadius,
            BushHitboxRadius,
            FlowerHitboxRadius,
            TreeHitboxHeight,
            RockHitboxHeight,
            BushHitboxHeight,
            FlowerHitboxHeight);

        terrain = new Terrain(selectedMapPath);
        Objects.Add(terrain);
        treeModel = new Model(Path.Combine("Data", "models", "tree", "Tree.obj"));

        rockModel = new Model(Path.Combine("Data", "models","rock", "Rock1.obj"));

        bushModel = new Model(Path.Combine("Data", "models", "bush", "bush.obj"));

        roseModel = new Model(Path.Combine("Data", "models", "rose", "plant.obj"));

        tulipModel = new Model(Path.Combine("Data", "models", "tulip", "Roses Orange.obj"));

        flowers = new Model[] { roseModel, tulipModel };

        sunModel = new Model(Path.Combine("Data", "models","Sun.obj"), false);



        CreateObjects();
        CreateFlowers();

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

                SurfaceType surfaceType = terrain.SurfaceTypes[x, z];
                if (objectCode == TreeObjectCode)
                {
                    if (surfaceType == SurfaceType.Water)
                    {
                        continue;
                    }

                    if (surfaceType == SurfaceType.Rock)
                    {
                        objectCode = BushObjectCode;
                    }
                }

                float worldX = x * TerrainSampleSpacing;
                float worldY = terrain.Heights[x, z];
                float worldZ = z * TerrainSampleSpacing;

                float rotationY = GetRandomRotationY();
                float scale = GetScale(objectCode);
                Vector3 objectPosition = new Vector3(worldX, worldY, worldZ);

                objectInstances.Add(
                    new ObjectInstance
                    {
                        Position = objectPosition,
                        Type = objectCode,
                        RotationY = rotationY,
                        Scale = scale,
                        Hitbox = collisionSystem.CreateHitbox(objectCode, objectPosition)
                    });
            }
        }
    }   

    /// <summary>
    /// Vytvoří náhodně rozmístěné květiny na volných místech terénu.
    /// </summary>
    private void CreateFlowers()
    {
        flowerInstances.Clear();

        int mapWidth = terrain.ObjectCodes.GetLength(0);
        int mapDepth = terrain.ObjectCodes.GetLength(1);
        int attempts = 0;
        int maxAttempts = FlowerCount * 50;

        while (flowerInstances.Count < FlowerCount && attempts < maxAttempts)
        {
            attempts++;

            int x = random.Next(mapWidth);
            int z = random.Next(mapDepth);
            SurfaceType surfaceType = terrain.SurfaceTypes[x, z];

            if (surfaceType == SurfaceType.Water || surfaceType == SurfaceType.Rock)
            {
                continue;
            }

            float worldX = x * TerrainSampleSpacing;
            float worldY = terrain.Heights[x, z];
            float worldZ = z * TerrainSampleSpacing;

            int modelIndex = random.Next(flowers.Length);
            float scale = GetScale(FlowerObjectCode, modelIndex);

            Vector3 flowerPosition = new Vector3(worldX, worldY, worldZ);
            ObjectHitbox flowerHitbox = collisionSystem.CreateHitbox(FlowerObjectCode, flowerPosition);

            if (!collisionSystem.IsPlacementFree(flowerHitbox, objectInstances, flowerInstances))
            {
                continue;
            }

            flowerInstances.Add(
                new FlowerInstance
                {
                    Position = flowerPosition,
                    RotationY = GetRandomRotationY(),
                    Scale = scale,
                    ModelIndex = modelIndex,
                    Hitbox = flowerHitbox
                });
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
    /// <param name="currentTime">Uplynulý čas pro animaci vody.</param>
    private void DrawScene(Viewport viewportState, Camera cameraState, double currentTime)
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
        shader.SetUniform("texGrass", 1);
        shader.SetUniform("texRock", 2);
        shader.SetUniform("texMud", 3);
        shader.SetUniform("texModel", 1);
        shader.SetUniform("surfaceTypeMap", 4);
        shader.SetUniform("solidColor", Vector3.One);

        shader.SetUniform("isTerrain", 2);

        Vector3 cameraPosition = cameraState.GetPosition();
        Vector4 lightPositionWorld = lightSun.GetPositionWorld();
        Vector3 lightPosition = new Vector3(lightPositionWorld.X, lightPositionWorld.Y, lightPositionWorld.Z);
        Vector3 sunPosition = cameraPosition + ((lightPosition - cameraPosition) * SunAxisFactor);

        sunModel.SetPosition(sunPosition);
        sunModel.SetRotation(Vector3.Zero);
        sunModel.SetScale(new Vector3(SunScale, SunScale, SunScale));

        shader.SetUniform("isTerrain", 0);
        shader.SetUniform("solidColor", SunColor);
        shader.SetUniform("model", sunModel.GetModelMatrix());
        sunModel.Draw();

        shader.SetUniform("isTerrain", 2);
        shader.SetUniform("solidColor", Vector3.One);

        foreach (ObjectInstance model in objectInstances)
        {
            Model sharedModel = GetModelForObjectType(model.Type);

            ApplyModelTransform(sharedModel, model.Position, model.RotationY, model.Scale);

            shader.SetUniform("model", sharedModel.GetModelMatrix());
            sharedModel.Draw();
        }

        foreach (FlowerInstance flower in flowerInstances)
        {
            Model flowerModel = flowers[flower.ModelIndex];
            ApplyModelTransform(flowerModel, flower.Position, flower.RotationY, flower.Scale);

            shader.SetUniform("model", flowerModel.GetModelMatrix());
            flowerModel.Draw();
        }

        foreach (SceneObject sceneObject in Objects)
        {
            if (sceneObject is Terrain terrainObject)
            {
                terrainObject.BindSurfaceTextures();
                terrainObject.BindSurfaceTexture(SurfaceType.Mud, 0);

                shader.SetUniform("isTerrain", 1);
                shader.SetUniform("terrainMaxXZ", new Vector2(terrainObject.MaxX, terrainObject.MaxZ));

                shader.SetUniform("model", sceneObject.GetModelMatrix());
                sceneObject.Draw();

                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                GL.DepthMask(false);

                waterShader.Use();
                waterShader.SetUniform("projection", cameraState.GetProjectionMatrix(viewportState.GetAspectRatio()));
                waterShader.SetUniform("view", cameraState.GetViewMatrix());
                waterShader.SetUniform("model", sceneObject.GetModelMatrix());
                waterShader.SetUniform("cameraPosWorld", cameraState.GetPosition());
                waterShader.SetUniform("lightPosWorld", lightSun.GetPositionWorld());
                waterShader.SetUniform("lightColor", lightSun.Color);
                waterShader.SetUniform("lightIntensity", lightSun.Intensity);
                waterShader.SetUniform("texWater", 0);
                waterShader.SetUniform("surfaceTypeMap", 4);
                waterShader.SetUniform("terrainMaxXZ", new Vector2(terrainObject.MaxX, terrainObject.MaxZ));

                terrainObject.BindWaterTexture(0, currentTime);
                terrainObject.DrawWaterSurface();

                GL.DepthMask(true);
                GL.Disable(EnableCap.Blend);
                shader.Use();
                shader.SetUniform("isTerrain", 2);
                continue;
            }
            else
            {
                shader.SetUniform("isTerrain", 2);
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

        waterAnimationTime += e.Time;

        GL.ClearColor(0.529f, 0.808f, 0.922f, 1.0f);
        DrawScene(viewport, camera, waterAnimationTime);
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

        Vector3 moveDirection;

        if (camera.IsGrounded)
        {
            moveDirection = Vector3.Zero;

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
        }
        else
        {
            moveDirection = camera.MoveDirection;
        }

        float dt = (float)e.Time;
        Vector3 currentCameraPosition = camera.GetPosition();

        if (collisionSystem.IsColliding(currentCameraPosition, moveDirection, dt, camera.MovementSpeed, camera.EyeHeight, objectInstances))
        {
            moveDirection = Vector3.Zero;
        }

        camera.MoveDirection = moveDirection;

        Vector3 predictedCameraPosition = collisionSystem.GetPredictedCameraPosition(currentCameraPosition, moveDirection, dt, camera.MovementSpeed);

        camera.ExtraGroundY = collisionSystem.GetRockTopGroundY(predictedCameraPosition, camera.EyeHeight, objectInstances);

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
        waterShader.Dispose();
        treeModel.Dispose();
        rockModel.Dispose();
        bushModel.Dispose();
        sunModel.Dispose();
        
        foreach (Model flower in flowers)
        {
            flower.Dispose();
        }

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

    /// <summary>
    /// Vrátí náhodné měřítko podle typu objektu.
    /// Pro květinu (kód 4) používá i index modelu.
    /// </summary>
    /// <param name="objectCode">Kód objektu mapy.</param>
    /// <param name="flowerModelIndex">Index modelu květiny, jinak null.</param>
    /// <returns>Náhodné měřítko objektu.</returns>
    private float GetScale(byte objectCode, int? flowerModelIndex = null)
    {
        if (objectCode == TreeObjectCode)
        {
            return GetRandomScale(0.8f, 1.2f);
        }

        if (objectCode == BushObjectCode)
        {
            return GetRandomScale(0.015f, 0.027f);
        }

        if (objectCode == RockObjectCode)
        {
            return GetRandomScale(0.30f, 0.50f);
        }

        if (objectCode == FlowerObjectCode)
        {
            if (!flowerModelIndex.HasValue)
            {
                throw new InvalidOperationException("Pro FlowerObjectCode je nutné předat flowerModelIndex.");
            }

            if (flowerModelIndex.Value == 0)
            {
                return GetRandomScale(0.3f, 0.5f);
            }

            return GetRandomScale(0.005f, 0.01f);
        }

        throw new InvalidOperationException("Neznámý object code pro výpočet scale.");
    }

    /// <summary>
    /// Vrátí sdílený model podle typu objektu.
    /// </summary>
    /// <param name="objectType">Typ objektu.</param>
    /// <returns>Model použitý pro vykreslení.</returns>
    private Model GetModelForObjectType(byte objectType)
    {
        if (objectType == TreeObjectCode)
        {
            return treeModel;
        }

        if (objectType == BushObjectCode)
        {
            return bushModel;
        }

        return rockModel;
    }

    /// <summary>
    /// Aplikuje pozici, rotaci a měřítko na model před vykreslením.
    /// </summary>
    /// <param name="model">Upravovaný model.</param>
    /// <param name="position">Pozice ve světě.</param>
    /// <param name="rotationY">Rotace kolem osy Y v radiánech.</param>
    /// <param name="scale">Jednotné měřítko modelu.</param>
    private static void ApplyModelTransform(Model model, Vector3 position, float rotationY, float scale)
    {
        model.SetPosition(position);
        model.SetRotation(new Vector3(0.0f, rotationY, 0.0f));
        model.SetScale(new Vector3(scale, scale, scale));
    }
}