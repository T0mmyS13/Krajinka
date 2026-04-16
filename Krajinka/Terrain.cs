using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Krajinka;

/// <summary>
/// Typ povrchu terénu na základě sklonu a výšky.
/// </summary>
internal enum SurfaceType : byte
{
    /// <summary>
    /// Voda (alfa kanál = 0 nebo nízká výška).
    /// </summary>
    Water = 0,

    /// <summary>
    /// Tráva (nízký sklon, střední výška, ne u vody).
    /// </summary>
    Grass = 2,

    /// <summary>
    /// Skála (vysoký sklon nebo vysoká výška).
    /// </summary>
    Rock = 3,

    /// <summary>
    /// Hlina (blízko vody).
    /// </summary>
    Mud = 1
}

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
    /// Poloměr hledání vody (ve vzorcích mřížky) pro určení hliny.
    /// </summary>
    private const int MudWaterSearchRadius = 5;

    /// <summary>
    /// Prahová výška pro určení povrchu jako voda.
    /// </summary>
    private const float WaterHeightThreshold = 1.0f;

    /// <summary>
    /// Prahová výška pro určení povrchu jako skála (vysoko).
    /// </summary>
    private const float RockHeightThreshold = 8.0f;

    /// <summary>
    /// Prahový sklon (v radiánech) pro určení povrchu jako skála.
    /// </summary>
    private const float RockSlopeThreshold = 0.4f;

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
    private readonly VertexNormalTexCoord[] vertices;

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
    /// Textury pro jednotlivé typy povrchů.
    /// </summary>
    private readonly Dictionary<SurfaceType, Texture> surfaceTextures;

    /// <summary>
    /// ID textury mapy typů povrchů generované z logiky terénu.
    /// </summary>
    private readonly int surfaceTypeMapTextureId;


    /// <summary>
    /// Výšky terénu v mřížce [x, z].
    /// </summary>
    public readonly float[,] Heights;

    /// <summary>
    /// Typy povrchů v mřížce [x, z].
    /// </summary>
    public readonly SurfaceType[,] SurfaceTypes;

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
        surfaceTextures = new Dictionary<SurfaceType, Texture>();

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

        SurfaceTypes = DetermineSurfaceTypes();
        LoadSurfaceTextures();
        surfaceTypeMapTextureId = CreateSurfaceTypeMapTexture();

        vertices = BuildMeshVertices();
        triangles = BuildMeshTriangles();

        VAO = GL.GenVertexArray();
        GL.BindVertexArray(VAO);

        VBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * VertexNormalTexCoord.GetSizeInBytes(), vertices, BufferUsageHint.StaticDraw);

        IBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, IBO);
        GL.BufferData(BufferTarget.ElementArrayBuffer, triangles.Length * 3 * sizeof(int), triangles, BufferUsageHint.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, VertexNormalTexCoord.GetSizeInBytes(), IntPtr.Zero);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, VertexNormalTexCoord.GetSizeInBytes(), (IntPtr)Vector3.SizeInBytes);
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, VertexNormalTexCoord.GetSizeInBytes(), 2 * (IntPtr)Vector3.SizeInBytes);

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
    }

    /// <summary>
    /// Načte textury povrchů ze složky Textures podle názvů enum hodnot.
    /// </summary>
    private void LoadSurfaceTextures()
    {
        foreach (SurfaceType surfaceType in Enum.GetValues<SurfaceType>())
        {
            string fileName = surfaceType.ToString().ToLowerInvariant() + ".png";
            string relativePath = Path.Combine("Data", "textures", fileName);
            Texture texture = new Texture(relativePath, TextureSetting.Default);

            surfaceTextures[surfaceType] = texture;
        }
    }

    /// <summary>
    /// Vytvoří texturu mapy typů povrchu přímo z hodnot SurfaceTypes.
    /// </summary>
    /// <returns>ID OpenGL textury.</returns>
    private int CreateSurfaceTypeMapTexture()
    {
        byte[] data = new byte[width * depth * 4];
        int index = 0;

        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                byte surfaceType = (byte)SurfaceTypes[x, z];
                data[index++] = surfaceType;
                data[index++] = 0;
                data[index++] = 0;
                data[index++] = 255;
            }
        }

        int textureId = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, textureId);
        GL.TexImage2D(
            TextureTarget.Texture2D,
            0,
            PixelInternalFormat.Rgba,
            width,
            depth,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            data);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        return textureId;
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
    /// Určí typ povrchu pro každou pozici na základě sklonu a výšky.
    /// </summary>
    /// <returns>Pole typů povrchů.</returns>
    private SurfaceType[,] DetermineSurfaceTypes()
    {
        SurfaceType[,] result = new SurfaceType[width, depth];

        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float height = Heights[x, z];
                byte alphaValue = AlphaValues[x, z];

                if (alphaValue == 0 || height < WaterHeightThreshold)
                {
                    result[x, z] = SurfaceType.Water;
                }
                else if (height > RockHeightThreshold)
                {
                    result[x, z] = SurfaceType.Rock;
                }
                else
                {
                    float slope = CalculateSlopeAtGrid(x, z);

                    if (slope > RockSlopeThreshold)
                    {
                        result[x, z] = SurfaceType.Rock;
                    }
                    else if (IsNearWater(x, z, MudWaterSearchRadius))
                    {
                        result[x, z] = SurfaceType.Mud;
                    }
                    else
                    {
                        result[x, z] = SurfaceType.Grass;
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Vrátí true, pokud je pozice v mřížce blízko vody (alpha = 0).
    /// </summary>
    /// <param name="gridX">Souřadnice X v mřížce.</param>
    /// <param name="gridZ">Souřadnice Z v mřížce.</param>
    /// <param name="searchRadius">Poloměr hledání ve vzorcích mřížky.</param>
    /// <returns>True pokud je v okolí voda, jinak false.</returns>
    private bool IsNearWater(int gridX, int gridZ, int searchRadius)
    {
        int startX = Math.Max(0, gridX - searchRadius);
        int endX = Math.Min(width - 1, gridX + searchRadius);
        int startZ = Math.Max(0, gridZ - searchRadius);
        int endZ = Math.Min(depth - 1, gridZ + searchRadius);

        for (int z = startZ; z <= endZ; z++)
        {
            for (int x = startX; x <= endX; x++)
            {
                if (AlphaValues[x, z] == 0)
                {
                    return true;
                }
            }
        }

        return false;
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
    private VertexNormalTexCoord[] BuildMeshVertices()
    {
        VertexNormalTexCoord[] result = new VertexNormalTexCoord[width * depth];

        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float worldX = x * SampleSpacing;
                float worldY = Heights[x, z];
                float worldZ = z * SampleSpacing;

                Vector3 normal = CalculateNormalAtGrid(x, z);
                Vector2 uv = new Vector2(worldX * 0.08f, worldZ * 0.08f);

                int index = ToVertexIndex(x, z);
                result[index] = new VertexNormalTexCoord(new Vector3(worldX, worldY, worldZ), normal, uv);
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
    /// Vypočítá sklon terénu v dané pozici mřížky.
    /// </summary>
    /// <param name="gridX">Souřadnice X v mřížce.</param>
    /// <param name="gridZ">Souřadnice Z v mřížce.</param>
    /// <returns>Sklon v radiánech.</returns>
    private float CalculateSlopeAtGrid(int gridX, int gridZ)
    {
        Vector3 normal = CalculateNormalAtGrid(gridX, gridZ);

        float slopeRadians = MathF.Acos(Math.Clamp(normal.Y, -1.0f, 1.0f));

        return slopeRadians;
    }

    /// <summary>
    /// Určí, zda je možné projít z jedné pozice do druhé při pohybu do kopce.
    /// </summary>
    /// <param name="fromX">Výchozí souřadnice X ve světě.</param>
    /// <param name="fromZ">Výchozí souřadnice Z ve světě.</param>
    /// <param name="toX">Cílová souřadnice X ve světě.</param>
    /// <param name="toZ">Cílová souřadnice Z ve světě.</param>
    /// <param name="uphillSlopeThreshold">Maximální povolená strmost při pohybu vzhůru (radiány).</param>
    /// <returns>True pokud je průchod povolen, jinak false.</returns>
    public bool CanMoveUphill(float fromX, float fromZ, float toX, float toZ, float uphillSlopeThreshold)
    {
        float fromHeight = GetHeightAt(fromX, fromZ);
        float toHeight = GetHeightAt(toX, toZ);

        if (toHeight <= fromHeight)
        {
            return true;
        }

        float clampedToX = MathHelper.Clamp(toX, MinX, MaxX);
        float clampedToZ = MathHelper.Clamp(toZ, MinZ, MaxZ);

        int gridX = (int)MathF.Round(clampedToX / SampleSpacing);
        int gridZ = (int)MathF.Round(clampedToZ / SampleSpacing);

        gridX = Math.Clamp(gridX, 0, width - 1);
        gridZ = Math.Clamp(gridZ, 0, depth - 1);

        float slopeAtTarget = CalculateSlopeAtGrid(gridX, gridZ);

        return slopeAtTarget <= uphillSlopeThreshold;
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

        foreach (Texture texture in surfaceTextures.Values)
        {
            texture.Dispose();
        }

        GL.DeleteTexture(surfaceTypeMapTextureId);

        GL.DeleteBuffer(VBO);
        GL.DeleteBuffer(IBO);
        GL.DeleteVertexArray(VAO);
        disposed = true;
    }

    /// <summary>
    /// Vrátí texturu pro daný typ povrchu.
    /// </summary>
    /// <param name="surfaceType">Typ povrchu.</param>
    /// <returns>Textura povrchu.</returns>
    public Texture GetSurfaceTexture(SurfaceType surfaceType)
    {
        if (surfaceTextures.TryGetValue(surfaceType, out Texture texture))
        {
            return texture;
        }

        throw new InvalidOperationException($"Textura pro povrch '{surfaceType}' nebyla načtena.");
    }

    /// <summary>
    /// Připojí texturu daného povrchu na zadanou texturovou jednotku.
    /// </summary>
    /// <param name="surfaceType">Typ povrchu.</param>
    /// <param name="unit">Texturová jednotka.</param>
    /// <returns>True pokud textura byla připojena.</returns>
    public void BindSurfaceTexture(SurfaceType surfaceType, int unit)
    {
        Texture texture = GetSurfaceTexture(surfaceType);
        texture.Bind(unit);
    }

    /// <summary>
    /// Připojí všechny dostupné textury povrchů na pevné texturové jednotky.
    /// </summary>
    /// <returns>True pokud byly textury připojeny.</returns>
    public void BindSurfaceTextures()
    {
        BindSurfaceTexture(SurfaceType.Water, 0);
        BindSurfaceTexture(SurfaceType.Grass, 1);
        BindSurfaceTexture(SurfaceType.Rock, 2);
        BindSurfaceTexture(SurfaceType.Mud, 3);
        GL.ActiveTexture(TextureUnit.Texture4);
        GL.BindTexture(TextureTarget.Texture2D, surfaceTypeMapTextureId);
    }
}