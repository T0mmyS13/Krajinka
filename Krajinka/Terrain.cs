using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Krajinka;

/// <summary>
/// Reprezentuje terén načtený z RGBA mapy.
/// </summary>
internal class Terrain : SceneObject
{
    /// <summary>
    /// Vzdálenost sousedních vzorků terénu v jednotkách světa.
    /// </summary>
    private const float SampleSpacing = 0.5f;

    /// <summary>
    /// Převodní koeficient výšky z mapy do jednotek světa.
    /// </summary>
    private const float HeightScale = 0.05f;

    /// <summary>
    /// Minimální povolená šířka terénu ve vzorcích.
    /// </summary>
    private const int MinimumTerrainWidth = 512;

    /// <summary>
    /// Minimální povolená hloubka terénu ve vzorcích.
    /// </summary>
    private const int MinimumTerrainDepth = 512;

    /// <summary>
    /// Výška, od které se začíná přecházet z trávy do hlíny.
    /// </summary>
    private const float GrassToDirtThreshold = 0.35f;

    /// <summary>
    /// Výška, od které se začíná přecházet z hlíny do skály.
    /// </summary>
    private const float DirtToRockThreshold = 0.70f;

    /// <summary>
    /// Výška, od které se začíná přecházet ze skály do sněhu.
    /// </summary>
    private const float RockToSnowThreshold = 0.90f;

    /// <summary>
    /// Minimální výškové rozpětí terénu pro výskyt sněhu.
    /// </summary>
    private const float SnowHeightRangeThreshold = 60.0f;

    /// <summary>
    /// Šířka mapy terénu ve vzorcích.
    /// </summary>
    private readonly int width;

    /// <summary>
    /// Hloubka mapy terénu ve vzorcích.
    /// </summary>
    private readonly int depth;

    /// <summary>
    /// Nejnižší výška terénu v mapě.
    /// </summary>
    private float minHeight;

    /// <summary>
    /// Nejvyšší výška terénu v mapě.
    /// </summary>
    private float maxHeight;

    /// <summary>
    /// Vrcholová data terénu.
    /// </summary>
    private readonly VertexColorNormal[] vertices;

    /// <summary>
    /// Indexová data terénu.
    /// </summary>
    private readonly Triangle[] triangles;

    /// <summary>
    /// ID vertex array objektu terénu.
    /// </summary>
    private readonly int VAO;

    /// <summary>
    /// ID vertex buffer objektu terénu.
    /// </summary>
    private readonly int VBO;

    /// <summary>
    /// ID index buffer objektu terénu.
    /// </summary>
    private readonly int IBO;

    /// <summary>
    /// Indikuje, zda byly prostředky terénu už uvolněny.
    /// </summary>
    private bool disposed;

    /// <summary>
    /// Výšky terénu v mřížce [x, z].
    /// </summary>
    public readonly float[,] Heights;

    /// <summary>
    /// Kódy objektů z G kanálu mapy v mřížce [x, z].
    /// </summary>
    public readonly byte[,] ObjectCodes;

    /// <summary>
    /// Hodnoty modrého kanálu mapy v mřížce [x, z].
    /// </summary>
    public readonly byte[,] BlueValues;

    /// <summary>
    /// Hodnoty alfa kanálu mapy v mřížce [x, z].
    /// </summary>
    public readonly byte[,] AlphaValues;

    /// <summary>
    /// Minimální X souřadnice pohybu po terénu.
    /// </summary>
    public float MinX;

    /// <summary>
    /// Maximální X souřadnice pohybu po terénu.
    /// </summary>
    public float MaxX;

    /// <summary>
    /// Minimální Z souřadnice pohybu po terénu.
    /// </summary>
    public float MinZ;

    /// <summary>
    /// Maximální Z souřadnice pohybu po terénu.
    /// </summary>
    public float MaxZ;

    /// <summary>
    /// Vytvoří terén z PNG mapy.
    /// </summary>
    /// <param name="heightMapRelativePath">Relativní cesta k mapě.</param>
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

        SetHeightRange();
        SetBounds();

        vertices = BuildMeshVertices();
        triangles = BuildMeshTriangles();

        VAO = GL.GenVertexArray();
        GL.BindVertexArray(VAO);

        VBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * VertexColorNormal.GetSizeInBytes(), vertices, BufferUsageHint.StaticDraw);

        IBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, IBO);
        GL.BufferData(BufferTarget.ElementArrayBuffer, triangles.Length * 3 * sizeof(int), triangles, BufferUsageHint.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, VertexColorNormal.GetSizeInBytes(), 0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, VertexColorNormal.GetSizeInBytes(), Vector3.SizeInBytes);
        GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, VertexColorNormal.GetSizeInBytes(), 2 * Vector3.SizeInBytes);

        GL.BindVertexArray(0);
    }

    /// <summary>
    /// Ověří minimální rozměry načtené mapy.
    /// </summary>
    /// <param name="width">Šířka mapy.</param>
    /// <param name="depth">Hloubka mapy.</param>
    private static void ValidateSize(int width, int depth)
    {
        if (width < MinimumTerrainWidth)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Šířka terénu musí být alespoň 512 vzorku.");
        }

        if (depth < MinimumTerrainDepth)
        {
            throw new ArgumentOutOfRangeException(nameof(depth), "Hloubka terénu musi být alespoň 512 vzorku.");
        }
    }

    /// <summary>
    /// Nastaví minimální a maximální výšku terénu.
    /// </summary>
    private void SetHeightRange()
    {
        minHeight = Heights[0, 0];
        maxHeight = Heights[0, 0];

        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float height = Heights[x, z];
                if (height < minHeight)
                {
                    minHeight = height;
                }

                if (height > maxHeight)
                {
                    maxHeight = height;
                }
            }
        }
    }

    /// <summary>
    /// Nastaví světové hranice terénu.
    /// </summary>
    private void SetBounds()
    {
        MinX = 0f;
        MaxX = (width - 1) * SampleSpacing;
        MinZ = 0f;
        MaxZ = (depth - 1) * SampleSpacing;
    }

    /// <summary>
    /// Načte PNG mapu a rozparsuje z ní výšky a datové kanály.
    /// </summary>
    /// <param name="heightMapRelativePath">Relativní cesta k mapě.</param>
    /// <param name="loadedWidth">Výstupní šířka mapy.</param>
    /// <param name="loadedDepth">Výstupní hloubka mapy.</param>
    /// <param name="loadedHeights">Výstupní pole výšek.</param>
    /// <param name="loadedObjectCodes">Výstupní pole kódů objektů.</param>
    /// <param name="loadedBlueValues">Výstupní pole modrého kanálu.</param>
    /// <param name="loadedAlphaValues">Výstupní pole alfa kanálu.</param>
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

                int encodedHeight = (pixel.Blue * 256) + pixel.Red;
                loadedHeights[x, z] = encodedHeight * HeightScale;
                loadedObjectCodes[x, z] = pixel.Green;
                loadedBlueValues[x, z] = pixel.Blue;
                loadedAlphaValues[x, z] = pixel.Alpha;
            }
        }
    }

    /// <summary>
    /// Vytvoří vrcholová data mesh terénu.
    /// </summary>
    /// <returns>Pole vrcholů terénu.</returns>
    private VertexColorNormal[] BuildMeshVertices()
    {
        VertexColorNormal[] result = new VertexColorNormal[width * depth];

        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float worldX = x * SampleSpacing;
                float worldY = Heights[x, z];
                float worldZ = z * SampleSpacing;

                Vector3 normal = CalculateNormalAtGrid(x, z);
                Vector3 color = GetHeightColor(worldY);

                int index = ToVertexIndex(x, z);
                result[index] = new VertexColorNormal(new Vector3(worldX, worldY, worldZ), color, normal);
            }
        }

        return result;
    }

    /// <summary>
    /// Vytvoří indexová data trojúhelníků terénu.
    /// </summary>
    /// <returns>Pole trojúhelníků.</returns>
    private Triangle[] BuildMeshTriangles()
    {
        Triangle[] result = new Triangle[(width - 1) * (depth - 1) * 2];
        int index = 0;

        for (int z = 0; z < depth - 1; z++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                int i00 = ToVertexIndex(x, z);
                int i10 = ToVertexIndex(x + 1, z);
                int i01 = ToVertexIndex(x, z + 1);
                int i11 = ToVertexIndex(x + 1, z + 1);

                result[index++] = new Triangle(i00, i01, i10);
                result[index++] = new Triangle(i10, i01, i11);
            }
        }

        return result;
    }

    /// <summary>
    /// Přepočítá 2D souřadnice mřížky na index do vrcholového pole.
    /// </summary>
    /// <param name="x">Souřadnice X v mřížce.</param>
    /// <param name="z">Souřadnice Z v mřížce.</param>
    /// <returns>Index vrcholu.</returns>
    private int ToVertexIndex(int x, int z)
    {
        return z * width + x;
    }

    /// <summary>
    /// Vypočítá normálu vrcholu ze čtyř sousedních výšek (vlevo, vpravo, dole, nahoře).
    /// </summary>
    /// <param name="gridX">Souřadnice X v mřížce.</param>
    /// <param name="gridZ">Souřadnice Z v mřížce.</param>
    /// <returns>Znormalizovaný normálový vektor.</returns>
    private Vector3 CalculateNormalAtGrid(int gridX, int gridZ)
    {
        float s = SampleSpacing;

        float x = gridX * s;
        float z = gridZ * s;

        float heightLeft = GetHeightAt(x - s, z);
        float heightRight = GetHeightAt(x + s, z);
        float heightDown = GetHeightAt(x, z - s);
        float heightUp = GetHeightAt(x, z + s);

        Vector3 normal = new Vector3(heightLeft - heightRight, 2.0f * s, heightDown - heightUp);

        if (normal.LengthSquared > 0.0f)
        {
            normal = Vector3.Normalize(normal);
        }
        else
        {
            normal = Vector3.UnitY;
        }

        return normal;
    }

    /// <summary>
    /// Převede výšku terénu na barvu od trávy přes hlínu a skálu až po sníh.
    /// </summary>
    /// <param name="height">Výška vrcholu v jednotkách světa.</param>
    /// <returns>Barva vrcholu terénu.</returns>
    private Vector3 GetHeightColor(float height)
    {
        float normalizedHeight = 0.0f;
        float heightRange = maxHeight - minHeight;
        if (heightRange > 0.0f)
        {
            normalizedHeight = (height - minHeight) / heightRange;
        }

        normalizedHeight = MathHelper.Clamp(normalizedHeight, 0.0f, 1.0f);

        Vector3 grassColor = new Vector3(0.14f, 0.42f, 0.14f);
        Vector3 dirtColor = new Vector3(0.45f, 0.32f, 0.20f);
        Vector3 rockColor = new Vector3(0.56f, 0.55f, 0.54f);
        Vector3 snowColor = new Vector3(0.96f, 0.97f, 0.99f);

        bool snowEnabled = heightRange >= SnowHeightRangeThreshold;

        if (normalizedHeight < GrassToDirtThreshold)
        {
            float t = normalizedHeight / GrassToDirtThreshold;
            return Vector3.Lerp(grassColor, dirtColor, t);
        }

        if (normalizedHeight < DirtToRockThreshold)
        {
            float t = (normalizedHeight - GrassToDirtThreshold) / (DirtToRockThreshold - GrassToDirtThreshold);
            return Vector3.Lerp(dirtColor, rockColor, t);
        }

        if (!snowEnabled)
        {
            return rockColor;
        }

        if (normalizedHeight < RockToSnowThreshold)
        {
            float t = (normalizedHeight - DirtToRockThreshold) / (RockToSnowThreshold - DirtToRockThreshold);
            return Vector3.Lerp(rockColor, snowColor, t);
        }

        return snowColor;
    }

    /// <summary>
    /// Vrátí výšku terénu v dané světové souřadnici.
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

        float row0Height = MathHelper.Lerp(h00, h10, blendX);
        float row1Height = MathHelper.Lerp(h01, h11, blendX);

        return MathHelper.Lerp(row0Height, row1Height, blendZ);
    }

    /// <summary>
    /// Vykreslí terén.
    /// </summary>
    public override void Draw()
    {
        GL.BindVertexArray(VAO);
        GL.DrawElements(PrimitiveType.Triangles, triangles.Length * 3, DrawElementsType.UnsignedInt, IntPtr.Zero);
        GL.BindVertexArray(0);
    }

    /// <summary>
    /// Uvolní grafické prostředky terénu.
    /// </summary>
    public override void Dispose()
    {
        if (disposed)
        {
            return;
        }

        GL.DeleteBuffer(VBO);
        GL.DeleteBuffer(IBO);
        GL.DeleteVertexArray(VAO);
        disposed = true;
    }
}