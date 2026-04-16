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
    /// <param name="defaultColor">Nepoužito (zachováno kvůli kompatibilitě).</param>
    /// <returns>Vrací pole vrcholů a pole trojúhelníků.</returns>
    public static (VertexNormalTexCoord[] vertices, Triangle[] triangles) Load(string filename, Vector3 defaultColor)
    {
        if (!File.Exists(filename))
        {
            throw new FileNotFoundException("OBJ soubor nebyl nalezen.", filename);
        }

        string[] lines = File.ReadAllLines(filename);

        List<VertexNormalTexCoord> vertices = new List<VertexNormalTexCoord>();
        List<Triangle> triangles = new List<Triangle>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> texCoords = new List<Vector2>();

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

            if (line.StartsWith("vt ", StringComparison.Ordinal))
            {
                ParseTexCoord(line, texCoords);
                continue;
            }

            if (line.StartsWith("f ", StringComparison.Ordinal))
            {
                ParseFace(line, vertices, normals, texCoords, triangles);
            }
        }

        NormalizeVertexNormals(vertices);

        return (vertices.ToArray(), triangles.ToArray());
    }

    /// <summary>
    /// Načte vrchol z řádku v.
    /// </summary>
    /// <param name="line">Řádek OBJ souboru.</param>
    /// <param name="vertices">Seznam vrcholů.</param>
    private static void ParseVertex(string line, List<VertexNormalTexCoord> vertices)
    {
        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
        {
            return;
        }

        float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
        float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
        float z = float.Parse(parts[3], CultureInfo.InvariantCulture);

        vertices.Add(new VertexNormalTexCoord(new Vector3(x, y, z), Vector3.Zero, Vector2.Zero));
    }

    /// <summary>
    /// Načte texturovou souřadnici z řádku vt.
    /// </summary>
    /// <param name="line">Řádek OBJ souboru.</param>
    /// <param name="texCoords">Seznam UV souřadnic.</param>
    private static void ParseTexCoord(string line, List<Vector2> texCoords)
    {
        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            return;
        }

        float u = float.Parse(parts[1], CultureInfo.InvariantCulture);
        float v = float.Parse(parts[2], CultureInfo.InvariantCulture);
        texCoords.Add(new Vector2(u, v));
    }

    /// <summary>
    /// Načte normálu z řádku vn.
    /// </summary>
    /// <param name="line">Řádek OBJ souboru.</param>
    /// <param name="normals">Seznam normál.</param>
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

        normals.Add(new Vector3(x, y, z));
    }

    /// <summary>
    /// Načte trojúhelník z řádku f.
    /// </summary>
    /// <param name="line">Řádek OBJ souboru.</param>
    /// <param name="vertices">Seznam vrcholů.</param>
    /// <param name="normals">Seznam normál.</param>
    /// <param name="texCoords">Seznam UV souřadnic.</param>
    /// <param name="triangles">Seznam trojúhelníků.</param>
    private static void ParseFace(string line, List<VertexNormalTexCoord> vertices, List<Vector3> normals, List<Vector2> texCoords, List<Triangle> triangles)
    {
        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 4)
        {
            return;
        }

        ParseFaceVertex(parts[1], out int i0, out int t0, out int n0);
        i0 -= 1;

        if (i0 < 0 || i0 >= vertices.Count)
        {
            return;
        }

        for (int i = 2; i + 1 < parts.Length; i++)
        {
            ParseFaceVertex(parts[i], out int i1, out int t1, out int n1);
            ParseFaceVertex(parts[i + 1], out int i2, out int t2, out int n2);

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

            SetTexCoordToVertex(vertices, texCoords, i0, t0 - 1);
            SetTexCoordToVertex(vertices, texCoords, i1, t1 - 1);
            SetTexCoordToVertex(vertices, texCoords, i2, t2 - 1);
        }
    }

    /// <summary>
    /// Rozparsuje token vrcholu ve tvaru v/vt/vn.
    /// </summary>
    /// <param name="token">Token vrcholu z řádku f.</param>
    /// <param name="vertexIndex">Výstupní index vrcholu.</param>
    /// <param name="texCoordIndex">Výstupní index tex coord.</param>
    /// <param name="normalIndex">Výstupní index normály.</param>
    private static void ParseFaceVertex(string token, out int vertexIndex, out int texCoordIndex, out int normalIndex)
    {
        string[] values = token.Split('/');

        vertexIndex = 0;
        texCoordIndex = 0;
        normalIndex = 0;

        if (values.Length > 0)
        {
            int.TryParse(values[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out vertexIndex);
        }

        if (values.Length > 1)
        {
            int.TryParse(values[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out texCoordIndex);
        }

        if (values.Length > 2)
        {
            int.TryParse(values[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out normalIndex);
        }
    }

    /// <summary>
    /// Přičte normálu do vrcholu.
    /// </summary>
    /// <param name="vertices">Seznam vrcholů.</param>
    /// <param name="normals">Seznam normál.</param>
    /// <param name="vertexIndex">Index vrcholu.</param>
    /// <param name="normalIndex">Index normály.</param>
    private static void AddNormalToVertex(List<VertexNormalTexCoord> vertices, List<Vector3> normals, int vertexIndex, int normalIndex)
    {
        if (normalIndex < 0 || normalIndex >= normals.Count)
        {
            return;
        }

        VertexNormalTexCoord vertex = vertices[vertexIndex];
        vertex.Normal += normals[normalIndex];
        vertices[vertexIndex] = vertex;
    }

    /// <summary>
    /// Nastaví UV souřadnici vrcholu.
    /// </summary>
    /// <param name="vertices">Seznam vrcholů.</param>
    /// <param name="vertexIndex">Index vrcholu.</param>
    /// <param name="texCoords">Seznam UV souřadnic.</param>
    /// <param name="texCoordIndex">Index UV souřadnice.</param>
    private static void SetTexCoordToVertex(List<VertexNormalTexCoord> vertices, List<Vector2> texCoords, int vertexIndex, int texCoordIndex)
    {
        if (texCoordIndex < 0 || texCoordIndex >= texCoords.Count)
        {
            return;
        }

        VertexNormalTexCoord vertex = vertices[vertexIndex];
        vertex.UV = texCoords[texCoordIndex];
        vertices[vertexIndex] = vertex;
    }

    /// <summary>
    /// Znormalizuje normály všech vrcholů.
    /// </summary>
    /// <param name="vertices">Seznam vrcholů.</param>
    private static void NormalizeVertexNormals(List<VertexNormalTexCoord> vertices)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            VertexNormalTexCoord vertex = vertices[i];

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
