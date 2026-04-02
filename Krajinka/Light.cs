using OpenTK.Mathematics;

namespace Krajinka;

/// <summary>
/// Jednoduchá reprezentace světla.
/// </summary>
internal class Light
{
    public Vector4 Position;
    public Vector3 Color;
    public float Intensity;

    public static Light CreatePoint(Vector3 position, Vector3 color, float intensity)
    {
        Light light = new Light();
        light.Position = new Vector4(position, 1.0f);
        light.Color = color;
        light.Intensity = intensity;
        return light;
    }
}
