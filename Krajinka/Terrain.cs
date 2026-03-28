using System;
using System.IO;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

/// <summary>
/// Reprezentuje jednoduchý terén vykreslovaný pomocí OpenGL shaderů.
/// </summary>
public class Terrain
{
    private const float SampleSpacing = 0.5f;

    private readonly float[] vertices;
    private readonly float[,] heights;
    private readonly int width;
    private readonly int depth;

    private int VBO;
    private int VAO;
    private int shaderProgram;
    private int vertexCount;

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
    /// Vytvoří terén o zadané šířce a hloubce a připraví data pro vykreslení.
    /// </summary>
    /// <param name="width">Počet bodů v ose X.</param>
    /// <param name="depth">Počet bodů v ose Z.</param>
    public Terrain(int width, int depth)
    {
        if (width < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Šířka terénu musí být alespoň 2 vzorky.");
        }

        if (depth < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(depth), "Hloubka terénu musí být alespoň 2 vzorky.");
        }

        this.width = width;
        this.depth = depth;

        MinX = 0f;
        MaxX = (width - 1) * SampleSpacing;
        MinZ = 0f;
        MaxZ = (depth - 1) * SampleSpacing;

        heights = new float[width, depth];
        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                heights[x, z] = 0.0f;
            }
        }

        vertexCount = (width - 1) * (depth - 1) * 6;
        vertices = new float[vertexCount * 9];

        int index = 0;
        for (int z = 0; z < depth - 1; z++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                float x0 = x * SampleSpacing;
                float x1 = (x + 1) * SampleSpacing;
                float z0 = z * SampleSpacing;
                float z1 = (z + 1) * SampleSpacing;

                float h00 = heights[x, z];
                float h10 = heights[x + 1, z];
                float h01 = heights[x, z + 1];
                float h11 = heights[x + 1, z + 1];

                AppendVertex(vertices, ref index, x0, h00, z0, x, z);
                AppendVertex(vertices, ref index, x0, h01, z1, x, z + 1);
                AppendVertex(vertices, ref index, x1, h10, z0, x + 1, z);

                AppendVertex(vertices, ref index, x1, h10, z0, x + 1, z);
                AppendVertex(vertices, ref index, x0, h01, z1, x, z + 1);
                AppendVertex(vertices, ref index, x1, h11, z1, x + 1, z + 1);
            }
        }

        SetupShaders();
        SetupBuffers();
    }

    /// <summary>
    /// Přidá jeden vrchol do pole vrcholů.
    /// </summary>
    /// <param name="data">Cílové pole vrcholů.</param>
    /// <param name="index">Aktuální index pro zápis do pole vrcholů.</param>
    /// <param name="x">Souřadnice X vrcholu.</param>
    /// <param name="y">Souřadnice Y vrcholu.</param>
    /// <param name="z">Souřadnice Z vrcholu.</param>
    /// <param name="gridX">Index vrcholu v mřížce na ose X.</param>
    /// <param name="gridZ">Index vrcholu v mřížce na ose Z.</param>
    private static void AppendVertex(float[] data, ref int index, float x, float y, float z, int gridX, int gridZ)
    {
        data[index++] = x;
        data[index++] = y;
        data[index++] = z;

        if ((gridX + gridZ) % 2 == 0)
        {
            data[index++] = 0.2f;
            data[index++] = 0.8f;
            data[index++] = 0.2f;
        }
        else
        {
            data[index++] = 0.1f;
            data[index++] = 0.6f;
            data[index++] = 0.1f;
        }

        data[index++] = 0.0f;
        data[index++] = 1.0f;
        data[index++] = 0.0f;
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

        float h00 = heights[x0, z0];
        float h10 = heights[x1, z0];
        float h01 = heights[x0, z1];
        float h11 = heights[x1, z1];

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