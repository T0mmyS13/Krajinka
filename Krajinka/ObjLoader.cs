using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using OpenTK.Mathematics;

namespace Krajinka;

/// <summary>
/// Načítá OBJ soubor a převádí ho do pole vrcholů pro OpenGL.
/// </summary>
public static class ObjLoader
{
    /// <summary>
    /// Načte OBJ soubor a vrátí vrcholy ve formátu pozice, barva, normála.
    /// </summary>
    /// <param name="filename">Cesta k OBJ souboru.</param>
    /// <param name="color">Barva, která se použije pro všechny vrcholy.</param>
    /// <returns>Pole vrcholů pro OpenGL.</returns>
    public static float[] LoadVertices(string filename, Vector3 color)
    {
        if (!File.Exists(filename))
        {
            throw new FileNotFoundException("OBJ soubor nebyl nalezen.", filename);
        }

        string[] lines = File.ReadAllLines(filename);

        List<Vector3> positions = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<float> vertices = new List<float>();

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex].Trim();

            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            if (line.StartsWith("v ", StringComparison.Ordinal))
            {
                ParseVertexPosition(line, positions);
            }
            else if (line.StartsWith("vn ", StringComparison.Ordinal))
            {
                ParseVertexNormal(line, normals);
            }
            else if (line.StartsWith("f ", StringComparison.Ordinal))
            {
                ParseFace(line, positions, normals, color, vertices);
            }
        }

        return vertices.ToArray();
    }

    /// <summary>
    /// Zpracuje řádek s pozicí vrcholu.
    /// </summary>
    /// <param name="line">Textový řádek OBJ.</param>
    /// <param name="positions">Seznam pozic vrcholů.</param>
    private static void ParseVertexPosition(string line, List<Vector3> positions)
    {
        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
        {
            return;
        }

        float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
        float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
        float z = float.Parse(parts[3], CultureInfo.InvariantCulture);

        positions.Add(new Vector3(x, y, z));
    }

    /// <summary>
    /// Zpracuje řádek s normálou vrcholu.
    /// </summary>
    /// <param name="line">Textový řádek OBJ.</param>
    /// <param name="normals">Seznam normál vrcholů.</param>
    private static void ParseVertexNormal(string line, List<Vector3> normals)
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
        if (normal.LengthSquared > 0.0f)
        {
            normal = Vector3.Normalize(normal);
        }

        normals.Add(normal);
    }

    /// <summary>
    /// Zpracuje řádek s face a přidá trojúhelníky do výsledného pole.
    /// </summary>
    /// <param name="line">Textový řádek OBJ.</param>
    /// <param name="positions">Seznam pozic vrcholů.</param>
    /// <param name="normals">Seznam normál vrcholů.</param>
    /// <param name="color">Barva vrcholu.</param>
    /// <param name="vertices">Výsledné pole vrcholů.</param>
    private static void ParseFace(string line, List<Vector3> positions, List<Vector3> normals, Vector3 color, List<float> vertices)
    {
        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
        {
            return;
        }

        for (int i = 1; i + 2 < parts.Length; i++)
        {
            AddFaceVertex(parts[1], positions, normals, color, vertices);
            AddFaceVertex(parts[i + 1], positions, normals, color, vertices);
            AddFaceVertex(parts[i + 2], positions, normals, color, vertices);
        }
    }

    /// <summary>
    /// Přidá jeden vrchol face do výsledného pole vrcholů.
    /// </summary>
    /// <param name="token">Token face ve formátu v/t/n nebo v//n.</param>
    /// <param name="positions">Seznam pozic vrcholů.</param>
    /// <param name="normals">Seznam normál vrcholů.</param>
    /// <param name="color">Barva vrcholu.</param>
    /// <param name="vertices">Výsledné pole vrcholů.</param>
    private static void AddFaceVertex(string token, List<Vector3> positions, List<Vector3> normals, Vector3 color, List<float> vertices)
    {
        string[] indexes = token.Split('/');

        int positionIndex = int.Parse(indexes[0], CultureInfo.InvariantCulture) - 1;
        int normalIndex = int.Parse(indexes[2], CultureInfo.InvariantCulture) - 1;

        Vector3 position = positions[positionIndex];
        Vector3 normal = normals[normalIndex];

        vertices.Add(position.X);
        vertices.Add(position.Y);
        vertices.Add(position.Z);

        vertices.Add(color.X);
        vertices.Add(color.Y);
        vertices.Add(color.Z);

        vertices.Add(normal.X);
        vertices.Add(normal.Y);
        vertices.Add(normal.Z);
    }
}
