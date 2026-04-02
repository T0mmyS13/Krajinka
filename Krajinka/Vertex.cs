using System.Runtime.InteropServices;
using OpenTK.Mathematics;

namespace Krajinka;

/// <summary>
/// Vrchol s pozicí a normálou.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VertexNormal
{
    public Vector3 Position;
    public Vector3 Normal;

    public VertexNormal(Vector3 position, Vector3 normal)
    {
        Position = position;
        Normal = normal;
    }

    public static int GetSizeInBytes()
    {
        return Marshal.SizeOf<VertexNormal>();
    }
}

/// <summary>
/// Vrchol s pozicí, barvou a normálou.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VertexColorNormal
{
    public Vector3 Position;
    public Vector3 Color;
    public Vector3 Normal;

    public VertexColorNormal(Vector3 position, Vector3 color, Vector3 normal)
    {
        Position = position;
        Color = color;
        Normal = normal;
    }

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
    public int i0;
    public int i1;
    public int i2;

    public Triangle(int index0, int index1, int index2)
    {
        i0 = index0;
        i1 = index1;
        i2 = index2;
    }
}
