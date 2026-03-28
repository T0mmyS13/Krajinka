using System.IO;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Krajinka;

/// <summary>
/// Reprezentuje obecný objekt ve scéně s transformací a společnými OpenGL metodami.
/// </summary>
public class SceneObject
{
    private static readonly Vector3 DefaultLightDirection = new Vector3(0.5f, -1.0f, 0.3f);

    private Vector3 position;
    private Vector3 rotation;
    private Vector3 scale;

    private int vertexArrayObject;
    private int vertexBufferObject;
    private int shaderProgram;
    private int meshVertexCount;

    /// <summary>
    /// Vytvoří objekt na zadané pozici.
    /// </summary>
    /// <param name="position">Výchozí pozice objektu.</param>
    public SceneObject(Vector3 position)
    {
        this.position = position;
        rotation = Vector3.Zero;
        scale = Vector3.One;
    }

    /// <summary>
    /// Pozice objektu.
    /// </summary>
    public Vector3 Position
    {
        get
        {
            return position;
        }
        set
        {
            position = value;
        }
    }

    /// <summary>
    /// Rotace objektu v radiánech (X, Y, Z).
    /// </summary>
    public Vector3 Rotation
    {
        get
        {
            return rotation;
        }
        set
        {
            rotation = value;
        }
    }

    /// <summary>
    /// Měřítko objektu.
    /// </summary>
    public Vector3 Scale
    {
        get
        {
            return scale;
        }
        set
        {
            scale = value;
        }
    }

    /// <summary>
    /// Vrátí modelovou matici objektu.
    /// </summary>
    /// <returns>Modelová transformační matice.</returns>
    public Matrix4 GetModelMatrix()
    {
        Matrix4 scaleMatrix = Matrix4.CreateScale(scale);
        Matrix4 rotationX = Matrix4.CreateRotationX(rotation.X);
        Matrix4 rotationY = Matrix4.CreateRotationY(rotation.Y);
        Matrix4 rotationZ = Matrix4.CreateRotationZ(rotation.Z);
        Matrix4 translation = Matrix4.CreateTranslation(position);

        return scaleMatrix * rotationX * rotationY * rotationZ * translation;
    }
    /// <summary>
    /// Načte text shaderu ze souboru ve výstupní složce aplikace.
    /// </summary>
    /// <param name="relativePath">Relativní cesta k shader souboru.</param>
    /// <returns>Textový obsah shaderu.</returns>
    private static string LoadShaderSource(string relativePath)
    {
        string fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        return File.ReadAllText(fullPath);
    }

    /// <summary>
    /// Inicializuje shader program ze souborů.
    /// </summary>
    /// <param name="vertexShaderRelativePath">Relativní cesta k vertex shaderu.</param>
    /// <param name="fragmentShaderRelativePath">Relativní cesta k fragment shaderu.</param>
    protected void InitializeShaderProgram(string vertexShaderRelativePath, string fragmentShaderRelativePath)
    {
        string vertexShaderSource = LoadShaderSource(vertexShaderRelativePath);
        string fragmentShaderSource = LoadShaderSource(fragmentShaderRelativePath);

        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.CompileShader(vertexShader);

        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
        GL.CompileShader(fragmentShader);

        shaderProgram = GL.CreateProgram();
        GL.AttachShader(shaderProgram, vertexShader);
        GL.AttachShader(shaderProgram, fragmentShader);
        GL.LinkProgram(shaderProgram);

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
    }

    /// <summary>
    /// Inicializuje VAO/VBO a atributy vrcholů pro formát pozice, barva, normála.
    /// </summary>
    /// <param name="meshVertices">Vrcholová data ve formátu 9 floatů na vrchol.</param>
    protected void InitializeMesh(float[] meshVertices)
    {
        meshVertexCount = meshVertices.Length / 9;

        vertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(vertexArrayObject);

        vertexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, meshVertices.Length * sizeof(float), meshVertices, BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 9 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 9 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 9 * sizeof(float), 6 * sizeof(float));
        GL.EnableVertexAttribArray(2);
    }

    /// <summary>
    /// Připraví společné uniformy pro vykreslení.
    /// Pokud není předán směr světla, použije se výchozí směr světla.
    /// </summary>
    /// <param name="model">Modelová matice objektu.</param>
    /// <param name="view">Pohledová matice kamery.</param>
    /// <param name="projection">Projekční matice kamery.</param>
    /// <param name="lightDirection">Volitelný směr světla.</param>
    protected void BeginRender(Matrix4 model, Matrix4 view, Matrix4 projection, Vector3? lightDirection = null)
    {
        GL.UseProgram(shaderProgram);
        GL.BindVertexArray(vertexArrayObject);

        int modelLocation = GL.GetUniformLocation(shaderProgram, "model");
        int viewLocation = GL.GetUniformLocation(shaderProgram, "view");
        int projectionLocation = GL.GetUniformLocation(shaderProgram, "projection");
        int lightDirectionLocation = GL.GetUniformLocation(shaderProgram, "lightDir");

        Vector3 selectedLightDirection = lightDirection ?? DefaultLightDirection;

        GL.UniformMatrix4(modelLocation, true, ref model);
        GL.UniformMatrix4(viewLocation, true, ref view);
        GL.UniformMatrix4(projectionLocation, true, ref projection);
        GL.Uniform3(lightDirectionLocation, selectedLightDirection);
    }

    /// <summary>
    /// Vykreslí mesh jako trojúhelníky.
    /// </summary>
    protected void DrawMeshTriangles()
    {
        GL.DrawArrays(PrimitiveType.Triangles, 0, meshVertexCount);
    }

    /// <summary>
    /// Vykreslí objekt.
    /// </summary>
    /// <param name="view">Pohledová matice kamery.</param>
    /// <param name="projection">Projekční matice kamery.</param>
    public virtual void Render(Matrix4 view, Matrix4 projection)
    {
    }


}
