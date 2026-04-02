using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using OpenTK.Mathematics;

namespace Krajinka;

/// <summary>
/// Načítá OBJ soubor a vrací vrcholy a trojúhelníky.
/// </summary>
public static class ObjLoader
{
    /// <summary>
    /// Načte OBJ soubor řádek po řádku.
    /// </summary>
    /// <param name="filename">Cesta k OBJ souboru.</param>
    /// <returns>Vrací pole vrcholů a pole trojúhelníků.</returns>
    public static (VertexNormal[] vertices, Triangle[] triangles) Load(string filename)
    {
        if (!File.Exists(filename))
        {
            throw new FileNotFoundException("OBJ soubor nebyl nalezen.", filename);
        }

        string[] lines = File.ReadAllLines(filename);

        List<VertexNormal> vertices = new List<VertexNormal>();
        List<Triangle> triangles = new List<Triangle>();
        List<Vector3> normals = new List<Vector3>();

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex].Trim();

            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            if (line.StartsWith("v ", StringComparison.Ordinal))
            {
                ParseVertex(line, vertices);
                continue;
            }

            if (line.StartsWith("vn ", StringComparison.Ordinal))
            {
                ParseNormal(line, normals);
                continue;
            }

            if (line.StartsWith("f ", StringComparison.Ordinal))
            {
                ParseFace(line, vertices, normals, triangles);
            }
        }

        NormalizeVertexNormals(vertices);

        return (vertices.ToArray(), triangles.ToArray());
    }

    /// <summary>
    /// Načte vrchol z řádku v.
    /// </summary>
    private static void ParseVertex(string line, List<VertexNormal> vertices)
    {
        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
        {
            return;
        }

        float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
        float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
        float z = float.Parse(parts[3], CultureInfo.InvariantCulture);

        vertices.Add(new VertexNormal(new Vector3(x, y, z), Vector3.Zero));
    }

    /// <summary>
    /// Načte normálu z řádku vn.
    /// </summary>
    private static void ParseNormal(string line, List<Vector3> normals)
    {
        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
        {
            return;
        }

        float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
        float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
        float z = float.Parse(parts[3], CultureInfo.InvariantCulture);

        Vector3 normal = new Vector3(x, y, z);
        if (normal.LengthSquared > 0)
        {
            normal = Vector3.Normalize(normal);
        }

        normals.Add(normal);
    }

    /// <summary>
    /// Načte trojúhelník z řádku f.
    /// </summary>
    private static void ParseFace(string line, List<VertexNormal> vertices, List<Vector3> normals, List<Triangle> triangles)
    {
        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 4)
        {
            return;
        }

        ParseFaceVertex(parts[1], out int i0, out int n0);
        i0 -= 1;

        if (i0 < 0 || i0 >= vertices.Count)
        {
            return;
        }

        for (int i = 2; i + 1 < parts.Length; i++)
        {
            ParseFaceVertex(parts[i], out int i1, out int n1);
            ParseFaceVertex(parts[i + 1], out int i2, out int n2);

            i1 -= 1;
            i2 -= 1;

            if (i1 < 0 || i2 < 0 || i1 >= vertices.Count || i2 >= vertices.Count)
            {
                continue;
            }

            triangles.Add(new Triangle(i0, i1, i2));

            AddNormalToVertex(vertices, normals, i0, n0 - 1);
            AddNormalToVertex(vertices, normals, i1, n1 - 1);
            AddNormalToVertex(vertices, normals, i2, n2 - 1);
        }
    }

    /// <summary>
    /// Rozparsuje token vrcholu ve tvaru v/vt/vn.
    /// </summary>
    private static void ParseFaceVertex(string token, out int vertexIndex, out int normalIndex)
    {
        string[] values = token.Split('/');

        vertexIndex = 0;
        normalIndex = 0;

        if (values.Length > 0)
        {
            int.TryParse(values[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out vertexIndex);
        }

        if (values.Length > 2)
        {
            int.TryParse(values[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out normalIndex);
        }
    }

    /// <summary>
    /// Přičte normálu do vrcholu.
    /// </summary>
    private static void AddNormalToVertex(List<VertexNormal> vertices, List<Vector3> normals, int vertexIndex, int normalIndex)
    {
        if (normalIndex < 0 || normalIndex >= normals.Count)
        {
            return;
        }

        VertexNormal vertex = vertices[vertexIndex];
        vertex.Normal += normals[normalIndex];
        vertices[vertexIndex] = vertex;
    }

    /// <summary>
    /// Znormalizuje normály všech vrcholů.
    /// </summary>
    private static void NormalizeVertexNormals(List<VertexNormal> vertices)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            VertexNormal vertex = vertices[i];

            if (vertex.Normal.LengthSquared > 0)
            {
                vertex.Normal = Vector3.Normalize(vertex.Normal);
            }
            else
            {
                vertex.Normal = Vector3.UnitY;
            }

            vertices[i] = vertex;
        }
    }
}
