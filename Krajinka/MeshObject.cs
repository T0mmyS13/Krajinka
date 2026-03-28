using System;
using System.IO;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Krajinka;

/// <summary>
/// Obecný objekt scény načtený z OBJ souboru.
/// </summary>
public class MeshObject : SceneObject
{
    /// <summary>
    /// Vytvoří objekt z OBJ souboru.
    /// </summary>
    /// <param name="objRelativePath">Relativní cesta k OBJ souboru.</param>
    /// <param name="color">Barva objektu.</param>
    /// <param name="position">Pozice objektu ve scéně.</param>
    public MeshObject(string objRelativePath, Vector3 color, Vector3 position)
        : base(position)
    {
        string fullPath = Path.Combine(AppContext.BaseDirectory, objRelativePath);
        float[] vertices = ObjLoader.LoadVertices(fullPath, color);

        InitializeShaderProgram(
            Path.Combine("Shaders", "terrain.vert"),
            Path.Combine("Shaders", "terrain.frag"));

        InitializeMesh(vertices);
    }

    /// <summary>
    /// Vykreslí objekt.
    /// </summary>
    /// <param name="view">Pohledová matice kamery.</param>
    /// <param name="projection">Projekční matice kamery.</param>
    public override void Render(Matrix4 view, Matrix4 projection)
    {
        Matrix4 model = GetModelMatrix();
        BeginRender(model, view, projection);
        DrawMeshTriangles();
    }
}
