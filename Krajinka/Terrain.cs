using System;
using System.IO;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using SkiaSharp;

/// <summary>
/// Reprezentuje jednoduchý terén vykreslovaný pomocí OpenGL shaderů.
/// </summary>
public class Terrain
{
    private const float SampleSpacing = 0.5f;
    private const float HeightScale = 0.05f;
    private const int MinimumTerrainWidth = 512;
    private const int MinimumTerrainDepth = 512;

    private readonly float[] vertices;
    private readonly int width;
    private readonly int depth;

    private int VBO;
    private int VAO;
    private int shaderProgram;
    private int vertexCount;

    /// <summary>
    /// Výšky načtené z kanálu R.
    /// </summary>
    public readonly float[,] Heights;

    /// <summary>
    /// Objektové kódy načtené z kanálu G.
    /// </summary>
    public readonly byte[,] ObjectCodes;

    /// <summary>
    /// Hodnoty načtené z kanálu B.
    /// </summary>
    public readonly byte[,] BlueValues;

    /// <summary>
    /// Hodnoty načtené z kanálu A.
    /// </summary>
    public readonly byte[,] AlphaValues;

    /// <summary>
    /// Minimální souřadnice X terénu ve světě.
    /// </summary>
    public float MinX;

    /// <summary>
    /// Maximální souřadnice X terénu ve světě.
    /// </summary>
    public float MaxX;

    /// <summary>
    /// Minimální souřadnice Z terénu ve světě.
    /// </summary>
    public float MinZ;

    /// <summary>
    /// Maximální souřadnice Z terénu ve světě.
    /// </summary>
    public float MaxZ;

    /// <summary>
    /// Vytvoří terén načtený z RGBA PNG mapy.
    /// </summary>
    /// <param name="heightMapRelativePath">Relativní cesta k mapě vůči output složce.</param>
    public Terrain(string heightMapRelativePath)
    {
        LoadMapFromPng(
            heightMapRelativePath,
            out int loadedWidth,
            out int loadedDepth,
            out float[,] loadedHeights,
            out byte[,] loadedObjectCodes,
            out byte[,] loadedBlueValues,
            out byte[,] loadedAlphaValues);

        ValidateSize(loadedWidth, loadedDepth);

        width = loadedWidth;
        depth = loadedDepth;
        Heights = loadedHeights;
        ObjectCodes = loadedObjectCodes;
        BlueValues = loadedBlueValues;
        AlphaValues = loadedAlphaValues;

        SetBounds();

        vertexCount = (width - 1) * (depth - 1) * 6;
        vertices = new float[vertexCount * 9];

        BuildMeshVertices();
        SetupShaders();
        SetupBuffers();
    }

    /// <summary>
    /// Zkontroluje minimální platné rozměry terénu.
    /// </summary>
    /// <param name="width">Počet bodů v ose X.</param>
    /// <param name="depth">Počet bodů v ose Z.</param>
    private static void ValidateSize(int width, int depth)
    {
        if (width < MinimumTerrainWidth)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Šířka terénu musí být alespoň 512 vzorku.");
        }

        if (depth < MinimumTerrainDepth)
        {
            throw new ArgumentOutOfRangeException(nameof(depth), "Hloubka terénu musí být alespoň 512 vzorku.");
        }
    }

    /// <summary>
    /// Nastaví hranice terénu ve světových souřadnicích.
    /// </summary>
    private void SetBounds()
    {
        MinX = 0f;
        MaxX = (width - 1) * SampleSpacing;
        MinZ = 0f;
        MaxZ = (depth - 1) * SampleSpacing;
    }

    /// <summary>
    /// Načte terénní data z RGBA PNG souboru.
    /// </summary>
    /// <param name="heightMapRelativePath">Relativní cesta k PNG mapě vůči output složce.</param>
    /// <param name="loadedWidth">Načtená šířka mapy.</param>
    /// <param name="loadedDepth">Načtená hloubka mapy.</param>
    /// <param name="loadedHeights">Výšky odvozené z kanálu R.</param>
    /// <param name="loadedObjectCodes">Objektové kódy z kanálu G.</param>
    /// <param name="loadedBlueValues">Hodnoty z kanálu B.</param>
    /// <param name="loadedAlphaValues">Hodnoty z kanálu A.</param>
    private static void LoadMapFromPng(
        string heightMapRelativePath,
        out int loadedWidth,
        out int loadedDepth,
        out float[,] loadedHeights,
        out byte[,] loadedObjectCodes,
        out byte[,] loadedBlueValues,
        out byte[,] loadedAlphaValues)
    {
        if (string.IsNullOrWhiteSpace(heightMapRelativePath))
        {
            throw new ArgumentException("Cesta k mapě nesmí být prázdná.", nameof(heightMapRelativePath));
        }

        string fullPath = Path.Combine(AppContext.BaseDirectory, heightMapRelativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Soubor mapy nebyl nalezen.", fullPath);
        }

        using Stream imageStream = File.OpenRead(fullPath);
        using SKBitmap? bitmap = SKBitmap.Decode(imageStream);

        if (bitmap == null)
        {
            throw new InvalidOperationException("Soubor mapy se nepodařilo načíst jako PNG obrázek.");
        }

        loadedWidth = bitmap.Width;
        loadedDepth = bitmap.Height;

        loadedHeights = new float[loadedWidth, loadedDepth];
        loadedObjectCodes = new byte[loadedWidth, loadedDepth];
        loadedBlueValues = new byte[loadedWidth, loadedDepth];
        loadedAlphaValues = new byte[loadedWidth, loadedDepth];

        for (int z = 0; z < loadedDepth; z++)
        {
            for (int x = 0; x < loadedWidth; x++)
            {
                SKColor pixel = bitmap.GetPixel(x, z);

                loadedHeights[x, z] = pixel.Red * HeightScale;
                loadedObjectCodes[x, z] = pixel.Green;
                loadedBlueValues[x, z] = pixel.Blue;
                loadedAlphaValues[x, z] = pixel.Alpha;
            }
        }
    }

    /// <summary>
    /// Naplní pole vrcholů trojúhelníkovém mesh terénu.
    /// </summary>
    private void BuildMeshVertices()
    {
        int index = 0;

        for (int z = 0; z < depth - 1; z++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                float x0 = x * SampleSpacing;
                float x1 = (x + 1) * SampleSpacing;
                float z0 = z * SampleSpacing;
                float z1 = (z + 1) * SampleSpacing;

                float h00 = Heights[x, z];
                float h10 = Heights[x + 1, z];
                float h01 = Heights[x, z + 1];
                float h11 = Heights[x + 1, z + 1];

                byte b00 = BlueValues[x, z];
                byte b10 = BlueValues[x + 1, z];
                byte b01 = BlueValues[x, z + 1];
                byte b11 = BlueValues[x + 1, z + 1];

                CalculateNormalAtGrid(x, z, out float n00x, out float n00y, out float n00z);
                CalculateNormalAtGrid(x + 1, z, out float n10x, out float n10y, out float n10z);
                CalculateNormalAtGrid(x, z + 1, out float n01x, out float n01y, out float n01z);
                CalculateNormalAtGrid(x + 1, z + 1, out float n11x, out float n11y, out float n11z);

                AppendVertex(vertices, ref index, x0, h00, z0, b00, n00x, n00y, n00z);
                AppendVertex(vertices, ref index, x0, h01, z1, b01, n01x, n01y, n01z);
                AppendVertex(vertices, ref index, x1, h10, z0, b10, n10x, n10y, n10z);

                AppendVertex(vertices, ref index, x1, h10, z0, b10, n10x, n10y, n10z);
                AppendVertex(vertices, ref index, x0, h01, z1, b01, n01x, n01y, n01z);
                AppendVertex(vertices, ref index, x1, h11, z1, b11, n11x, n11y, n11z);
            }
        }
    }

    /// <summary>
    /// Spočítá normálu v bodě mřížky podle okolních výšek.
    /// </summary>
    /// <param name="gridX">Index bodu v ose X.</param>
    /// <param name="gridZ">Index bodu v ose Z.</param>
    /// <param name="nx">Výstupní složka X normály.</param>
    /// <param name="ny">Výstupní složka Y normály.</param>
    /// <param name="nz">Výstupní složka Z normály.</param>
    private void CalculateNormalAtGrid(int gridX, int gridZ, out float nx, out float ny, out float nz)
    {
        int leftX = Math.Max(gridX - 1, 0);
        int rightX = Math.Min(gridX + 1, width - 1);
        int downZ = Math.Max(gridZ - 1, 0);
        int upZ = Math.Min(gridZ + 1, depth - 1);

        float distanceX = (rightX - leftX) * SampleSpacing;
        float distanceZ = (upZ - downZ) * SampleSpacing;

        float heightChangePerX = 0.0f;
        if (distanceX > 0.0f)
        {
            heightChangePerX = (Heights[rightX, gridZ] - Heights[leftX, gridZ]) / distanceX;
        }

        float heightChangePerZ = 0.0f;
        if (distanceZ > 0.0f)
        {
            heightChangePerZ = (Heights[gridX, upZ] - Heights[gridX, downZ]) / distanceZ;
        }

        Vector3 normal = new Vector3(-heightChangePerX, 1.0f, -heightChangePerZ);
        if (normal.LengthSquared > 0.0f)
        {
            normal = Vector3.Normalize(normal);
        }

        nx = normal.X;
        ny = normal.Y;
        nz = normal.Z;
    }

    /// <summary>
    /// Přidá jeden vrchol do pole vrcholů.
    /// </summary>
    /// <param name="data">Cílové pole vrcholů.</param>
    /// <param name="index">Aktuální index pro zápis do pole vrcholů.</param>
    /// <param name="x">Souřadnice X vrcholu.</param>
    /// <param name="y">Souřadnice Y vrcholu.</param>
    /// <param name="z">Souřadnice Z vrcholu.</param>
    /// <param name="blueValue">Hodnota z kanálu B pro tento vrchol.</param>
    /// <param name="nx">Složka X normály.</param>
    /// <param name="ny">Složka Y normály.</param>
    /// <param name="nz">Složka Z normály.</param>
    private static void AppendVertex(float[] data, ref int index, float x, float y, float z, byte blueValue, float nx, float ny, float nz)
    {
        data[index++] = x;
        data[index++] = y;
        data[index++] = z;

        GetGreenSpectrumColor(blueValue, out float red, out float green, out float blue);

        data[index++] = red;
        data[index++] = green;
        data[index++] = blue;

        data[index++] = nx;
        data[index++] = ny;
        data[index++] = nz;
    }

    /// <summary>
    /// Převede hodnotu kanálu B (0-255) na barvu ve spektru zelené.
    /// </summary>
    /// <param name="blueValue">Hodnota kanálu B.</param>
    /// <param name="red">Výstupní složka R.</param>
    /// <param name="green">Výstupní složka G.</param>
    /// <param name="blue">Výstupní složka B.</param>
    private static void GetGreenSpectrumColor(byte blueValue, out float red, out float green, out float blue)
    {
        float normalized = blueValue / 255.0f;

        float minRed = 0.05f;
        float maxRed = 0.25f;
        float minGreen = 0.35f;
        float maxGreen = 0.90f;
        float minBlue = 0.05f;
        float maxBlue = 0.25f;

        red = minRed + (maxRed - minRed) * normalized;
        green = minGreen + (maxGreen - minGreen) * normalized;
        blue = minBlue + (maxBlue - minBlue) * normalized;
    }

    /// <summary>
    /// Vrátí výšku terénu v dané pozici pomocí bilineární interpolace.
    /// </summary>
    /// <param name="x">Souřadnice X ve světě.</param>
    /// <param name="z">Souřadnice Z ve světě.</param>
    /// <returns>Interpolovaná výška terénu.</returns>
    public float GetHeightAt(float x, float z)
    {
        float clampedX = MathHelper.Clamp(x, MinX, MaxX);
        float clampedZ = MathHelper.Clamp(z, MinZ, MaxZ);

        float gridX = clampedX / SampleSpacing;
        float gridZ = clampedZ / SampleSpacing;

        int x0 = (int)MathF.Floor(gridX);
        int z0 = (int)MathF.Floor(gridZ);
        int x1 = Math.Min(x0 + 1, width - 1);
        int z1 = Math.Min(z0 + 1, depth - 1);

        float blendX = gridX - x0;
        float blendZ = gridZ - z0;

        float h00 = Heights[x0, z0];
        float h10 = Heights[x1, z0];
        float h01 = Heights[x0, z1];
        float h11 = Heights[x1, z1];

        float row0Height = h00 + (h10 - h00) * blendX;
        float row1Height = h01 + (h11 - h01) * blendX;
        float finalHeight = row0Height + (row1Height - row0Height) * blendZ;

        return finalHeight;
    }

    /// <summary>
    /// Načte text shaderu ze souboru ve výstupní složce aplikace.
    /// </summary>
    /// <param name="relativePath">Relativní cesta k shader souboru.</param>
    /// <returns>Textový obsah shaderu.</returns>
    private static string LoadShaderSource(string relativePath)
    {
        string fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        return File.ReadAllText(fullPath);
    }

    /// <summary>
    /// Zkompiluje shadery a vytvoří OpenGL shader program.
    /// </summary>
    private void SetupShaders()
    {
        string vertexShaderSource = LoadShaderSource(Path.Combine("Shaders", "terrain.vert"));
        string fragmentShaderSource = LoadShaderSource(Path.Combine("Shaders", "terrain.frag"));

        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.CompileShader(vertexShader);

        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
        GL.CompileShader(fragmentShader);

        shaderProgram = GL.CreateProgram();
        GL.AttachShader(shaderProgram, vertexShader);
        GL.AttachShader(shaderProgram, fragmentShader);
        GL.LinkProgram(shaderProgram);

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
    }

    /// <summary>
    /// Vytvoří a nastaví OpenGL buffery a atributy vrcholů pro pozici, barvu a normálu.
    /// </summary>
    private void SetupBuffers()
    {
        VAO = GL.GenVertexArray();
        GL.BindVertexArray(VAO);

        VBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 9 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 9 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 9 * sizeof(float), 6 * sizeof(float));
        GL.EnableVertexAttribArray(2);
    }

    /// <summary>
    /// Vykreslí terén pomocí předaných transformačních matic.
    /// </summary>
    /// <param name="model">Modelová matice objektu.</param>
    /// <param name="view">Pohledová matice kamery.</param>
    /// <param name="projection">Projekční matice kamery.</param>
    public void Render(Matrix4 model, Matrix4 view, Matrix4 projection)
    {
        GL.UseProgram(shaderProgram);
        GL.BindVertexArray(VAO);

        int modelLocation = GL.GetUniformLocation(shaderProgram, "model");
        int viewLocation = GL.GetUniformLocation(shaderProgram, "view");
        int projLocation = GL.GetUniformLocation(shaderProgram, "projection");
        int lightDirLocation = GL.GetUniformLocation(shaderProgram, "lightDir");

        GL.UniformMatrix4(modelLocation, true, ref model);
        GL.UniformMatrix4(viewLocation, true, ref view);
        GL.UniformMatrix4(projLocation, true, ref projection);
        GL.Uniform3(lightDirLocation, new Vector3(0.5f, -1.0f, 0.3f));

        GL.DrawArrays(PrimitiveType.Triangles, 0, vertexCount);
    }
}