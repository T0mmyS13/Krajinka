using System.Runtime.InteropServices;
using OpenTK.Mathematics;

namespace Krajinka;


/// <summary>
/// Vrchol s pozicí, barvou a normálou.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VertexColorNormal
{
    /// <summary>
    /// Pozice vrcholu.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// Barva vrcholu.
    /// </summary>
    public Vector3 Color;

    /// <summary>
    /// Normála vrcholu.
    /// </summary>
    public Vector3 Normal;

    /// <summary>
    /// Vytvoří vrchol s pozicí, barvou a normálou.
    /// </summary>
    /// <param name="position">Pozice vrcholu.</param>
    /// <param name="color">Barva vrcholu.</param>
    /// <param name="normal">Normála vrcholu.</param>
    public VertexColorNormal(Vector3 position, Vector3 color, Vector3 normal)
    {
        Position = position;
        Color = color;
        Normal = normal;
    }

    /// <summary>
    /// Vrátí velikost struktury v bajtech.
    /// </summary>
    /// <returns>Velikost v bajtech.</returns>
    public static int GetSizeInBytes()
    {
        return Marshal.SizeOf<VertexColorNormal>();
    }
}

/// <summary>
/// Trojúhelník indexů do vrcholového pole.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Triangle
{
    /// <summary>
    /// První index vrcholu.
    /// </summary>
    public int i0;

    /// <summary>
    /// Druhý index vrcholu.
    /// </summary>
    public int i1;

    /// <summary>
    /// Třetí index vrcholu.
    /// </summary>
    public int i2;

    /// <summary>
    /// Vytvoří trojúhelník ze tří indexů.
    /// </summary>
    /// <param name="index0">První index.</param>
    /// <param name="index1">Druhý index.</param>
    /// <param name="index2">Třetí index.</param>
    public Triangle(int index0, int index1, int index2)
    {
        i0 = index0;
        i1 = index1;
        i2 = index2;
    }
}
