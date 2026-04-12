using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Krajinka;

/// <summary>
/// Obecný objekt scény načtený z OBJ souboru.
/// </summary>
internal class Model : SceneObject
{
    /// <summary>
    /// Počet indexů použitých při vykreslení modelu.
    /// </summary>
    private readonly int indexCount;

    /// <summary>
    /// ID vertex array objektu.
    /// </summary>
    private int VAO;

    /// <summary>
    /// ID vertex buffer objektu.
    /// </summary>
    private int VBO;

    /// <summary>
    /// ID index buffer objektu.
    /// </summary>
    private int IBO;

    /// <summary>
    /// Indikuje, zda byl model už uvolněn.
    /// </summary>
    private bool disposed;

    /// <summary>
    /// Vytvoří objekt z OBJ souboru.
    /// </summary>
    /// <param name="objRelativePath">Relativní cesta k OBJ souboru.</param>
    /// <param name="color">Výchozí barva objektu, pokud model nepoužívá MTL.</param>
    /// <param name="position">Pozice objektu ve scéně.</param>
    public Model(string objRelativePath, Vector3 color, Vector3 position)
    {
        SetPosition(position);

        string fullPath = Path.Combine(AppContext.BaseDirectory, objRelativePath);
        (VertexColorNormal[] vertices, Triangle[] triangles) = ObjLoader.Load(fullPath, color);

        indexCount = triangles.Length * 3;
        CreateModelBuffers(vertices, triangles);
    }

    /// <summary>
    /// Vytvoří OpenGL buffery pro model.
    /// </summary>
    /// <param name="vertices">Vrcholová data.</param>
    /// <param name="triangles">Indexová data trojúhelníků.</param>
    private void CreateModelBuffers(VertexColorNormal[] vertices, Triangle[] triangles)
    {
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

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, VertexColorNormal.GetSizeInBytes(), IntPtr.Zero);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, VertexColorNormal.GetSizeInBytes(), (IntPtr)Vector3.SizeInBytes);
        GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, VertexColorNormal.GetSizeInBytes(), 2 * (IntPtr)Vector3.SizeInBytes);

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
    }

    /// <summary>
    /// Vykreslí model.
    /// </summary>
    public override void Draw()
    {
        GL.BindVertexArray(VAO);
        GL.DrawElements(PrimitiveType.Triangles, indexCount, DrawElementsType.UnsignedInt, IntPtr.Zero);
        GL.BindVertexArray(0);
    }

    /// <summary>
    /// Uvolní OpenGL prostředky modelu.
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
