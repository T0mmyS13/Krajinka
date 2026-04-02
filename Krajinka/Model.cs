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
    private readonly int indexCount;
    private int VAO;
    private int VBO;
    private int IBO;

    private bool disposed;

    /// <summary>
    /// Vytvoří objekt z OBJ souboru.
    /// </summary>
    /// <param name="objRelativePath">Relativní cesta k OBJ souboru.</param>
    /// <param name="color">Barva objektu.</param>
    /// <param name="position">Pozice objektu ve scéně.</param>
    public Model(string objRelativePath, Vector3 color, Vector3 position)
    {
        SetPosition(position);

        string fullPath = Path.Combine(AppContext.BaseDirectory, objRelativePath);
        (VertexNormal[] loadedVertices, Triangle[] triangles) = ObjLoader.Load(fullPath);
        VertexColorNormal[] vertices = ConvertVertices(loadedVertices, color);

        indexCount = triangles.Length * 3;
        CreateModelBuffers(vertices, triangles);
    }

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

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, VertexColorNormal.GetSizeInBytes(), 0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, VertexColorNormal.GetSizeInBytes(), Vector3.SizeInBytes);
        GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, VertexColorNormal.GetSizeInBytes(), 2 * Vector3.SizeInBytes);

        GL.BindVertexArray(0);
    }

    public override void Draw()
    {
        GL.BindVertexArray(VAO);
        GL.DrawElements(PrimitiveType.Triangles, indexCount, DrawElementsType.UnsignedInt, IntPtr.Zero);
        GL.BindVertexArray(0);
    }

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

    private static VertexColorNormal[] ConvertVertices(VertexNormal[] loadedVertices, Vector3 color)
    {
        VertexColorNormal[] result = new VertexColorNormal[loadedVertices.Length];

        for (int i = 0; i < loadedVertices.Length; i++)
        {
            result[i] = new VertexColorNormal(loadedVertices[i].Position, color, loadedVertices[i].Normal);
        }

        return result;
    }
}
