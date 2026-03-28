using System;
using OpenTK.Mathematics;

namespace Krajinka;

/// <summary>
/// Jednoduchá FPS kamera s pozicí, natočením a výpočtem pohledové matice.
/// </summary>
public class Camera
{
    /// <summary>
    /// Aktuální pozice kamery ve světě.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// Směr, kterým kamera aktuálně míří.
    /// </summary>
    public Vector3 Front;

    /// <summary>
    /// Vektor směrem nahoru použitý pro orientaci kamery.
    /// </summary>
    public Vector3 Up;

    /// <summary>
    /// Pravý směrový vektor kamery odvozený z orientace.
    /// </summary>
    public Vector3 Right;

    /// <summary>
    /// Horizontální úhel natočení kamery ve stupních.
    /// </summary>
    public float Yaw;

    /// <summary>
    /// Vertikální úhel natočení kamery ve stupních.
    /// </summary>
    public float Pitch;

    /// <summary>
    /// Vytvoří kameru na zadané pozici a nastaví výchozí směr pohledu.
    /// </summary>
    /// <param name="startPosition">Počáteční pozice kamery ve světě.</param>
    public Camera(Vector3 startPosition)
    {
        Position = startPosition;
        Yaw = -90.0f;
        Pitch = 0.0f;
        Up = new Vector3(0.0f, 1.0f, 0.0f);

        UpdateVectors();
    }

    /// <summary>
    /// Vrátí pohledovou matici kamery pro vykreslování scény.
    /// </summary>
    /// <returns>Pohledová matice vytvořená z pozice a směru kamery.</returns>
    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(Position, Position + Front, Up);
    }

    /// <summary>
    /// Přepočítá směrové vektory kamery podle aktuálních úhlů yaw a pitch.
    /// </summary>
    public void UpdateVectors()
    {
        // Omezení vertikálního náklonu, aby se kamera nepřetočila
        if (Pitch > 89.0f) Pitch = 89.0f;
        if (Pitch < -89.0f) Pitch = -89.0f;

        float radYaw = MathHelper.DegreesToRadians(Yaw);
        float radPitch = MathHelper.DegreesToRadians(Pitch);

        Front.X = (float)(Math.Cos(radPitch) * Math.Cos(radYaw));
        Front.Y = (float)Math.Sin(radPitch);
        Front.Z = (float)(Math.Cos(radPitch) * Math.Sin(radYaw));
        Front = Vector3.Normalize(Front);

        Right = Vector3.Normalize(Vector3.Cross(Front, Up));
    }
}