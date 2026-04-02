using OpenTK.Mathematics;

namespace Krajinka;

/// <summary>
/// Jednoduchá reprezentace světla.
/// </summary>
internal class Light
{
    /// <summary>
    /// Pozice světla ve světě (W=1 pro bodové světlo).
    /// </summary>
    public Vector4 Position;

    /// <summary>
    /// Barva světla.
    /// </summary>
    public Vector3 Color;

    /// <summary>
    /// Intenzita světla.
    /// </summary>
    public float Intensity;

    /// <summary>
    /// Vytvoří bodové světlo.
    /// </summary>
    /// <param name="position">Pozice světla.</param>
    /// <param name="color">Barva světla.</param>
    /// <param name="intensity">Intenzita světla.</param>
    /// <returns>Nové bodové světlo.</returns>
    public static Light CreatePoint(Vector3 position, Vector3 color, float intensity)
    {
        Light light = new Light();
        light.Position = new Vector4(position, 1.0f);
        light.Color = color;
        light.Intensity = intensity;
        return light;
    }
}
