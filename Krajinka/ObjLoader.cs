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
    /// <param name="defaultColor">Výchozí barva vrcholů.</param>
    /// <returns>Vrací pole vrcholů a pole trojúhelníků.</returns>
    public static (VertexColorNormal[] vertices, Triangle[] triangles) Load(string filename, Vector3 defaultColor)
    {
        if (!File.Exists(filename))
        {
            throw new FileNotFoundException("OBJ soubor nebyl nalezen.", filename);
        }

        string[] lines = File.ReadAllLines(filename);

        List<VertexColorNormal> vertices = new List<VertexColorNormal>();
        List<Triangle> triangles = new List<Triangle>();
        List<Vector3> normals = new List<Vector3>();

        Dictionary<string, Vector3> materials = new Dictionary<string, Vector3>();
        Vector3 currentColor = defaultColor;
        string directory = Path.GetDirectoryName(filename) ?? string.Empty;

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex].Trim();

            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            if (line.StartsWith("mtllib ", StringComparison.Ordinal))
            {
                string mtlFile = Path.Combine(directory, line.Substring(7).Trim());
                materials = LoadMtl(mtlFile);
                continue;
            }

            if (line.StartsWith("usemtl ", StringComparison.Ordinal))
            {
                string materialName = line.Substring(7).Trim();
                if (materials.TryGetValue(materialName, out Vector3 materialColor))
                {
                    currentColor = materialColor;
                }

                continue;
            }

            if (line.StartsWith("v ", StringComparison.Ordinal))
            {
                ParseVertex(line, vertices, defaultColor);
                continue;
            }

            if (line.StartsWith("vn ", StringComparison.Ordinal))
            {
                ParseNormal(line, normals);
                continue;
            }

            if (line.StartsWith("f ", StringComparison.Ordinal))
            {
                ParseFace(line, vertices, normals, triangles, currentColor);
            }
        }

        NormalizeVertexNormals(vertices);

        return (vertices.ToArray(), triangles.ToArray());
    }

    /// <summary>
    /// Načte MTL soubor a vrátí diffuse barvy materiálů.
    /// </summary>
    /// <param name="mtlPath">Cesta k MTL souboru.</param>
    /// <returns>Slovník materiálů a jejich barev.</returns>
    private static Dictionary<string, Vector3> LoadMtl(string mtlPath)
    {
        Dictionary<string, Vector3> materials = new Dictionary<string, Vector3>();
        if (!File.Exists(mtlPath))
        {
            return materials;
        }

        string currentMaterial = string.Empty;

        foreach (string line in File.ReadAllLines(mtlPath))
        {
            string trimmed = line.Trim();

            if (trimmed.StartsWith("newmtl ", StringComparison.Ordinal))
            {
                currentMaterial = trimmed.Substring(7).Trim();
            }
            else if (trimmed.StartsWith("Kd ", StringComparison.Ordinal) && currentMaterial != string.Empty)
            {
                string[] parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4)
                {
                    continue;
                }

                float r = float.Parse(parts[1], CultureInfo.InvariantCulture);
                float g = float.Parse(parts[2], CultureInfo.InvariantCulture);
                float b = float.Parse(parts[3], CultureInfo.InvariantCulture);

                materials[currentMaterial] = new Vector3(r, g, b);
            }
        }

        return materials;
    }

    /// <summary>
    /// Načte vrchol z řádku v.
    /// </summary>
    /// <param name="line">Řádek OBJ souboru.</param>
    /// <param name="vertices">Seznam vrcholů.</param>
    /// <param name="defaultColor">Výchozí barva vrcholu.</param>
    private static void ParseVertex(string line, List<VertexColorNormal> vertices, Vector3 defaultColor)
    {
        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
        {
            return;
        }

        float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
        float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
        float z = float.Parse(parts[3], CultureInfo.InvariantCulture);

        vertices.Add(new VertexColorNormal(new Vector3(x, y, z), defaultColor, Vector3.Zero));
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
    /// <param name="line">Řádek OBJ souboru.</param>
    /// <param name="vertices">Seznam vrcholů.</param>
    /// <param name="normals">Seznam normál.</param>
    /// <param name="triangles">Seznam trojúhelníků.</param>
    /// <param name="faceColor">Barva aktuální plochy podle materiálu.</param>
    private static void ParseFace(string line, List<VertexColorNormal> vertices, List<Vector3> normals, List<Triangle> triangles, Vector3 faceColor)
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

            SetColorToVertex(vertices, i0, faceColor);
            SetColorToVertex(vertices, i1, faceColor);
            SetColorToVertex(vertices, i2, faceColor);
        }
    }

    /// <summary>
    /// Rozparsuje token vrcholu ve tvaru v/vt/vn.
    /// </summary>
    /// <param name="token">Token vrcholu z řádku f.</param>
    /// <param name="vertexIndex">Výstupní index vrcholu.</param>
    /// <param name="normalIndex">Výstupní index normály.</param>
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
    /// <param name="vertices">Seznam vrcholů.</param>
    /// <param name="normals">Seznam normál.</param>
    /// <param name="vertexIndex">Index vrcholu.</param>
    /// <param name="normalIndex">Index normály.</param>
    private static void AddNormalToVertex(List<VertexColorNormal> vertices, List<Vector3> normals, int vertexIndex, int normalIndex)
    {
        if (normalIndex < 0 || normalIndex >= normals.Count)
        {
            return;
        }

        VertexColorNormal vertex = vertices[vertexIndex];
        vertex.Normal += normals[normalIndex];
        vertices[vertexIndex] = vertex;
    }

    /// <summary>
    /// Nastaví barvu vrcholu.
    /// </summary>
    /// <param name="vertices">Seznam vrcholů.</param>
    /// <param name="vertexIndex">Index vrcholu.</param>
    /// <param name="color">Barva vrcholu.</param>
    private static void SetColorToVertex(List<VertexColorNormal> vertices, int vertexIndex, Vector3 color)
    {
        VertexColorNormal vertex = vertices[vertexIndex];
        vertex.Color = color;
        vertices[vertexIndex] = vertex;
    }

    /// <summary>
    /// Znormalizuje normály všech vrcholů.
    /// </summary>
    /// <param name="vertices">Seznam vrcholů.</param>
    private static void NormalizeVertexNormals(List<VertexColorNormal> vertices)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            VertexColorNormal vertex = vertices[i];

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
